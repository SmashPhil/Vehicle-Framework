using System;
using System.Collections.Generic;
using System.Text;
using SmashTools;
using SmashTools.Performance;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace Vehicles;

/// <summary>
/// Vehicle specific path grid
/// </summary>
public sealed class VehiclePathGrid : VehicleGridManager, IGridDebouncerSource
{
  public const int ImpassableCost = 10000;

  private readonly IPathGridCalculator calculator;
  private NativeArray<int> costGrid;

  /// <summary>
  /// Emits when the path grid is about to recalculate the path cost of a cell and update the path grid array.
  /// </summary>
  /// <remarks>This is not called on the main thread.</remarks>
  public event Action OnWritingToGrid;

  /// <summary>
  /// Emits when the walkability state of a cell changes.
  /// </summary>
  /// <remarks>
  /// Listeners are notified with the coordinates of the cell whose walkability has changed.
  /// </remarks>
  public event Action<IntVec3> OnWalkabilityChanged;

  public VehiclePathGrid(IPathingManager pathing, VehicleDef vehicleDef, IPathGridCalculator calculator) :
    base(pathing, vehicleDef)
  {
    this.calculator = calculator;
    costGrid = new NativeArray<int>(pathing.Map.cellIndices.NumGridCells, Allocator.Persistent);
  }

  public bool Enabled { get; private set; }

  GridDebouncer IGridDebouncerSource.ActiveDebouncer { set => GridDebouncer = value; }

  private GridDebouncer GridDebouncer { get; set; }

  public NativeArray<int>.ReadOnly CostGrid => costGrid.AsReadOnly();

  public int this[int index]
  {
    get
    {
      return costGrid[index];
    }
  }

  internal void Release()
  {
    Enabled = false;
  }

  /// <summary>
  /// <paramref name="loc"/> is not impassable
  /// </summary>
  /// <param name="loc"></param>
  public bool Walkable(IntVec3 loc)
  {
    try
    {
      return loc.InBounds(map) && WalkableFast(loc);
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Mapping: {pathing is null} Map: {map is null} CellInd: " +
        $"{map?.cellIndices is null} Info: {map?.info}Exception: {ex}");
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
    return WalkableFast(map.cellIndices.CellToIndex(loc));
  }

  /// <summary>
  /// <seealso cref="WalkableFast(IntVec3)"/> given (<paramref name="x"/>,<paramref name="z"/>) coordinates
  /// </summary>
  /// <param name="x"></param>
  /// <param name="z"></param>
  public bool WalkableFast(int x, int z)
  {
    return WalkableFast(map.cellIndices.CellToIndex(x, z));
  }

  /// <summary>
  /// <seealso cref="WalkableFast(IntVec3)"/> given cell <paramref name="index"/>
  /// </summary>
  /// <param name="index"></param>
  public bool WalkableFast(int index)
  {
    return costGrid[index] < ImpassableCost;
  }

  /// <summary>
  /// Cached path cost at <paramref name="loc"/>
  /// </summary>
  /// <param name="loc"></param>
  public int PerceivedPathCostAt(IntVec3 loc)
  {
    return costGrid[map.cellIndices.CellToIndex(loc)];
  }

  void IGridDebouncerSource.Execute(int index)
  {
    RecalculatePerceivedPathCostAt(map.cellIndices[index]);
  }

  /// <summary>
  /// Recalculate path cost for each cell in CellRect.
  /// </summary>
  public void RecalculatePerceivedPathCostUnderRect(CellRect cellRect)
  {
    for (int z = cellRect.minZ; z <= cellRect.maxZ; z++)
    {
      for (int x = cellRect.minX; x <= cellRect.maxX; x++)
      {
        IntVec3 cell = new(x, 0, z);
        RecalculatePerceivedPathCostAt(cell);
      }
    }
  }

