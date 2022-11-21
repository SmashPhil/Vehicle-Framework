using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region cost calculator
	/// </summary>
	public class VehicleRegionCostCalculator
	{
		private const int SampleCount = 11;

		private static int[] pathCostSamples = new int[SampleCount];

		private static readonly List<int> tmpCellIndices = new List<int>();
		private static readonly List<int> tmpPathableNeighborIndices = new List<int>();
		private static readonly Dictionary<int, float> tmpDistances = new Dictionary<int, float>();

		private readonly VehicleMapping mapping;
		private readonly VehicleDef vehicleDef;

		private VehicleRegion[] regionGrid;
		private ByteGrid avoidGrid;

		private TraverseParms traverseParms;
		private IntVec3 destinationCell;

		private int moveTicksCardinal;
		private int moveTicksDiagonal;
		private bool drafted;

		private Func<int, int, float> preciseRegionLinkDistancesDistanceGetter;

		private readonly Dictionary<int, VehicleRegionLink> regionMinLink = new Dictionary<int, VehicleRegionLink>();
		private readonly Dictionary<VehicleRegionLink, int> distances = new Dictionary<VehicleRegionLink, int>();
		private readonly Dictionary<VehicleRegionLink, IntVec3> linkTargetCells = new Dictionary<VehicleRegionLink, IntVec3>();
		private readonly Dictionary<VehicleRegion, int> minPathCosts = new Dictionary<VehicleRegion, int>();

		private readonly FastPriorityQueue<RegionLinkQueueEntry> queue = new FastPriorityQueue<RegionLinkQueueEntry>(new DistanceComparer());

		private readonly List<Pair<VehicleRegionLink, int>> preciseRegionLinkDistances = new List<Pair<VehicleRegionLink, int>>();

		public VehicleRegionCostCalculator(VehicleMapping mapping, VehicleDef vehicleDef)
		{
			this.mapping = mapping;
			this.vehicleDef = vehicleDef;
			preciseRegionLinkDistancesDistanceGetter = new Func<int, int, float>(PreciseRegionLinkDistancesDistanceGetter);
		}

		/// <summary>
		/// Initialize region cost calculation between region to region
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="destRegions"></param>
		/// <param name="parms"></param>
		/// <param name="moveTicksCardinal"></param>
		/// <param name="moveTicksDiagonal"></param>
		/// <param name="avoidGrid"></param>
		/// <param name="drafted"></param>
		public void Init(CellRect destination, HashSet<VehicleRegion> destRegions, TraverseParms parms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, bool drafted)
		{
			regionGrid = mapping[vehicleDef].VehicleRegionGrid.DirectGrid;
			traverseParms = parms;
			destinationCell = destination.CenterCell;
			this.moveTicksCardinal = moveTicksCardinal;
			this.moveTicksDiagonal = moveTicksDiagonal;
			this.avoidGrid = avoidGrid;
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

		/// <summary>
		/// Calculate distance between <paramref name="region"/> and <see cref="destinationCell"/>
		/// </summary>
		/// <param name="region"></param>
		/// <param name="minLink"></param>
		public int GetRegionDistance(VehicleRegion region, out VehicleRegionLink minLink)
		{
			if (regionMinLink.TryGetValue(region.id, out minLink))
			{
				return distances[minLink];
			}
			while (queue.Count != 0)
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
			return VehiclePathGrid.ImpassableCost;
		}

		/// <summary>
		/// Retrieve best heuristic cost region links from <paramref name="region"/> and neighboring regions
		/// </summary>
		/// <param name="region"></param>
		/// <param name="bestLink"></param>
		/// <param name="secondBestLink"></param>
		/// <param name="secondBestCost"></param>
		public int GetRegionBestDistances(VehicleRegion region, out VehicleRegionLink bestLink, out VehicleRegionLink secondBestLink, out int secondBestCost)
		{
			int regionDistance = GetRegionDistance(region, out bestLink);
			secondBestLink = null;
			secondBestCost = int.MaxValue;
			foreach (VehicleRegionLink regionLink in region.links)
			{
				if (regionLink != bestLink && regionLink.GetOtherRegion(region).type.Passable())
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

		/// <summary>
		/// Calculate approximate cell cost between region links
		/// </summary>
		/// <param name="region"></param>
		public int RegionMedianPathCost(VehicleRegion region)
		{
			if (minPathCosts.TryGetValue(region, out int result))
			{
				return result;
			}
			CellIndices cellIndices = mapping.map.cellIndices;
			Rand.PushState();
			Rand.Seed = cellIndices.CellToIndex(region.extentsClose.CenterCell) * (region.links.Count + 1);
			for(int i = 0; i < SampleCount; i++)
			{
				pathCostSamples[i] = GetCellCostFast(cellIndices.CellToIndex(region.RandomCell));
			}
			Rand.PopState();
			Array.Sort(pathCostSamples);
			int num = pathCostSamples[4];
			minPathCosts[region] = num;
			return num;
		}

		/// <summary>
		/// Fast calculate cell cost from path grid and avoid grid
		/// </summary>
		/// <param name="index"></param>
		private int GetCellCostFast(int index)
		{
			int num = mapping[vehicleDef].VehiclePathGrid.pathGrid[index];
			if (avoidGrid != null)
			{
				num += avoidGrid[index] * 8;
			}
			num += drafted ? mapping.map.terrainGrid.topGrid[index].extraDraftedPerceivedPathCost : mapping.map.terrainGrid.topGrid[index].extraNonDraftedPerceivedPathCost;
			return num;
		}

		/// <summary>
		/// Distance between region links <paramref name="a"/> and <paramref name="b"/>
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="minPathCost"></param>
		private int RegionLinkDistance(VehicleRegionLink a, VehicleRegionLink b, int minPathCost)
		{
			IntVec3 a2 = (!linkTargetCells.ContainsKey(a)) ? RegionLinkCenter(a) : linkTargetCells[a];
			IntVec3 b2 = (!linkTargetCells.ContainsKey(b)) ? RegionLinkCenter(b) : linkTargetCells[b];
			IntVec3 intVec = a2 - b2;
			int num = Math.Abs(intVec.x);
			int num2 = Math.Abs(intVec.z);
			return OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
		}

		/// <summary>
		/// Distance between <paramref name="cell"/> and <paramref name="link"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="link"></param>
		/// <param name="minPathCost"></param>
		public int RegionLinkDistance(IntVec3 cell, VehicleRegionLink link, int minPathCost)
		{
			IntVec3 linkTargetCell = GetLinkTargetCell(cell, link);
			IntVec3 intVec = cell - linkTargetCell;
			int num = Math.Abs(intVec.x);
			int num2 = Math.Abs(intVec.z);
			return OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
		}

		/// <summary>
		/// Span centered on X axis
		/// </summary>
		/// <param name="e"></param>
		private static int SpanCenterX(EdgeSpan e)
		{
			return e.root.x + ((e.dir != SpanDirection.East) ? 0 : (e.length / 2));
		}

		/// <summary>
		/// Span centered on Z axis
		/// </summary>
		/// <param name="e"></param>
		private static int SpanCenterZ(EdgeSpan e)
		{
			return e.root.z + ((e.dir != SpanDirection.North) ? 0 : (e.length / 2));
		}

		/// <summary>
		/// Center of region link <paramref name="link"/>
		/// </summary>
		/// <param name="link"></param>
		private static IntVec3 RegionLinkCenter(VehicleRegionLink link)
		{
			return new IntVec3(SpanCenterX(link.span), 0, SpanCenterZ(link.span));
		}

		/// <summary>
		/// Minimum distance from <paramref name="cell"/> to <paramref name="link"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="link"></param>
		private int MinimumRegionLinkDistance(IntVec3 cell, VehicleRegionLink link)
		{
			IntVec3 intVec = cell - LinkClosestCell(cell, link);
			return OctileDistance(Math.Abs(intVec.x), Math.Abs(intVec.z));
		}

		/// <summary>
		/// Octile distance with precalculated ticks cardinal and diagonal
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dz"></param>
		private int OctileDistance(int dx, int dz)
		{
			return GenMath.OctileDistance(dx, dz, moveTicksCardinal, moveTicksDiagonal);
		}

		/// <summary>
		/// Wrapper for static method <see cref="LinkClosestCell(IntVec3, VehicleRegionLink)"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="link"></param>
		private IntVec3 GetLinkTargetCell(IntVec3 cell, VehicleRegionLink link)
		{
			return LinkClosestCell(cell, link);
		}

		/// <summary>
		/// Closest cell to <paramref name="cell"/> within <paramref name="link"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="link"></param>
		private static IntVec3 LinkClosestCell(IntVec3 cell, VehicleRegionLink link)
		{
			EdgeSpan span = link.span;
			int num = 0;
			int num2 = 0;
			if (span.dir == SpanDirection.North)
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

		/// <summary>
		/// Calculate exact distance from <paramref name="region"/> to <paramref name="destination"/> split up by region links
		/// </summary>
		/// <param name="region"></param>
		/// <param name="destination"></param>
		/// <param name="outDistances"></param>
		private void GetPreciseRegionLinkDistances(VehicleRegion region, CellRect destination, List<Pair<VehicleRegionLink, int>> outDistances)
		{
			outDistances.Clear();
			tmpCellIndices.Clear();
			if(destination.Width == 1 && destination.Height == 1)
			{
				tmpCellIndices.Add(mapping.map.cellIndices.CellToIndex(destination.CenterCell));
			}
			else
			{
				foreach (IntVec3 cell in destination)
				{
					if (cell.InBounds(mapping.map))
					{
						tmpCellIndices.Add(mapping.map.cellIndices.CellToIndex(cell));
					}
				}
			}
			Dijkstra<int>.Run(tmpCellIndices, (int x) => PreciseRegionLinkDistancesNeighborsGetter(x, region), preciseRegionLinkDistancesDistanceGetter, tmpDistances, null);
			foreach (VehicleRegionLink regionLink in region.links)
			{
				if (regionLink.GetOtherRegion(region).Allows(traverseParms, false))
				{
					if(!tmpDistances.TryGetValue(mapping.map.cellIndices.CellToIndex(linkTargetCells[regionLink]), out float num))
					{
						Log.ErrorOnce("Dijkstra couldn't reach one of the cells even though they are in the same region. There is most likely something wrong with the " +
							"neighbor nodes getter.", vehicleDef.GetHashCode() ^ "VehiclesDijkstraRegionLinkDistanceCalculator".GetHashCode());
						num = 100f;
					}
					outDistances.Add(new Pair<VehicleRegionLink, int>(regionLink, (int)num));
				}
			}
		}

		/// <summary>
		/// Retrieve pathable cell indices
		/// </summary>
		/// <param name="node"></param>
		/// <param name="region"></param>
		private IEnumerable<int> PreciseRegionLinkDistancesNeighborsGetter(int node, VehicleRegion region)
		{
			if (regionGrid[node] is null || regionGrid[node] != region) return null;
			return PathableNeighborIndices(node);
		}

		/// <summary>
		/// Cell cost relevant to cell -> cell direction
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		private float PreciseRegionLinkDistancesDistanceGetter(int a, int b)
		{
			int moveTicks = !AreCellsDiagonal(a, b) ? moveTicksCardinal : moveTicksDiagonal;
			return GetCellCostFast(b) + moveTicks;
		}

		/// <summary>
		/// Determine if cell indecies <paramref name="a"/> and <paramref name="b"/> are diagonal to each other
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		private bool AreCellsDiagonal(int a, int b)
		{
			int x = mapping.map.Size.x;
			return a % x != b % x && a / x != b / x;
		}

		/// <summary>
		/// Retrieve all pathable cell indices
		/// </summary>
		/// <param name="index"></param>
		private List<int> PathableNeighborIndices(int index)
		{
			tmpPathableNeighborIndices.Clear();
			VehiclePathGrid pathGrid = mapping[vehicleDef].VehiclePathGrid;
			int x = mapping.map.Size.x;
			bool flag = index % x > 0;
			bool flag2 = index % x < x - 1;
			bool flag3 = index >= x;
			bool flag4 = index / x < mapping.map.Size.z - 1;
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
			bool flag5 = !flag || VehiclePathFinder.BlocksDiagonalMovement(mapping.map, vehicleDef, index - 1);
			bool flag6 = !flag2 || VehiclePathFinder.BlocksDiagonalMovement(mapping.map, vehicleDef, index + 1);
			if(flag3 && !VehiclePathFinder.BlocksDiagonalMovement(mapping.map, vehicleDef, index - x))
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
			if(flag4 && !VehiclePathFinder.BlocksDiagonalMovement(mapping.map, vehicleDef, index + x))
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

		/// <summary>
		/// Queue handling for regions and region links
		/// </summary>
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

		/// <summary>
		/// Comparer using approximate path cost between region links
		/// </summary>
		private class DistanceComparer : IComparer<RegionLinkQueueEntry>
		{
			public int Compare(RegionLinkQueueEntry a, RegionLinkQueueEntry b)
			{
				return a.EstimatedPathCost.CompareTo(b.EstimatedPathCost);
			}
		}
	}
}
