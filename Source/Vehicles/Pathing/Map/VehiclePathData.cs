using System;
using System.Collections.Generic;
using CoreLib;
using CoreLib.Collections;
using CoreLib.PathFinding;
using JetBrains.Annotations;
using SmashTools;

namespace Vehicles;

#pragma warning disable CS0618

public class PathDataContainer
{
  private readonly IPathingManager manager;
  private readonly List<VehicleDef> vehicleDefs;

  private readonly PathData[] pathDatas;

  private readonly Ref<VehicleDef> buildingFor = new();

  public PathDataContainer(IPathingManager manager, List<VehicleDef> vehicleDefs)
  {
    this.manager = manager;
    this.vehicleDefs = vehicleDefs;
    pathDatas = new PathData[vehicleDefs.Count];
  }

  public PathData this[int index] => pathDatas[index];

  public PathData this[VehicleDef def]
  {
    get
    {
#if DEBUG
      if (buildingFor.Value == def)
      {
        Trace.Fail(
          "Trying to pull PathData by indexing when it's currently in the middle of generation.");
        return null;
      }
#endif
      return pathDatas[def.DefIndex];
    }
  }

  public ReadOnlyArray<PathData> AllPathData => new(pathDatas);

  // TODO - Decouple pathfinder from various region classes.
  public void GenerateAllPathData(IPathGridCalculator calculator, [CanBeNull] IPathFinder<PathSettings> pathFinder = null)
  {
    // All vehicles need path data (even aerial vehicles for landing)
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      pathDatas[vehicleDef.DefIndex] = CreatePathData(calculator, vehicleDef, pathFinder);
    }
  }

  private PathData CreatePathData(IPathGridCalculator calculator, VehicleDef vehicleDef,
    [CanBeNull] IPathFinder<PathSettings> pathFinder)
  {
    PathData pathData;
    using (new ScopedReferenceRollback<VehicleDef>(buildingFor, vehicleDef))
    {
      pathData = new PathData(manager, vehicleDef)
      {
        VehiclePathFinder = pathFinder as VehiclePathFinder,
        // TODO - may need refactor later, pathfinder isn't necessarily the only grid that needs synchronizing
        VehiclePathGrid = new VehiclePathGrid(manager, vehicleDef, calculator)
      };

      if (pathData.IsOwner)
      {
        pathData.RegionData = new RegionData(manager, vehicleDef, pathFinder);
        // TODO 1.7 - Remove
        pathData.ReachabilityData = new VehiclePathingSystem.VehicleReachabilitySettings(pathData.RegionData);
      }
      else
      {
        // Will return itself if it's an owner
        VehicleDef ownerDef = manager.GridOwners.GetOwner(vehicleDef);
        pathData.RegionData = pathDatas[ownerDef.DefIndex].RegionData;
        pathData.ReachabilityData = pathDatas[ownerDef.DefIndex].ReachabilityData;
      }

      pathDatas[vehicleDef.DefIndex] = pathData;
    }
    pathData.PostInit();
    return pathData;
  }
}

/// <summary>
/// Container for all path related subcomponents specific to a <see cref="VehicleDef"/>.
/// </summary>
/// <remarks>Stores data strictly for deviations from vanilla regarding impassable values</remarks>
public class PathData
{
  private readonly IPathingManager manager;
  private readonly VehicleDef vehicleDef;

  internal PathData(IPathingManager manager, VehicleDef vehicleDef)
  {
    this.manager = manager;
    this.vehicleDef = vehicleDef;
  }

  public bool IsOwner => manager.GridOwners.IsOwner(vehicleDef);

  // Region grid is currently disabled.
  public bool Suspended => !VehicleRegionAndRoomUpdater.Enabled;

  // Internal setter added for legacy support (for now). Only mods referencing these old systems
  // will need them. These will be removed in the near future.
  // TODO 1.7 - Remove
  [Obsolete("Access classes directly from properties.")]
  public VehiclePathingSystem.VehicleReachabilitySettings ReachabilityData { get; internal set; }

  internal RegionData RegionData { get; set; }

  public VehiclePathGrid VehiclePathGrid { get; internal set; }

  // Internal setter added for legacy support (for now). Only mods referencing these old systems
  // will need them. These will be removed in the near future.
  [Obsolete("Use VehiclePathingSystem.PathFinder instead.")]
  public VehiclePathFinder VehiclePathFinder { get; internal set; }

  public VehicleReachability VehicleReachability => RegionData.reachability;

  // TODO 1.6.2144 - Remove
  [Obsolete("Fetch from region grid manager.")]
  public VehicleRegionGrid VehicleRegionGrid => VehicleRegionGridManager[RegionGridType.Normal];

  public VehicleRegionGridManager VehicleRegionGridManager => RegionData.regionGridManager;

  public VehicleRegionMaker VehicleRegionMaker => RegionData.regionMaker;

  public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater => RegionData.regionAndRoomUpdater;

  public VehicleRegionDirtyer VehicleRegionDirtyer => RegionData.regionDirtyer;

  public void PostInit()
  {
    VehiclePathGrid.PostInit();
    if (IsOwner)
    {
      RegionData.PostInit();
    }
  }
}