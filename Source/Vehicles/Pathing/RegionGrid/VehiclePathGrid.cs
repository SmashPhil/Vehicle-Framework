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
		/// Calculate cost for <see cref="vehicleDef"/> at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public int CalculatedCostAt(IntVec3 cell)
		{
			return CalculatePathCostFor(vehicleDef, map, cell);
		}

		/// <summary>
		/// Static calculation that allows for pseudo-calculations outside real-time pathgrids
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		public static int CalculatePathCostFor(VehicleDef vehicleDef, Map map, IntVec3 cell, StringBuilder stringBuilder = null)
		{
			stringBuilder ??= new StringBuilder();
			stringBuilder.Clear();
			stringBuilder.AppendLine($"Starting calculation for {vehicleDef} at {cell}.");
			TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
			if (terrainDef is null)
			{
				stringBuilder.AppendLine($"Unable to retrieve terrain at {cell}.");
				return ImpassableCost;
			}
			int pathCost = terrainDef.pathCost;
			stringBuilder.AppendLine($"def pathCost = {pathCost}");
			if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
			{
				stringBuilder.AppendLine($"custom turrain cost: {customPathCost}");
				pathCost = customPathCost;
			}
			else if (terrainDef.passability == Traversability.Impassable)
			{
				stringBuilder.AppendLine($"terrainDef impassable: {ImpassableCost}");
				return ImpassableCost;
			}
			else if (vehicleDef.properties.defaultTerrainImpassable)
			{
				stringBuilder.AppendLine($"defaultTerrain is impassable and no custom pathCost was found.");
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
						stringBuilder.AppendLine($"thingPathCost is impassable: {thingPathCost}");
						return ImpassableCost;
					}
					if (thingPathCost > thingCost)
					{
						thingCost = thingPathCost;
					}
				}
				else if (thing.def.passability == Traversability.Impassable)
				{
					stringBuilder.AppendLine($"thingDef is impassable: {thingPathCost}");
					return ImpassableCost;
				}
				else
				{
					thingPathCost = thing.def.pathCost;
				}
				stringBuilder.AppendLine($"thingPathCost: {thingPathCost}");
				if (thingPathCost > thingCost)
				{
					thingCost = thingPathCost;
				}
			}
			pathCost += thingCost;
			SnowCategory snowCategory = map.snowGrid.GetCategory(cell);
			if (!vehicleDef.properties.customSnowCosts.TryGetValue(snowCategory, out int snowPathCost))
			{
				snowPathCost = SnowUtility.MovementTicksAddOn(snowCategory).Clamp(0, 450);
			}
			stringBuilder.AppendLine($"snowPathCost: {snowPathCost}");
			pathCost += snowPathCost;
			if (pathCost < 0)
			{
				stringBuilder.AppendLine($"pathCost < 0. Setting to {ImpassableCost}");
				pathCost = ImpassableCost;
			}
			stringBuilder.AppendLine($"final cost: {pathCost}");
			return pathCost;
		}

		/// <summary>
		/// Calculate cost for <paramref name="vehicleDef"/> on <paramref name="terrainDef"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="terrainDef"></param>
		/// <param name="stringBuilder"></param>
		public static int CalculatePathCostForTerrain(VehicleDef vehicleDef, TerrainDef terrainDef, StringBuilder stringBuilder = null)
		{
			stringBuilder ??= new StringBuilder();
			stringBuilder.Clear();
			stringBuilder.Append($"Starting calculation for {vehicleDef} and {terrainDef.defName}.");
			int pathCost = terrainDef.pathCost;
			stringBuilder.AppendLine($"def pathCost = {pathCost}");
			if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
			{
				stringBuilder.AppendLine($"custom turrain cost: {customPathCost}");
				pathCost = customPathCost;
			}
			else if (terrainDef.passability == Traversability.Impassable)
			{
				stringBuilder.AppendLine($"terrainDef impassable: {ImpassableCost}");
				return ImpassableCost;
			}
			else if (vehicleDef.properties.defaultTerrainImpassable)
			{
				stringBuilder.AppendLine($"defaultTerrain is impassable and no custom pathCost was found.");
				return ImpassableCost;
			}
			if (pathCost < 0)
			{
				stringBuilder.AppendLine($"pathCost < 0. Setting to {ImpassableCost}");
				pathCost = ImpassableCost;
			}
			stringBuilder.AppendLine($"final cost: {pathCost}");
			return pathCost;
		}
	}
}