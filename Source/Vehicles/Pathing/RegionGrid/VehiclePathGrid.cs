using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
		public void RecalculatePerceivedPathCostUnderRect(CellRect cellRect, List<Thing>[] thingLists)
		{
			int index = 0;
			for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (int j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					IntVec3 cell = new IntVec3(j, 0, i);
					RecalculatePerceivedPathCostAt(cell, thingLists[index]);
					index++;
				}
			}
		}

		/// <summary>
		/// Recalculate and recache path cost at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public void RecalculatePerceivedPathCostAt(IntVec3 cell, List<Thing> thingList)
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
			pathGrid[mapping.map.cellIndices.CellToIndex(cell)] = CalculatedCostAt(cell, thingList, debugString);
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
				RecalculatePerceivedPathCostAt(cell, mapping.map.thingGrid.ThingsListAt(cell));
			}
		}

		/// <summary>
		/// Calculate cost for <see cref="vehicleDef"/> at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public int CalculatedCostAt(IntVec3 cell, List<Thing> thingList, StringBuilder stringBuilder = null)
		{
			return CalculatePathCostFor(vehicleDef, mapping.map, cell, thingList, stringBuilder);
		}

		/// <summary>
		/// Static calculation that allows for pseudo-calculations outside real-time pathgrids
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="map"></param>
		/// <param name="cell"></param>
		public static int CalculatePathCostFor(VehicleDef vehicleDef, Map map, IntVec3 cell, List<Thing> thingList, StringBuilder stringBuilder = null)
		{
			stringBuilder ??= new StringBuilder();
			stringBuilder.Clear();
			stringBuilder.AppendLine($"Starting calculation for {vehicleDef} at {cell}.");
			int pathCost = 0;
			try
			{
				TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
				if (terrainDef is null)
				{
					stringBuilder.AppendLine($"Unable to retrieve terrain at {cell}.");
					return ImpassableCost;
				}
				pathCost = terrainDef.pathCost;
				stringBuilder.AppendLine($"def pathCost = {pathCost}");

				stringBuilder.AppendLine($"Starting Terrain check.");
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

				stringBuilder.AppendLine($"Starting ThingList check.");
				if (!thingList.NullOrEmpty())
				{
					int thingCost = 0;
					foreach (Thing thing in thingList)
					{
						int thingPathCost = 0;
						if (thing is null || !thing.Spawned || thing.Destroyed || thing is VehiclePawn vehicle)
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
						}
						else if (thing.ImpassableForVehicles())
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
				}
				
				SnowCategory snowCategory = map.snowGrid.GetCategory(cell);
				if (!vehicleDef.properties.customSnowCategoryTicks.TryGetValue(snowCategory, out int snowPathCost))
				{
					snowPathCost = SnowUtility.MovementTicksAddOn(snowCategory);
				}
				snowPathCost = snowPathCost.Clamp(0, 450);

				stringBuilder.AppendLine($"snowPathCost: {snowPathCost}");
				pathCost += snowPathCost;
				stringBuilder.AppendLine($"final cost: {pathCost}");
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while recalculating cost for {vehicleDef} at {cell}.\nException={ex}");
				Log.Error($"Calculated Cost Report:\n{stringBuilder}\nProps={vehicleDef?.properties is null} Terrain={vehicleDef?.properties?.customTerrainCosts is null} Snow: {vehicleDef?.properties?.customSnowCategoryTicks is null}");
			}
			return pathCost;
		}
	}
}