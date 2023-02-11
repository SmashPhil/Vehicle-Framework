using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Vehicle specific path grid
	/// </summary>
	public sealed class VehiclePathGrid
	{
		public const int ImpassableCost = 10000;

		private readonly VehicleMapping mapping;
		private readonly VehicleDef vehicleDef;
		
		public int[] pathGrid;

		public VehiclePathGrid(VehicleMapping mapping, VehicleDef vehicleDef)
		{
			this.mapping = mapping;
			this.vehicleDef = vehicleDef;
			ResetPathGrid();
		}

		/// <summary>
		/// Clear path grid of all costs
		/// </summary>
		public void ResetPathGrid()
		{
			pathGrid = new int[mapping.map.cellIndices.NumGridCells];
		}

		/// <summary>
		/// <paramref name="loc"/> is not impassable for <see cref="vehicleDef"/>
		/// </summary>
		/// <param name="loc"></param>
		public bool Walkable(IntVec3 loc)
		{
			return loc.InBounds(mapping.map) && WalkableFast(loc);
		}

		/// <summary>
		/// <see cref="Walkable(IntVec3)"/> with no <see cref="GenGrid.InBounds(IntVec3, Map)"/> validation.
		/// </summary>
		/// <param name="loc"></param>
		public bool WalkableFast(IntVec3 loc)
		{
			return WalkableFast(mapping.map.cellIndices.CellToIndex(loc));
		}

		/// <summary>
		/// <seealso cref="WalkableFast(IntVec3)"/> given (<paramref name="x"/>,<paramref name="z"/>) coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		public bool WalkableFast(int x, int z)
		{
			return WalkableFast(mapping.map.cellIndices.CellToIndex(x, z));
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
			return pathGrid[mapping.map.cellIndices.CellToIndex(loc)];
		}

		/// <summary>
		/// Recalculate path cost for tile <paramref name="vehicle"/> is registered on
		/// </summary>
		/// <param name="vehicle"></param>
		public void RecalculatePerceivedPathCostUnderThing(Thing thing)
		{
			if (thing.def.Size == IntVec2.One)
			{
				RecalculatePerceivedPathCostAt(thing.Position);
				return;
			}
			CellRect cellRect = thing.OccupiedRect();
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
			if (!cell.InBounds(mapping.map))
			{
				return;
			}
			bool walkable = WalkableFast(cell);
			StringBuilder debugString = null;
			if (VehicleMod.settings.debug.debugPathCostChanges)
			{
				debugString = new StringBuilder();
			}
			pathGrid[mapping.map.cellIndices.CellToIndex(cell)] = CalculatedCostAt(cell, debugString);
			debugString?.Append($"WalkableNew: {WalkableFast(cell)} WalkableOld: {walkable}");
			bool walkabilityChanged = WalkableFast(cell) != walkable;
			if (VehicleMod.settings.debug.debugPathCostChanges)
			{
				Debug.Message(debugString.ToStringSafe());
			}
			if (walkabilityChanged)
			{
				mapping[vehicleDef].VehicleReachability.ClearCache();
				mapping[vehicleDef].VehicleRegionDirtyer.Notify_WalkabilityChanged(cell);
			}
		}

		/// <summary>
		/// Recalculate all cells in the map
		/// </summary>
		public void RecalculateAllPerceivedPathCosts()
		{
			foreach (IntVec3 cell in mapping.map.AllCells)
			{
				RecalculatePerceivedPathCostAt(cell);
			}
		}

		/// <summary>
		/// Calculate cost for <see cref="vehicleDef"/> at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public int CalculatedCostAt(IntVec3 cell, StringBuilder stringBuilder = null)
		{
			return CalculatePathCostFor(vehicleDef, mapping.map, cell, stringBuilder);
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
				int thingPathCost = 0;
				if (thing is VehiclePawn vehicle)
				{
					continue;
				}
				else if (vehicleDef.properties.customThingCosts.TryGetValue(thing.def, out thingPathCost))
				{
					if (thingPathCost >= ImpassableCost)
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
			if (!vehicleDef.properties.customSnowCategoryTicks.TryGetValue(snowCategory, out int snowPathCost))
			{
				snowPathCost = SnowUtility.MovementTicksAddOn(snowCategory);
			}
			snowPathCost = snowPathCost.Clamp(0, 450);

			stringBuilder.AppendLine($"snowPathCost: {snowPathCost}");
			pathCost += snowPathCost;
			stringBuilder.AppendLine($"final cost: {pathCost}");
			return pathCost;
		}
	}
}