using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles.AI
{
	public class VehicleRegionCostCalculator
	{
		private const int SampleCount = 11;

		private static int[] pathCostSamples = new int[SampleCount];

		private static List<int> tmpCellIndices = new List<int>();

		private static Dictionary<int, float> tmpDistances = new Dictionary<int, float>();

		private static List<int> tmpPathableNeighborIndices = new List<int>();

		private Map map;

		private VehicleMapping mapE;

		private VehicleRegion[] regionGrid;

		private TraverseParms traverseParms;

		private IntVec3 destinationCell;

		private int moveTicksCardinal;

		private int moveTicksDiagonal;

		private ByteGrid avoidGrid;

		private Area allowedArea;

		private bool drafted;

		private Func<int, int, float> preciseRegionLinkDistancesDistanceGetter;

		private Dictionary<int, VehicleRegionLink> regionMinLink = new Dictionary<int, VehicleRegionLink>();

		private Dictionary<VehicleRegionLink, int> distances = new Dictionary<VehicleRegionLink, int>();

		private FastPriorityQueue<RegionLinkQueueEntry> queue = new FastPriorityQueue<RegionLinkQueueEntry>(new DistanceComparer());

		private Dictionary<VehicleRegion, int> minPathCosts = new Dictionary<VehicleRegion, int>();

		private List<Pair<VehicleRegionLink, int>> preciseRegionLinkDistances = new List<Pair<VehicleRegionLink, int>>();

		private Dictionary<VehicleRegionLink, IntVec3> linkTargetCells = new Dictionary<VehicleRegionLink, IntVec3>();

		public VehicleRegionCostCalculator(Map map)
		{
			this.map = map;
			mapE = map.GetComponent<VehicleMapping>();
			preciseRegionLinkDistancesDistanceGetter = new Func<int, int, float>(PreciseRegionLinkDistancesDistanceGetter);
		}

		public void Init(CellRect destination, HashSet<VehicleRegion> destRegions, TraverseParms parms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted)
		{
			regionGrid = mapE.VehicleRegionGrid.DirectGrid;
			traverseParms = parms;
			destinationCell = destination.CenterCell;
			this.moveTicksCardinal = moveTicksCardinal;
			this.moveTicksDiagonal = moveTicksDiagonal;
			this.avoidGrid = avoidGrid;
			this.allowedArea = allowedArea;
			this.drafted = drafted;
			regionMinLink.Clear();
			distances.Clear();
			linkTargetCells.Clear();
			queue.Clear();
			minPathCosts.Clear();

			foreach (VehicleRegion region in destRegions)
			{
				int minPathCost = RegionMedianPathCost(region);
				foreach (VehicleRegionLink regionLink in region.links)
				{
					if (regionLink.GetOtherRegion(region).Allows(traverseParms, false))
					{
						int num = RegionLinkDistance(destinationCell, regionLink, minPathCost);
						if(distances.TryGetValue(regionLink, out int num2))
						{
							if(num < num2)
							{
								linkTargetCells[regionLink] = GetLinkTargetCell(destinationCell, regionLink);
							}
							num = Math.Min(num2, num);
						}
						else
						{
							linkTargetCells[regionLink] = GetLinkTargetCell(destinationCell, regionLink);
						}
						distances[regionLink] = num;
					}
				}
				GetPreciseRegionLinkDistances(region, destination, preciseRegionLinkDistances);
				for(int i = 0; i < preciseRegionLinkDistances.Count; i++)
				{
					Pair<VehicleRegionLink, int> pair = preciseRegionLinkDistances[i];
					VehicleRegionLink first = pair.First;
					int num3 = distances[first];
					int num4;
					if(pair.Second > num3)
					{
						distances[first] = pair.Second;
						num4 = pair.Second;
					}
					else
					{
						num4 = num3;
					}
					queue.Push(new RegionLinkQueueEntry(region, first, num4, num4));
				}
			}
		}

		public int GetRegionDistance(VehicleRegion region, out VehicleRegionLink minLink)
		{
			if (regionMinLink.TryGetValue(region.id, out minLink))
			{
				return distances[minLink];
			}
			while(queue.Count != 0)
			{
				RegionLinkQueueEntry regionLinkQueueEntry = queue.Pop();
				int num = distances[regionLinkQueueEntry.Link];
				if(regionLinkQueueEntry.Cost == num)
				{
					VehicleRegion otherRegion = regionLinkQueueEntry.Link.GetOtherRegion(regionLinkQueueEntry.From);
					if(!(otherRegion is null) && otherRegion.valid)
					{
						int num2 = 0;
						if(!(otherRegion.door is null))
						{
							num2 = VehiclePathFinder.GetBuildingCost(otherRegion.door, traverseParms, traverseParms.pawn);
							if (num2 == int.MaxValue) continue;
							num2 += OctileDistance(1, 0);
						}
						int minPathCost = RegionMedianPathCost(otherRegion);
						foreach(VehicleRegionLink regionLink in otherRegion.links)
						{
							if(regionLink != regionLinkQueueEntry.Link && regionLink.GetOtherRegion(otherRegion).type.Passable())
							{
								int num3 = (otherRegion.door is null) ? RegionLinkDistance(regionLinkQueueEntry.Link, regionLink, minPathCost) : num2;
								num3 = Math.Max(num3, 1);
								int num4 = num + num3;
								int estimatedPathCost = MinimumRegionLinkDistance(destinationCell, regionLink) + num4;
								if (distances.TryGetValue(regionLink, out int num5))
								{
									if(num4 < num5)
									{
										distances[regionLink] = num4;
										queue.Push(new RegionLinkQueueEntry(otherRegion, regionLink, num4, estimatedPathCost));
									}
								}
								else
								{
									distances.Add(regionLink, num4);
									queue.Push(new RegionLinkQueueEntry(otherRegion, regionLink, num4, estimatedPathCost));
								}
							}
						}
						if (!regionMinLink.ContainsKey(otherRegion.id))
						{
							regionMinLink.Add(otherRegion.id, regionLinkQueueEntry.Link);
							if(otherRegion == region)
							{
								minLink = regionLinkQueueEntry.Link;
								return regionLinkQueueEntry.Cost;
							}
						}
					}
				}
			}
			return 10000;
		}

		public int GetRegionBestDistances(VehicleRegion region, out VehicleRegionLink bestLink, out VehicleRegionLink secondBestLink, out int secondBestCost)
		{
			int regionDistance = GetRegionDistance(region, out bestLink);
			secondBestLink = null;
			secondBestCost = int.MaxValue;
			foreach (VehicleRegionLink regionLink in region.links)
			{
				if(regionLink != bestLink && regionLink.GetOtherRegion(region).type.Passable())
				{
					if (distances.TryGetValue(regionLink, out int num) && num < secondBestCost)
					{
						secondBestCost = num;
						secondBestLink = regionLink;
					}
				}
			}
			return regionDistance;
		}

		public int RegionMedianPathCost(VehicleRegion region)
		{
			if (minPathCosts.TryGetValue(region, out int result))
			{
				return result;
			}
			bool ignoreAllowedAreaCost = allowedArea != null && region.OverlapWith(allowedArea) != AreaOverlap.None;
			CellIndices cellIndices = map.cellIndices;
			Rand.PushState();
			Rand.Seed = cellIndices.CellToIndex(region.extentsClose.CenterCell) * (region.links.Count + 1);
			for(int i = 0; i < SampleCount; i++)
			{
				pathCostSamples[i] = GetCellCostFast(cellIndices.CellToIndex(region.RandomCell), ignoreAllowedAreaCost);
			}
			Rand.PopState();
			Array.Sort(pathCostSamples);
			int num = pathCostSamples[4];
			minPathCosts[region] = num;
			return num;
		}

		private int GetCellCostFast(int index, bool ignoreAllowedAreaCost = false)
		{
			int num = mapE.VehiclePathGrid.pathGrid[index];
			num += !(avoidGrid is null) ? (avoidGrid[index] * 8) : 0;
			num += (!(allowedArea is null) && !ignoreAllowedAreaCost && !allowedArea[index]) ? 600 : 0;
			num += drafted ? map.terrainGrid.topGrid[index].extraDraftedPerceivedPathCost : map.terrainGrid.topGrid[index].extraNonDraftedPerceivedPathCost;
			return num;
		}

		private int RegionLinkDistance(VehicleRegionLink a, VehicleRegionLink b, int minPathCost)
		{
			IntVec3 a2 = (!linkTargetCells.ContainsKey(a)) ? RegionLinkCenter(a) : linkTargetCells[a];
			IntVec3 b2 = (!linkTargetCells.ContainsKey(b)) ? RegionLinkCenter(b) : linkTargetCells[b];
			IntVec3 intVec = a2 - b2;
			int num = Math.Abs(intVec.x);
			int num2 = Math.Abs(intVec.z);
			return OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
		}

		public int RegionLinkDistance(IntVec3 cell, VehicleRegionLink link, int minPathCost)
		{
			IntVec3 linkTargetCell = GetLinkTargetCell(cell, link);
			IntVec3 intVec = cell - linkTargetCell;
			int num = Math.Abs(intVec.x);
			int num2 = Math.Abs(intVec.z);
			return OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
		}

		private static int SpanCenterX(EdgeSpan e)
		{
			return e.root.x + ((e.dir != SpanDirection.East) ? 0 : (e.length / 2));
		}

		private static int SpanCenterZ(EdgeSpan e)
		{
			return e.root.z + ((e.dir != SpanDirection.North) ? 0 : (e.length / 2));
		}

		private static IntVec3 RegionLinkCenter(VehicleRegionLink link)
		{
			return new IntVec3(SpanCenterX(link.span), 0, SpanCenterZ(link.span));
		}

		private int MinimumRegionLinkDistance(IntVec3 cell, VehicleRegionLink link)
		{
			IntVec3 intVec = cell - LinkClosestCell(cell, link);
			return OctileDistance(Math.Abs(intVec.x), Math.Abs(intVec.z));
		}

		private int OctileDistance(int dx, int dz)
		{
			return GenMath.OctileDistance(dx, dz, moveTicksCardinal, moveTicksDiagonal);
		}

		private IntVec3 GetLinkTargetCell(IntVec3 cell, VehicleRegionLink link)
		{
			return LinkClosestCell(cell, link);
		}

		private static IntVec3 LinkClosestCell(IntVec3 cell, VehicleRegionLink link)
		{
			EdgeSpan span = link.span;
			int num = 0;
			int num2 = 0;
			if(span.dir == SpanDirection.North)
			{
				num2 = span.length - 1;
			}
			else
			{
				num = span.length - 1;
			}

			IntVec3 root = span.root;
			return new IntVec3(Mathf.Clamp(cell.x, root.x, root.x + num), 0, Mathf.Clamp(cell.z, root.z, root.z + num2));
		}

		private void GetPreciseRegionLinkDistances(VehicleRegion region, CellRect destination, List<Pair<VehicleRegionLink, int>> outDistances)
		{
			outDistances.Clear();
			tmpCellIndices.Clear();
			if(destination.Width == 1 && destination.Height == 1)
			{
				tmpCellIndices.Add(map.cellIndices.CellToIndex(destination.CenterCell));
			}
			else
			{
				foreach (IntVec3 cell in destination)
				{
					if (cell.InBoundsShip(map))
					{
						tmpCellIndices.Add(map.cellIndices.CellToIndex(cell));
					}
				}
			}
			Dijkstra<int>.Run(tmpCellIndices, (int x) => PreciseRegionLinkDistancesNeighborsGetter(x, region),
				preciseRegionLinkDistancesDistanceGetter, tmpDistances, null);
			foreach (VehicleRegionLink regionLink in region.links)
			{
				if (regionLink.GetOtherRegion(region).Allows(traverseParms, false))
				{
					if(!tmpDistances.TryGetValue(map.cellIndices.CellToIndex(linkTargetCells[regionLink]), out float num))
					{
						Log.ErrorOnce("Dijkstra couldn't reach one of the cells even though they are in the same region. There is most likely something wrong with the " +
							"neighbor nodes getter. Error occurred in ShipPathFinder of Vehicles", 1938471531);
						num = 100f;
					}
					outDistances.Add(new Pair<VehicleRegionLink, int>(regionLink, (int)num));
				}
			}
		}

		private IEnumerable<int> PreciseRegionLinkDistancesNeighborsGetter(int node, VehicleRegion region)
		{
			if (regionGrid[node] is null || regionGrid[node] != region)
				return null;
			return PathableNeighborIndices(node);
		}

		private float PreciseRegionLinkDistancesDistanceGetter(int a, int b)
		{
			return (GetCellCostFast(b, false) + ((!AreCellsDiagonal(a, b)) ? moveTicksCardinal : moveTicksDiagonal));
		}

		private bool AreCellsDiagonal(int a, int b)
		{
			int x = map.Size.x;
			return a % x != b % x && a / x != b / x;
		}

		private List<int> PathableNeighborIndices(int index)
		{
			tmpPathableNeighborIndices.Clear();
			VehiclePathGrid pathGrid = mapE.VehiclePathGrid;
			int x = map.Size.x;
			bool flag = index % x > 0;
			bool flag2 = index % x < x - 1;
			bool flag3 = index >= x;
			bool flag4 = index / x < map.Size.z - 1;
			if(flag3 && pathGrid.WalkableFast(index - x))
			{
				tmpPathableNeighborIndices.Add(index - x);
			}
			if(flag2 && pathGrid.WalkableFast(index + 1))
			{
				tmpPathableNeighborIndices.Add(index + 1);
			}
			if(flag && pathGrid.WalkableFast(index - 1))
			{
				tmpPathableNeighborIndices.Add(index - 1);
			}
			if(flag4 && pathGrid.WalkableFast(index + x))
			{
				tmpPathableNeighborIndices.Add(index + x);
			}
			bool flag5 = !flag || VehiclePathFinder.BlocksDiagonalMovement(map, index - 1);
			bool flag6 = !flag2 || VehiclePathFinder.BlocksDiagonalMovement(map, index + 1);
			if(flag3 && !VehiclePathFinder.BlocksDiagonalMovement(map, index - x))
			{
				if(!flag6 && pathGrid.WalkableFast(index - x + 1))
				{
					tmpPathableNeighborIndices.Add(index - x + 1);
				}
				if(!flag5 && pathGrid.WalkableFast(index - x - 1))
				{
					tmpPathableNeighborIndices.Add(index - x - 1);
				}
			}
			if(flag4 && !VehiclePathFinder.BlocksDiagonalMovement(map, index + x))
			{
				if(!flag6 && pathGrid.WalkableFast(index + x + 1))
				{
					tmpPathableNeighborIndices.Add(index + x + 1);
				}
				if(!flag5 && pathGrid.WalkableFast(index + x - 1))
				{
					tmpPathableNeighborIndices.Add(index + x - 1);
				}
			}
			return tmpPathableNeighborIndices;
		}

		private struct RegionLinkQueueEntry
		{
			private readonly VehicleRegion from;

			private readonly VehicleRegionLink link;

			private readonly int cost;

			private readonly int estimatedPathCost;

			public RegionLinkQueueEntry(VehicleRegion from, VehicleRegionLink link, int cost, int estimatedPathCost)
			{
				this.from = from;
				this.link = link;
				this.cost = cost;
				this.estimatedPathCost = estimatedPathCost;
			}

			public VehicleRegion From => from;

			public VehicleRegionLink Link => link;

			public int Cost => cost;

			public int EstimatedPathCost => estimatedPathCost;
		}

		private class DistanceComparer : IComparer<RegionLinkQueueEntry>
		{
			public int Compare(RegionLinkQueueEntry a, RegionLinkQueueEntry b)
			{
				return a.EstimatedPathCost.CompareTo(b.EstimatedPathCost);
			}
		}
	}
}
