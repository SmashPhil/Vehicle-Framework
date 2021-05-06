using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class WorldVehiclePathfinder : WorldComponent
	{
		private const int SearchLimit = 500000;
		private const float BestRoadDiscount = 0.5f;

		private FastPriorityQueue<CostNode> openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
		private PathFinderNodeFast[] calcGrid;
		private ushort statusOpenValue = 1;
		private ushort statusClosedValue = 2;
		
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
		

		public WorldVehiclePathfinder(World world) : base(world)
		{
			this.world = world;
			calcGrid = new PathFinderNodeFast[Find.WorldGrid.TilesCount];
			openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
		}

		public WorldPath FindPath(int startTile, int destTile, VehicleCaravan caravan, Func<float, bool> terminator = null)
		{
			if (startTile < 0)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid start tile ",
					startTile,
					", caravan= ",
					caravan
				}));
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
				}));
				return WorldPath.NotFound;
			}

			if (!Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(caravan, destTile))
			{
				return WorldPath.NotFound;
			}

			World world = Find.World;
			WorldGrid grid = world.grid;
			List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
			List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
			Vector3 normalized = grid.GetTileCenter(destTile).normalized;
			Dictionary<VehicleDef, float[]> movementDifficulty = Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().movementDifficulty;
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
						if (num > SearchLimit)
						{
							Log.Warning(string.Concat(new object[]
							{
								caravan,
								" pathing from ",
								startTile,
								" to ",
								destTile,
								" hit search limit of ",
								SearchLimit,
								" tiles."
							}));
							return WorldPath.NotFound;
						}
						int num4 = (tile + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[tile + 1] : tileIDToNeighbors_values.Count;
						for (int i = tileIDToNeighbors_offsets[tile]; i < num4; i++)
						{
							int num5 = tileIDToNeighbors_values[i];
							if (calcGrid[num5].status != statusClosedValue && caravan.UniqueVehicleDefsInCaravan().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(num5, v)) &&
								(!caravan.HasBoat() || !(Find.World.CoastDirectionAt(num5).IsValid && num5 != destTile)))
							{
								float highestTerrainCost = caravan.UniqueVehicleDefsInCaravan().Max(v => movementDifficulty[v][num5]);
								int num6 = (int)(num2 * highestTerrainCost * VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(caravan, tile, num5, null)) + calcGrid[tile].knownCost;
								ushort status = calcGrid[num5].status;
								if ((status != statusClosedValue && status != statusOpenValue) || calcGrid[num5].knownCost > num6)
								{
									Vector3 tileCenter = grid.GetTileCenter(num5);
									if (status != statusClosedValue && status != statusOpenValue)
									{
										float num7 = grid.ApproxDistanceInTiles(GenMath.SphericalDistance(tileCenter.normalized, normalized));
										calcGrid[num5].heuristicCost = Mathf.RoundToInt(num2 * num7 * num3 * BestRoadDiscount);
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
						if (terminator != null && terminator(calcGrid[tile].costNodeCost))
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
			}));
			return WorldPath.NotFound;
		}

		public WorldPath FindPath(int startTile, int destTile, List<VehiclePawn> vehicles, Func<float, bool> terminator = null)
		{
			if (startTile < 0)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid start tile ",
					startTile,
					", vehicles= ",
					vehicles
				}));
				return WorldPath.NotFound;
			}
			if (destTile < 0)
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to FindPath with invalid dest tile ",
					destTile,
					", vehicles= ",
					vehicles
				}));
				return WorldPath.NotFound;
			}

			if (!Find.World.GetCachedWorldComponent<WorldVehicleReachability>().CanReach(vehicles.UniqueVehicleDefsInList().ToList(), startTile, destTile))
			{
				return WorldPath.NotFound;
			}

			World world = Find.World;
			WorldGrid grid = world.grid;
			List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
			List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
			Vector3 normalized = grid.GetTileCenter(destTile).normalized;
			Dictionary<VehicleDef, float[]> movementDifficulty = Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().movementDifficulty;
			int num = 0;
			int num2 = 3300; //REDO
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
						if (num > SearchLimit)
						{
							Log.Warning(string.Concat(new object[]
							{
								vehicles,
								" pathing from ",
								startTile,
								" to ",
								destTile,
								" hit search limit of ",
								SearchLimit,
								" tiles."
							}));
							return WorldPath.NotFound;
						}
						int num4 = (tile + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[tile + 1] : tileIDToNeighbors_values.Count;
						for (int i = tileIDToNeighbors_offsets[tile]; i < num4; i++)
						{
							int num5 = tileIDToNeighbors_values[i];
							if (calcGrid[num5].status != statusClosedValue && vehicles.UniqueVehicleDefsInList().All(v => Find.World.GetCachedWorldComponent<WorldVehiclePathGrid>().Passable(num5, v)) &&
								(!vehicles.HasBoat() || !(Find.World.CoastDirectionAt(num5).IsValid && num5 != destTile)))
							{
								float highestTerrainCost = vehicles.UniqueVehicleDefsInList().Max(v => movementDifficulty[v][num5]);
								int num6 = (int)(num2 * highestTerrainCost * VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(vehicles.UniqueVehicleDefsInList().ToList(), tile, num5, null)) + calcGrid[tile].knownCost;
								ushort status = calcGrid[num5].status;
								if ((status != statusClosedValue && status != statusOpenValue) || calcGrid[num5].knownCost > num6)
								{
									Vector3 tileCenter = grid.GetTileCenter(num5);
									if (status != statusClosedValue && status != statusOpenValue)
									{
										float num7 = grid.ApproxDistanceInTiles(GenMath.SphericalDistance(tileCenter.normalized, normalized));
										calcGrid[num5].heuristicCost = Mathf.RoundToInt(num2 * num7 * num3 * BestRoadDiscount);
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
						if (terminator != null && terminator(calcGrid[tile].costNodeCost))
						{
							return WorldPath.NotFound;
						}
					}
				}
			}
			Log.Warning(string.Concat(new object[]
			{
				vehicles,
				" pathing from ",
				startTile,
				" to ",
				destTile,
				" ran out of tiles to process."
			}));
			return WorldPath.NotFound;
		}

		private WorldPath FinalizedPath(int lastTile)
		{
			WorldPath emptyWorldPath = Find.WorldPathPool.GetEmptyWorldPath();
			int num = lastTile;
			for (;;)
			{
				int parentTile = calcGrid[num].parentTile;
				int num2 = num;
				emptyWorldPath.AddNodeAtStart(num2);
				if (num2 == parentTile)
				{
					break;
				}
				num = parentTile;
			}
			emptyWorldPath.SetupFound(calcGrid[lastTile].knownCost);
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

		private class CostNodeComparer : IComparer<CostNode>
		{
			public int Compare(CostNode a, CostNode b)
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
