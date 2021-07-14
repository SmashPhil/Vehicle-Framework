using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles.AI
{
	/// <summary>
	/// Vehicle specific path grid
	/// </summary>
	public sealed class VehiclePathGrid
	{
		public const int ImpassableCost = 10000;

		private readonly Map map;
		private readonly VehicleDef vehicleDef;
		
		public int[] pathGrid;

		public VehiclePathGrid(Map map, VehicleDef vehicleDef)
		{
			this.map = map;
			this.vehicleDef = vehicleDef;
			ResetPathGrid();
		}

		/// <summary>
		/// Clear path grid of all costs
		/// </summary>
		public void ResetPathGrid()
		{
			pathGrid = new int[map.cellIndices.NumGridCells];
		}

		/// <summary>
		/// <paramref name="loc"/> is not impassable for <see cref="vehicleDef"/>
		/// </summary>
		/// <param name="loc"></param>
		public bool Walkable(IntVec3 loc)
		{
			return loc.InBounds(map) && pathGrid[map.cellIndices.CellToIndex(loc)] < ImpassableCost;
		}

		/// <summary>
		/// <see cref="Walkable(IntVec3)"/> with no <see cref="GenGrid.InBounds(IntVec3, Map)"/> validation.
		/// </summary>
		/// <param name="loc"></param>
		public bool WalkableFast(IntVec3 loc)
		{
			return pathGrid[map.cellIndices.CellToIndex(loc)] < ImpassableCost;
		}

		/// <summary>
		/// <seealso cref="WalkableFast(IntVec3)"/> given (<paramref name="x"/>,<paramref name="z"/>) coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		public bool WalkableFast(int x, int z)
		{
			return pathGrid[map.cellIndices.CellToIndex(x, z)] < ImpassableCost;
		}

		/// <summary>
		/// <seealso cref="WalkableFast(IntVec3)"/> given cell <paramref name="index"/>
		/// </summary>
		/// <param name="index"></param>
		public bool WalkableFast(int index)
		{
			return pathGrid[index] < ImpassableCost;
		}

		/// <summary>
		/// Cached path cost at <paramref name="loc"/>
		/// </summary>
		/// <param name="loc"></param>
		public int PerceivedPathCostAt(IntVec3 loc)
		{
			return pathGrid[map.cellIndices.CellToIndex(loc)];
		}

		/// <summary>
		/// Recalculate path cost for tile <paramref name="vehicle"/> is registered on
		/// </summary>
		/// <param name="vehicle"></param>
		public void RecalculatePerceivedPathCostUnderThing(VehiclePawn vehicle)
		{
			if (vehicle.def.size == IntVec2.One)
			{
				RecalculatePerceivedPathCostAt(vehicle.Position);
				return;
			}
			CellRect cellRect = vehicle.OccupiedRect();
			for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (int j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					IntVec3 c = new IntVec3(j, 0, i);
					RecalculatePerceivedPathCostAt(c);
				}
			}
		}

		/// <summary>
		/// Recalculate and recache path cost at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public void RecalculatePerceivedPathCostAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			bool walkable = WalkableFast(cell);
			pathGrid[map.cellIndices.CellToIndex(cell)] = CalculatedCostAt(cell);
			if (WalkableFast(cell) != walkable)
			{
				map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleReachability.ClearCache();
				map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.Notify_WalkabilityChanged(cell);
			}
		}

		/// <summary>
		/// Recalculate all cells in the map
		/// </summary>
		public void RecalculateAllPerceivedPathCosts()
		{
			foreach (IntVec3 cell in map.AllCells)
			{
				RecalculatePerceivedPathCostAt(cell);
			}
		}

		/// <summary>
		/// Calculate cost for <see cref="VehicleDef"/> at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public int CalculatedCostAt(IntVec3 cell)
		{
			TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
			if (terrainDef is null)
			{
				return ImpassableCost;
			}
			int pathCost = terrainDef.pathCost;
			if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
			{
				pathCost = customPathCost;
			}
			else if (terrainDef.passability == Traversability.Impassable)
			{
				return ImpassableCost;
			}
			else if (vehicleDef.properties.defaultTerrainImpassable)
			{
				return ImpassableCost;
			}
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			int thingCost = 0;
			foreach (Thing thing in list)
			{
				if (vehicleDef.properties.customThingCosts.TryGetValue(thing.def, out int thingPathCost))
				{
					if (thingPathCost < 0 || thingPathCost >= ImpassableCost)
					{
						return ImpassableCost;
					}
					if (thingPathCost > thingCost)
					{
						thingCost = thingPathCost;
					}
				}
				else if (thing.def.passability == Traversability.Impassable)
				{
					return ImpassableCost;
				}
				thingPathCost = thing.def.pathCost;
				if (thingPathCost > thingCost)
				{
					thingCost = thingPathCost;
				}
			}
			pathCost += thingCost;
			pathCost += Mathf.RoundToInt(SnowUtility.MovementTicksAddOn(map.snowGrid.GetCategory(cell)) * vehicleDef.properties.snowPathingMultiplier);
			if (pathCost < 0)
			{
				pathCost = ImpassableCost;
			}
			return pathCost;
		}

		/// <summary>
		/// Contains ignore path cost repeater
		/// </summary>
		/// <param name="c"></param>
		private bool ContainsPathCostIgnoreRepeater(IntVec3 cell)
		{
			List<Thing> list = map.thingGrid.ThingsListAt(cell);
			for (int i = 0; i < list.Count; i++)
			{
				if (IsPathCostIgnoreRepeater(list[i].def))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// ThingDef ignores repeat path costs
		/// </summary>
		/// <param name="def"></param>
		private static bool IsPathCostIgnoreRepeater(ThingDef def)
		{
			return def.pathCost >= 25 && def.pathCostIgnoreRepeat;
		}

		/// <summary>
		/// Output all terrain path costs for each <see cref="VehicleDef"/> 
		/// </summary>
		[DebugOutput]
		private static void OutputAllPathcostsFor()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				stringBuilder.AppendLine($"------------- {vehicleDef.defName} -------------");

				foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
				{
					int pathCost = terrainDef.pathCost;
					if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
					{
						pathCost = customPathCost;
					}
					else if (vehicleDef.properties.defaultTerrainImpassable)
					{
						pathCost = ImpassableCost;
					}
					stringBuilder.AppendLine($"{terrainDef.defName} = {pathCost}");
				}

				stringBuilder.AppendLine($"--------------  End of Path Costs  --------------");
			}
			Log.Message(stringBuilder.ToString());
		}
	}
}