  /// <summary>
  /// Recalculate and recache path cost at <paramref name="cell"/>
  /// </summary>
  /// <param name="cell"></param>
  public void RecalculatePerceivedPathCostAt(IntVec3 cell)
  {
    // PathGrid can get disabled while recalculating from another thread.
    if (!cell.InBounds(map) || !Enabled)
      return;

    if (GridDebouncer != null)
    {
      GridDebouncer.SetDirty(cell);
      return;
    }

    OnWritingToGrid?.Invoke();
    bool walkable = WalkableFast(cell);
    // TODO 1.7 - convert all calculate functions to ushort return types
    costGrid[map.cellIndices.CellToIndex(cell)] = CalculatedCostAt(cell);
    bool walkabilityChanged = WalkableFast(cell) != walkable;

    if (walkabilityChanged)
    {
      OnWalkabilityChanged?.Invoke(cell);
    }
  }

  /// <summary>
  /// Recalculate all cells in the map
  /// </summary>
  public void RecalculateAllPerceivedPathCosts()
  {
    Enabled = true;

    foreach (IntVec3 cell in map.AllCells)
    {
      RecalculatePerceivedPathCostAt(cell);
    }
  }

  // TODO 1.7 - Remove
  /// <summary>
  /// Calculate cost at <paramref name="cell"/>
  /// </summary>
  public int CalculatedCostAt(IntVec3 cell, StringBuilder stringBuilder = null)
  {
    return calculator.PathCostAt(map, cell, createdFor);
  }

  /// <summary>
  /// Static calculation that allows for pseudo-calculations outside real-time pathgrids
  /// </summary>
  [Profile, Obsolete("Path cost calculations have been moved to PathGridCalculator")]
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
      lock (thingGrid)
      {
        List<Thing> thingList = thingGrid.ThingsListAt(cell);
        stringBuilder?.AppendLine("Starting ThingList check.");
        if (!thingList.NullOrEmpty())
        {
          int maxCost = 0;
          foreach (Thing thing in thingList)
          {
            if (thing is null || !thing.Spawned || thing.Destroyed || thing is VehiclePawn)
              continue;
            int thingCost = ThingCostOf(vehicleDef, thing.def, stringBuilder);
            stringBuilder?.AppendLine($"thingPathCost: {thingCost}");
            if (thingCost > maxCost)
              maxCost = thingCost;
          }
          pathCost += maxCost;
        }
      }

      WeatherBuildupCategory weatherBuildupCategory = map.snowGrid.GetCategory(cell);
      if (!vehicleDef.properties.customWeatherCosts.TryGetValue(weatherBuildupCategory, out int weatherPathCost))
      {
        weatherPathCost = WeatherBuildupUtility.MovementTicksAddOn(weatherBuildupCategory);
      }
      weatherPathCost = weatherPathCost.Clamp(0, 450);
      stringBuilder?.AppendLine($"weatherPathCost: {weatherPathCost}");
      pathCost += weatherPathCost;
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

  public static int ThingCostOf(VehicleDef vehicleDef, ThingDef thingDef,
    StringBuilder stringBuilder = null)
  {
    if (vehicleDef.properties.customThingCosts.TryGetValue(thingDef,
      out int thingPathCost))
    {
      if (thingPathCost >= ImpassableCost)
      {
        stringBuilder?.AppendLine($"thingPathCost is impassable: {thingPathCost}");
        return ImpassableCost;
      }
    }
    else if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Things) != 0)
    {
      stringBuilder?.AppendLine($"thingPathCost is impassable: {thingPathCost}");
      return ImpassableCost;
    }
    else if (thingDef.ImpassableForVehicles())
    {
      stringBuilder?.AppendLine($"thingDef is impassable: {thingPathCost}");
      return ImpassableCost;
    }
    else
    {
      thingPathCost = thingDef.pathCost;
    }
    return thingPathCost;
  }

  // TODO 1.7 - Get rid of the unused out parameter 'pathCost'
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
    else if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Terrain) != 0)
    {
      stringBuilder?.AppendLine("defaultTerrain is impassable and no custom pathCost was found.");
      return ImpassableCost;
    }

    return pathCost;
  }
}