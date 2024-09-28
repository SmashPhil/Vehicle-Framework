using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using System.Threading;

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

		public void PostInit()
		{
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
			try
			{
				return loc.InBounds(mapping.map) && WalkableFast(loc);
			}
			catch (Exception ex)
			{
				Log.Error($"Mapping: {mapping is null} Map: {mapping?.map is null} CellInd: {mapping?.map?.cellIndices is null} Info: {mapping?.map?.info}Exception: {ex}");
				Log.Error($"StackTrace: {StackTraceUtility.ExtractStackTrace()}");
			}
			return false;
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
		public void RecalculatePerceivedPathCostUnderRect(CellRect cellRect)
		{
			int index = 0;
			for (int z = cellRect.minZ; z <= cellRect.maxZ; z++)
			{
				for (int x = cellRect.minX; x <= cellRect.maxX; x++)
				{
					IntVec3 cell = new IntVec3(x, 0, z);
					RecalculatePerceivedPathCostAt(cell);
					index++;
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
			int cost = CalculatedCostAt(cell, debugString);
			Interlocked.Exchange(ref pathGrid[mapping.map.cellIndices.CellToIndex(cell)], cost);
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
			stringBuilder?.AppendLine($"Starting calculation for {vehicleDef} at {cell}.");
			int pathCost = 0;
			try
			{
				TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
				if (terrainDef is null)
				{
					stringBuilder?.AppendLine($"Unable to retrieve terrain at {cell}.");
					return ImpassableCost;
				}
				pathCost = terrainDef.pathCost;
				stringBuilder?.AppendLine($"def pathCost = {pathCost}");

				if (!PassableTerrainCost(vehicleDef, terrainDef, ref pathCost, stringBuilder))
				{
					return ImpassableCost;
				}

				ThingGrid thingGrid = map.thingGrid;
				Monitor.Enter(thingGrid);
				try
				{
					List<Thing> thingList = thingGrid.ThingsListAt(cell);
					stringBuilder?.AppendLine($"Starting ThingList check.");
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
									stringBuilder?.AppendLine($"thingPathCost is impassable: {thingPathCost}");
									return ImpassableCost;
								}
							}
							else if (thing.ImpassableForVehicles())
							{
								stringBuilder?.AppendLine($"thingDef is impassable: {thingPathCost}");
								return ImpassableCost;
							}
							else
							{
								thingPathCost = thing.def.pathCost;
							}
							stringBuilder?.AppendLine($"thingPathCost: {thingPathCost}");
							if (thingPathCost > thingCost)
							{
								thingCost = thingPathCost;
							}
						}
						pathCost += thingCost;
					}
				}
				finally
				{
					Monitor.Exit(thingGrid);
				}
				
				SnowCategory snowCategory = map.snowGrid.GetCategory(cell);
				if (!vehicleDef.properties.customSnowCategoryTicks.TryGetValue(snowCategory, out int snowPathCost))
				{
					snowPathCost = SnowUtility.MovementTicksAddOn(snowCategory);
				}
				snowPathCost = snowPathCost.Clamp(0, 450);

				stringBuilder?.AppendLine($"snowPathCost: {snowPathCost}");
				pathCost += snowPathCost;
				stringBuilder?.AppendLine($"final cost: {pathCost}");
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while recalculating cost for {vehicleDef} at {cell}.\nException={ex}");
				Log.Error($"Calculated Cost Report:\n{stringBuilder}\nProps={vehicleDef?.properties is null} Terrain={vehicleDef?.properties?.customTerrainCosts is null} Snow: {vehicleDef?.properties?.customSnowCategoryTicks is null}");
			}
			return pathCost;
		}

		public static bool PassableTerrainCost(VehicleDef vehicleDef, TerrainDef terrainDef, ref int pathCost, StringBuilder stringBuilder = null)
		{
			stringBuilder?.AppendLine($"Starting Terrain check.");
			if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
			{
				stringBuilder?.AppendLine($"custom terrain cost: {customPathCost}");
				pathCost = customPathCost;
			}
			else if (terrainDef.passability == Traversability.Impassable)
			{
				stringBuilder?.AppendLine($"terrainDef impassable: {ImpassableCost}");
				return false;
			}
			else if (vehicleDef.properties.defaultTerrainImpassable)
			{
				stringBuilder?.AppendLine($"defaultTerrain is impassable and no custom pathCost was found.");
				return false;
			}
			return true;
		}
	}
}