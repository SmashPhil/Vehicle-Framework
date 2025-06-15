using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles
{
  /// <summary>
  /// Vehicle specific path grid
  /// </summary>
  public sealed class VehiclePathGrid : VehicleGridManager
  {
    public const int ImpassableCost = 10000;

    public int[] innerArray;

    public VehiclePathGrid(VehiclePathingSystem mapping, VehicleDef vehicleDef) : base(mapping,
      vehicleDef)
    {
      innerArray = new int[mapping.map.cellIndices.NumGridCells];
    }

    public bool Enabled { get; private set; }

    public void Release()
    {
      Enabled = false;

      if (mapping.GridOwners.IsOwner(createdFor) &&
        !mapping.GridOwners.TryForfeitOwnership(createdFor))
      {
        // If there are no vehicles left to claim ownership, we should release the region grid
        // rather than leave it in an invalid state.
        mapping[createdFor].VehicleRegionAndRoomUpdater.Release();
      }
    }

    public override void PostInit()
    {
    }

    /// <summary>
    /// <paramref name="loc"/> is not impassable
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
        Log.Error(
          $"Mapping: {mapping is null} Map: {mapping?.map is null} CellInd: " +
          $"{mapping?.map?.cellIndices is null} Info: {mapping?.map?.info}Exception: {ex}");
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
      return innerArray[index] < ImpassableCost;
    }

    /// <summary>
    /// Cached path cost at <paramref name="loc"/>
    /// </summary>
    /// <param name="loc"></param>
    public int PerceivedPathCostAt(IntVec3 loc)
    {
      return innerArray[mapping.map.cellIndices.CellToIndex(loc)];
    }

    /// <summary>
    /// Recalculate path cost for each cell in CellRect.
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
      Interlocked.Exchange(ref innerArray[mapping.map.cellIndices.CellToIndex(cell)], cost);
      debugString?.Append($"WalkableNew: {WalkableFast(cell)} WalkableOld: {walkable}");
      bool walkabilityChanged = WalkableFast(cell) != walkable;

      if (VehicleMod.settings.debug.debugPathCostChanges)
        Debug.Message(debugString.ToStringSafe());

      if (walkabilityChanged && !mapping[createdFor].Suspended)
      {
        mapping[createdFor].VehicleReachability.ClearCache();
        mapping[createdFor].VehicleRegionDirtyer.NotifyWalkabilityChanged(cell);
      }
    }

    /// <summary>
    /// Recalculate all cells in the map
    /// </summary>
    public void RecalculateAllPerceivedPathCosts()
    {
      Enabled = true;

      foreach (IntVec3 cell in mapping.map.AllCells)
      {
        RecalculatePerceivedPathCostAt(cell);
      }
    }

    /// <summary>
    /// Calculate cost at <paramref name="cell"/>
    /// </summary>
    public int CalculatedCostAt(IntVec3 cell, StringBuilder stringBuilder = null)
    {
      return CalculatePathCostFor(createdFor, mapping.map, cell, stringBuilder);
    }

    /// <summary>
    /// Static calculation that allows for pseudo-calculations outside real-time pathgrids
    /// </summary>
    /// <param name="vehicleDef"></param>
    /// <param name="map"></param>
    /// <param name="cell"></param>
    public static int CalculatePathCostFor(VehicleDef vehicleDef, Map map, IntVec3 cell,
      StringBuilder stringBuilder = null)
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

        if (!PassableTerrainCost(vehicleDef, terrainDef, out pathCost, stringBuilder))
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
              if (thing is null || !thing.Spawned || thing.Destroyed ||
                thing is VehiclePawn vehicle)
              {
                continue;
              }
              else if (vehicleDef.properties.customThingCosts.TryGetValue(thing.def,
                out thingPathCost))
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

        WeatherBuildupCategory weatherBuildupCategory = map.snowGrid.GetCategory(cell);
        if (!vehicleDef.properties.customWeatherCosts.TryGetValue(weatherBuildupCategory,
          out int snowPathCost))
        {
          snowPathCost = WeatherBuildupUtility.MovementTicksAddOn(weatherBuildupCategory);
        }

        snowPathCost = snowPathCost.Clamp(0, 450);

        stringBuilder?.AppendLine($"snowPathCost: {snowPathCost}");
        pathCost += snowPathCost;
        stringBuilder?.AppendLine($"final cost: {pathCost}");
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Exception thrown while recalculating cost for {vehicleDef} at {cell}.\nException={ex}");
        Log.Error(
          $"Calculated Cost Report:\n{stringBuilder}\nProps={vehicleDef?.properties is null} " +
          $"Terrain={vehicleDef?.properties?.customTerrainCosts is null} Snow: " +
          $"{vehicleDef?.properties?.customWeatherCosts is null}");
      }

      return pathCost;
    }

    public static bool PassableTerrainCost(VehicleDef vehicleDef, TerrainDef terrainDef,
      out int pathCost, StringBuilder stringBuilder = null)
    {
      pathCost = TerrainCostAt(vehicleDef, terrainDef, stringBuilder);
      return pathCost < ImpassableCost;
    }

    public static int TerrainCostAt(VehicleDef vehicleDef, TerrainDef terrainDef,
      StringBuilder stringBuilder = null)
    {
      int pathCost = terrainDef.pathCost;
      stringBuilder?.AppendLine($"Starting Terrain check. Default Cost = {pathCost}");
      if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
      {
        stringBuilder?.AppendLine($"custom terrain cost: {customPathCost}");
        pathCost = customPathCost;
      }
      else if (terrainDef.passability == Traversability.Impassable)
      {
        stringBuilder?.AppendLine($"terrainDef impassable: {ImpassableCost}");
        return ImpassableCost;
      }
      else if (vehicleDef.properties.defaultTerrainImpassable)
      {
        stringBuilder?.AppendLine("defaultTerrain is impassable and no custom pathCost was found.");
        return ImpassableCost;
      }

      return pathCost;
    }
  }
}