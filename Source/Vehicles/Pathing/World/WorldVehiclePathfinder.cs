using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Vehicle pathfinder for world map
	/// </summary>
	public class WorldVehiclePathfinder : WorldComponent
	{
		private const int SearchLimit = 500000;
		private const int HeuristicTickCost = 1200;

		private readonly FastPriorityQueue<CostNode> openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
		private readonly PathFinderNodeFast[] calcGrid;
		private readonly float[] tileCache;

		private TileFeatureLookup tileFeatureLookup;
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
			tileFeatureLookup ??= new TileFeatureLookup(Find.WorldGrid);

			tileFeatureLookup.RegisterAllFeatureTypes();
			tileCache = new float[tileFeatureLookup.TileCacheSize];

			Instance = this;
		}

		/// <summary>
		/// Singleton getter
		/// </summary>
		public static WorldVehiclePathfinder Instance { get; private set; }

		/// <summary>
		/// Clear tile cache unique to tile types
		/// </summary>
		private void ClearTileCache()
		{
			Array.Clear(tileCache, 0, tileCache.Length);
		}

		/// <summary>
		/// Retrieve cached cost for tile give biome and feature types
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="vehicleDefs"></param>
		private float TileTypeCost(int tile, List<VehicleDef> vehicleDefs)
		{
			float cost = tileCache[tileFeatureLookup.IndexFor(tile)];
			if (cost <= 0)
			{
				cost = vehicleDefs.NullOrEmpty() ? -1 : vehicleDefs.Max(vehicleDef => WorldVehiclePathGrid.Instance.movementDifficulty[vehicleDef.DefIndex][tile]);
				tileCache[tileFeatureLookup.IndexFor(tile)] = cost;
			}
			return cost;
		}

		/// <summary>
		/// Find path from <paramref name="startTile"/> to <paramref name="destTile"/> for <paramref name="caravan"/>
		/// </summary>
		/// <param name="startTile"></param>
		/// <param name="destTile"></param>
		/// <param name="caravan"></param>
		/// <param name="terminator"></param>
		public WorldPath FindPath(int startTile, int destTile, VehicleCaravan caravan, Func<float, bool> terminator = null)
		{
			return FindPath(startTile, destTile, caravan.AllVehicles(), caravan.TicksPerMove, terminator);
		}

		/// <summary>
		/// Find path from <paramref name="startTile"/> to <paramref name="destTile"/> for <paramref name="vehicles"/>
		/// </summary>
		/// <param name="startTile"></param>
		/// <param name="destTile"></param>
		/// <param name="vehicles"></param>
		/// <param name="terminator"></param>
		/// <param name="explanations"></param>
		public WorldPath FindPath(int startTile, int destTile, List<VehiclePawn> vehicles, int ticksPerMove = VehicleCaravanTicksPerMoveUtility.DefaultTicksPerMove, Func<float, bool> terminator = null)
		{
			return FindPath(startTile, destTile, vehicles.UniqueVehicleDefsInList(), ticksPerMove, terminator);
		}

		/// <summary>
		/// Find path from <paramref name="startTile"/> to <paramref name="destTile"/> for <paramref name="vehicleDefs"/>
		/// </summary>
		/// <param name="startTile"></param>
		/// <param name="destTile"></param>
		/// <param name="vehicleDefs"></param>
		/// <param name="terminator"></param>
		public WorldPath FindPath(int startTile, int destTile, List<VehicleDef> vehicleDefs, int ticksPerMove = VehicleCaravanTicksPerMoveUtility.DefaultTicksPerMove, Func<float, bool> terminator = null)
		{
			if (vehicleDefs.NullOrEmpty())
			{
				Log.Error($"Attempting to find path with no vehicles.");
				return WorldPath.NotFound;
			}
			ClearTileCache();
			try
			{
				string vehiclesPathing = string.Join(",", vehicleDefs.Select(v => v.defName));
				if (startTile < 0)
				{
					Log.Error($"Tried to FindPath with invalid startTile={startTile} vehicles={vehiclesPathing}");
					return WorldPath.NotFound;
				}
				if (destTile < 0)
				{
					Log.Error($"Tried to FindPath with invalid destTile={destTile} vehicles={vehiclesPathing}");
					return WorldPath.NotFound;
				}
				if (!vehicleDefs.All(vehicleDef => WorldVehicleReachability.Instance.CanReach(vehicleDef, startTile, destTile)))
				{
					return WorldPath.NotFound;
				}

				World world = Find.World;
				WorldGrid grid = world.grid;
				bool coastalTravel = vehicleDefs.All(v => v.properties.customBiomeCosts.ContainsKey(BiomeDefOf.Ocean));
				List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
				List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
				Vector3 normalized = grid.GetTileCenter(destTile).normalized;
				float bestRoadDiscount = DefDatabase<RoadDef>.AllDefsListForReading.Min(road => VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(vehicleDefs, road));
				int tilesSearched = 0;
				int heuristicStrength = CalculateHeuristicStrength(startTile, destTile);
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
							if (tilesSearched > SearchLimit)
							{
								Log.Warning($"{vehiclesPathing} pathing from {startTile} to {destTile}. Hit search limit of {SearchLimit} tiles.");
								return WorldPath.NotFound;
							}
							int neighborOffsetCount = (tile + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[tile + 1] : tileIDToNeighbors_values.Count;
							for (int i = tileIDToNeighbors_offsets[tile]; i < neighborOffsetCount; i++)
							{
								int neighbor = tileIDToNeighbors_values[i];
								if (calcGrid[neighbor].status != statusClosedValue)
								{
									bool allPassable = vehicleDefs.All(vehicleDef => WorldVehiclePathGrid.Instance.Passable(neighbor, vehicleDef));
									if (allPassable /*|| (coastalTravel && (neighbor == startTile || neighbor == destTile))*/)
									{
										float highestTerrainCost = TileTypeCost(neighbor, vehicleDefs);
										if (coastalTravel)
										{
											if (tile != startTile && neighbor != destTile)
											{
												highestTerrainCost = vehicleDefs.Max(vehicleDef => WorldVehiclePathGrid.ConsistentDirectionCost(tile, neighbor, vehicleDef));
											}
										}
										float roadMultiplier = VehicleCaravan_PathFollower.GetRoadMovementDifficultyMultiplier(vehicleDefs, tile, neighbor);
										int totalPathCost = (int)(ticksPerMove * highestTerrainCost * roadMultiplier) + calcGrid[tile].knownCost;
										ushort status = calcGrid[neighbor].status;
										bool diffStatusValues = status != statusClosedValue && status != statusOpenValue;
										if (diffStatusValues || calcGrid[neighbor].knownCost > totalPathCost)
										{
											Vector3 tileCenter = grid.GetTileCenter(neighbor);
											if (diffStatusValues)
											{
												float tileDistance = grid.ApproxDistanceInTiles(GenMath.SphericalDistance(tileCenter.normalized, normalized));
												calcGrid[neighbor].heuristicCost = Mathf.RoundToInt(ticksPerMove * tileDistance * heuristicStrength * bestRoadDiscount);
											}
											int costNodeCost = totalPathCost + calcGrid[neighbor].heuristicCost;
											calcGrid[neighbor].parentTile = tile;
											calcGrid[neighbor].knownCost = totalPathCost;
											calcGrid[neighbor].status = statusOpenValue;
											calcGrid[neighbor].costNodeCost = costNodeCost;
											if (DebugHelper.World.VehicleDef != null)
											{
												if (DebugHelper.World.DebugType == WorldPathingDebugType.PathCosts)
												{
													Find.World.debugDrawer.FlashTile(neighbor, colorPct: calcGrid[neighbor].knownCost / 150f,
														text: $"t:{ticksPerMove} h:{calcGrid[neighbor].heuristicCost}");
												}
												else if (DebugHelper.World.DebugType == WorldPathingDebugType.Reachability)
												{
													Find.World.debugDrawer.FlashTile(neighbor, colorPct: 0.55f);
												}
											}
											openList.Push(new CostNode(neighbor, costNodeCost));
										}
									}
								}
							}
							tilesSearched++;
							calcGrid[tile].status = statusClosedValue;
							if (terminator != null && terminator(calcGrid[tile].costNodeCost))
							{
								return WorldPath.NotFound;
							}
						}
					}
				}
				Log.Warning($"{vehiclesPathing} pathing from {startTile} to {destTile} ran out of tiles to process.");
				return WorldPath.NotFound;
			}
			finally
			{
				ClearTileCache();
			}
		}

		/// <summary>
		/// Finalize path from <paramref name="lastTile"/>
		/// </summary>
		/// <param name="lastTile"></param>
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

		/// <summary>
		/// Reset all statuses
		/// </summary>
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

		/// <summary>
		/// Calculate heuristic strength from <paramref name="startTile"/> to <paramref name="destTile"/>
		/// </summary>
		/// <param name="startTile"></param>
		/// <param name="destTile"></param>
		private int CalculateHeuristicStrength(int startTile, int destTile)
		{
			float x = Find.WorldGrid.ApproxDistanceInTiles(startTile, destTile);
			return Mathf.RoundToInt(HeuristicStrength_DistanceCurve.Evaluate(x));
		}

		/// <summary>
		/// Cost node for world tiles
		/// </summary>
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

		/// <summary>
		/// Node values for each Tile
		/// </summary>
		private struct PathFinderNodeFast
		{
			public int knownCost;
			public int heuristicCost;
			public int parentTile;
			public int costNodeCost;
			public ushort status;
		}

		/// <summary>
		/// Cost comparer between tiles
		/// </summary>
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

		private class TileFeatureLookup
		{
			private readonly List<BiomeDef> biomeDefs = new List<BiomeDef>();
			private readonly List<RiverDef> riverDefs = new List<RiverDef>();
			private readonly List<RoadDef> roadDefs = new List<RoadDef>();
			private readonly List<Hilliness> hills = new List<Hilliness>();

			public TileFeatureLookup(WorldGrid worldGrid)
			{
				WorldGrid = worldGrid;
			}

			private WorldGrid WorldGrid { get; set; }

			public void RegisterAllFeatureTypes()
			{
				biomeDefs.AddRange(DefDatabase<BiomeDef>.AllDefsListForReading);
				roadDefs.AddRange(DefDatabase<RoadDef>.AllDefsListForReading);
				riverDefs.AddRange(DefDatabase<RiverDef>.AllDefsListForReading);
				hills.AddRange(Enum.GetValues(typeof(Hilliness)).Cast<Hilliness>());
			}

			public int TileCacheSize => biomeDefs.Count * riverDefs.Count * roadDefs.Count * hills.Count;

			private int IndexFor(BiomeDef biomeDef) => biomeDefs.IndexOf(biomeDef) + 1;

			private int IndexFor(RiverDef riverDef) => riverDefs.IndexOf(riverDef) + 1;

			//REDO - Will need implementation for TileTo -> TileFrom calculation
			private int IndexFor(RoadDef roadDef) => 0;// roadDefs.IndexOf(roadDef) + 1;

			private int IndexFor(Hilliness hilliness) => hills.IndexOf(hilliness) + 1;

			public int IndexFor(int tileId)
			{
				Tile tile = WorldGrid[tileId];
				BiomeDef biomeDef = tile.biome;
				RiverDef riverDef = tile.Rivers?.MaxBy(river => river.river.widthOnWorld).river;
				RoadDef roadDef = tile.Roads?.MinBy(road => road.road.movementCostMultiplier).road;
				Hilliness hilliness = tile.hilliness;
				return (IndexFor(biomeDef) * biomeDefs.Count) + (IndexFor(riverDef) * riverDefs.Count) + (IndexFor(roadDef) * roadDefs.Count) + (IndexFor(hilliness) * hills.Count);
			}
		}
	}
}
