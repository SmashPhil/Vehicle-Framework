using System.Collections.Generic;
using CoreLib.PathFinding;
using JetBrains.Annotations;

namespace Vehicles;

internal class RegionData
{
  internal readonly VehicleRegionMaker regionMaker;
  internal readonly VehicleRegionGridManager regionGridManager;
  internal readonly VehicleRegionAndRoomUpdater regionAndRoomUpdater;
  internal readonly VehicleRegionDirtyer regionDirtyer;
  internal readonly VehicleReachability reachability;

  private readonly VehicleGridManager[] gridManagers;

  // TODO 1.7 - Decouple pathfinder from reachability
  public RegionData(IPathingManager pathing, VehicleDef vehicleDef, [CanBeNull] IPathFinder<PathSettings> pathFinder)
  {
    regionMaker = new VehicleRegionMaker(pathing, vehicleDef);
    regionDirtyer = new VehicleRegionDirtyer(pathing, vehicleDef, regionMaker);
    regionAndRoomUpdater = new VehicleRegionAndRoomUpdater(pathing, vehicleDef, regionDirtyer);
    regionGridManager = new VehicleRegionGridManager(pathing, vehicleDef, regionMaker, regionDirtyer);
    reachability = new VehicleReachability(pathing, vehicleDef, pathFinder);

    gridManagers = [regionMaker, regionDirtyer, regionAndRoomUpdater, regionGridManager, reachability];
  }

  public IEnumerable<VehicleGridManager> AllGridManagers => gridManagers;

  public void PostInit()
  {
    foreach (var gridManager in gridManagers)
    {
      gridManager.PostInit();
    }
  }

  public void ChangeOwner(VehicleDef vehicleDef)
  {
    foreach (var gridManager in gridManagers)
    {
      gridManager.ChangeOwner(vehicleDef);
    }
  }
}