using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class WorldOceanPathFinder : WorldComponent
    {
        public WorldOceanPathFinder(World world) : base(world)
        {
            this.world = world;
            calcGrid = new WorldOceanPathFinder.PathFinderNodeFast[Find.WorldGrid.TilesCount];
        }

        public WorldPath FindOceanPath(int startTile, int destTile, Caravan caravan, Func<float, bool> terminator = null)
        {
            if(ShipHarmony.debug)
            {
                Log.Message("==========");
                Log.Message("Finding Path");
                Log.Message("Caravan: " + caravan?.LabelShort + " | " + (caravan is null));
                Log.Message("Terminator: " + (terminator is null));
                Log.Message("Planner: " + ShipHarmony.routePlannerActive);
                Log.Message("==========");
            }

            if (startTile < 0)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid start tile ",
					startTile,
					", caravan= ",
					caravan
				}), false);
				return WorldPath.NotFound;
			}
			if (destTile < 0)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid dest tile ",
					destTile,
					", caravan= ",
					caravan
				}), false);
				return WorldPath.NotFound;
			}
			if (caravan != null)
			{
				if (!caravan.CanReach(destTile))
				{
					return WorldPath.NotFound;
				}
			}
			else if (!Find.WorldReachability.CanReach(startTile, destTile))
			{
				return WorldPath.NotFound;
			}
			World world = Find.World;
			WorldGrid grid = world.grid;
			List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
			List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
			Vector3 normalized = grid.GetTileCenter(destTile).normalized;
			float[] movementDifficulty = world.pathGrid.movementDifficulty;
			int num = 0;
			int num2 = (caravan != null) ? caravan.TicksPerMove : 3300;
			int num3 = CalculateHeuristicStrength(startTile, destTile);
			statusOpenValue += 2;
			statusClosedValue += 2;
			if (statusClosedValue >= 65435)
			{
				ResetStatuses();
			}
			calcGrid[startTile].knownCost = 0;
			calcGrid[startTile].heuristicCost = 0;
			calcGrid[startTile].costNodeCost = 0;
			calcGrid[startTile].parentTile = startTile;
			calcGrid[startTile].status = statusOpenValue;
			openList.Clear();
			openList.Push(new CostNode(startTile, 0));
			while (openList.Count > 0)
			{
				CostNode costNode = openList.Pop();
				if (costNode.cost == calcGrid[costNode.tile].costNodeCost)
				{
					int tile = costNode.tile;
					if (calcGrid[tile].status != statusClosedValue)
					{
						if (tile == destTile)
						{
							return FinalizedPath(tile);
						}
						if (num > 500000)
						{
							Log.Warning(string.Concat(new object[]
							{
								caravan,
								" pathing from ",
								startTile,
								" to ",
								destTile,
								" hit search limit of ",
								500000,
								" tiles."
							}), false);
							return WorldPath.NotFound;
						}
						int num4 = (tile + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[tile + 1] : tileIDToNeighbors_values.Count;
						for (int i = tileIDToNeighbors_offsets[tile]; i < num4; i++)
						{
							int num5 = tileIDToNeighbors_values[i];
							if (calcGrid[num5].status != statusClosedValue && !HelperMethods.ImpassableModified(world, num5, startTile, destTile, caravan))
							{
                                int num6 = (int)(num2 * movementDifficulty[num5] + calcGrid[tile].knownCost);
								ushort status = calcGrid[num5].status;
								if ((status != statusClosedValue && status != statusOpenValue) || calcGrid[num5].knownCost > num6)
								{
									Vector3 tileCenter = grid.GetTileCenter(num5);
									if (status != statusClosedValue && status != statusOpenValue)
									{
										float num7 = grid.ApproxDistanceInTiles(GenMath.SphericalDistance(tileCenter.normalized, normalized));
										calcGrid[num5].heuristicCost = Mathf.RoundToInt(num2 * num7 * num3 * 0.5f);
									}
									int num8 = num6 + calcGrid[num5].heuristicCost;
									calcGrid[num5].parentTile = tile;
									calcGrid[num5].knownCost = num6;
									calcGrid[num5].status = statusOpenValue;
									calcGrid[num5].costNodeCost = num8;
									openList.Push(new CostNode(num5, num8));
								}
							}
						}
						num++;
						calcGrid[tile].status = statusClosedValue;
						if (terminator != null && terminator((float)calcGrid[tile].costNodeCost))
						{
							return WorldPath.NotFound;
						}
					}
				}
			}
			Log.Warning(string.Concat(new object[]
			{
				caravan,
				" pathing from ",
				startTile,
				" to ",
				destTile,
				" ran out of tiles to process."
			}), false);
			return WorldPath.NotFound;
        }

        public void FloodPathsWithCost(List<int> startTiles, Func<int, int, int> costFunc, Func<int, bool> impassable = null, Func<int, float, bool> terminator = null)
		{
			if (startTiles.Count < 1 || startTiles.Contains(-1))
			{
				Log.Error("Tried to FindPath with invalid start tiles", false);
				return;
			}
			World world = Find.World;
			WorldGrid grid = world.grid;
			List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
			List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
			if (impassable == null)
			{
				impassable = ((int tid) => world.Impassable(tid));
			}
			statusOpenValue += 2;
			statusClosedValue += 2;
			if (statusClosedValue >= 65435)
			{
				ResetStatuses();
			}
			openList.Clear();
			using (List<int>.Enumerator enumerator = startTiles.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					int num = enumerator.Current;
					calcGrid[num].knownCost = 0;
					calcGrid[num].costNodeCost = 0;
					calcGrid[num].parentTile = num;
					calcGrid[num].status = statusOpenValue;
					openList.Push(new CostNode(num, 0));
				}
				goto IL_2F2;
			}
			IL_127:
			CostNode costNode = openList.Pop();
			if (costNode.cost == calcGrid[costNode.tile].costNodeCost)
			{
				int tile = costNode.tile;
				if (calcGrid[tile].status != statusClosedValue)
				{
					int num2 = (tile + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[tile + 1] : tileIDToNeighbors_values.Count;
					for (int i = tileIDToNeighbors_offsets[tile]; i < num2; i++)
					{
						int num3 = tileIDToNeighbors_values[i];
						if (calcGrid[num3].status != statusClosedValue && !impassable(num3))
						{
							int num4 = costFunc(tile, num3) + calcGrid[tile].knownCost;
							ushort status = calcGrid[num3].status;
							if ((status != statusClosedValue && status != statusOpenValue) || calcGrid[num3].knownCost > num4)
							{
								int num5 = num4;
								calcGrid[num3].parentTile = tile;
								calcGrid[num3].knownCost = num4;
								calcGrid[num3].status = this.statusOpenValue;
								calcGrid[num3].costNodeCost = num5;
								openList.Push(new CostNode(num3, num5));
							}
						}
					}
					calcGrid[tile].status = statusClosedValue;
					if (terminator != null && terminator(tile, (float)calcGrid[tile].costNodeCost))
					{
						return;
					}
				}
			}
			IL_2F2:
			if (openList.Count > 0)
			{
				goto IL_127;
			}
		}

        public List<int>[] FloodPathsWithCostForTree(List<int> startTiles, Func<int, int, int> costFunc, Func<int, bool> impassable = null, Func<int, float, bool> terminator = null)
		{
			FloodPathsWithCost(startTiles, costFunc, impassable, terminator);
			WorldGrid grid = Find.World.grid;
			List<int>[] array = new List<int>[grid.TilesCount];
			for (int i = 0; i < grid.TilesCount; i++)
			{
				if (calcGrid[i].status == statusClosedValue)
				{
					int parentTile = calcGrid[i].parentTile;
					if (parentTile != i)
					{
						if (array[parentTile] == null)
						{
							array[parentTile] = new List<int>();
						}
						array[parentTile].Add(i);
					}
				}
			}
			return array;
		}

		private WorldPath FinalizedPath(int lastTile)
		{
            WorldPath emptyWorldPath = Find.WorldPathPool.GetEmptyWorldPath();
			int num = lastTile;
			for (;;)
			{
				int parentTile = this.calcGrid[num].parentTile;
				int num2 = num;
				emptyWorldPath.AddNodeAtStart(num2);
				if (num2 == parentTile)
				{
					break;
				}
				num = parentTile;
			}
			emptyWorldPath.SetupFound((float)calcGrid[lastTile].knownCost);
			return emptyWorldPath;
		}

		private void ResetStatuses()
		{
			int num = calcGrid.Length;
			for (int i = 0; i < num; i++)
			{
				calcGrid[i].status = 0;
			}
			statusOpenValue = 1;
			statusClosedValue = 2;
		}

        private int CalculateHeuristicStrength(int startTile, int destTile)
		{
			float x = Find.WorldGrid.ApproxDistanceInTiles(startTile, destTile);
			return Mathf.RoundToInt(HeuristicStrength_DistanceCurve.Evaluate(x));
		}

		private FastPriorityQueue<WorldOceanPathFinder.CostNode> openList;
		private PathFinderNodeFast[] calcGrid;
		private ushort statusOpenValue = 1;
		private ushort statusClosedValue = 2;
		private const int SearchLimit = 500000;
		private static readonly SimpleCurve HeuristicStrength_DistanceCurve = new SimpleCurve
		{
			{
				new CurvePoint(30f, 1f),
				true
			},
			{
				new CurvePoint(40f, 1.3f),
				true
			},
			{
				new CurvePoint(130f, 2f),
				true
			}
		};
		private const float BestRoadDiscount = 0.5f;
		private struct CostNode
		{
			public CostNode(int tile, int cost)
			{
				this.tile = tile;
				this.cost = cost;
			}
			public int tile;
			public int cost;
		}
		private struct PathFinderNodeFast
		{
			public int knownCost;
			public int heuristicCost;
			public int parentTile;
			public int costNodeCost;
			public ushort status;
		}
		private class CostNodeComparer : IComparer<WorldOceanPathFinder.CostNode>
		{
			public int Compare(WorldOceanPathFinder.CostNode a, WorldOceanPathFinder.CostNode b)
			{
				int cost = a.cost;
				int cost2 = b.cost;
				if (cost > cost2)
				{
					return 1;
				}
				if (cost < cost2)
				{
					return -1;
				}
				return 0;
			}
		}
    }
}
