using System.Collections.Generic;
using System.Linq;
using CoreLib.PathFinding;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

/// <summary>
/// Reachability calculator for quick result path finding before running the algorithm
/// </summary>
[PublicAPI]
public sealed class VehicleReachability : VehicleGridManager
{
  private readonly VehicleReachabilityCache cache;

  private ChunkSearch chunkSearch;
  private VehiclePathGrid pathGrid;
  private VehicleRegionGridManager regionGridManager;
  private IPathFinder<PathSettings> pathFinder;

  public VehicleReachability(IPathingManager pathing, VehicleDef createdFor,
    [CanBeNull] IPathFinder<PathSettings> pathFinder) : base(pathing, createdFor)
  {
    cache = new VehicleReachabilityCache();
    this.pathFinder = pathFinder;
  }

  /// <summary>
  /// Currently calculating reachability between regions
  /// </summary>
  private bool CalculatingReachability { get; set; }

  public override void PostInit()
  {
    pathGrid = pathing.GetPathGrid(createdFor);
    pathGrid.OnWalkabilityChanged += ClearCache;

    regionGridManager = pathing.GetRegionGridManager(createdFor);
    chunkSearch = new ChunkSearch(pathing, createdFor, cache);
  }

  // TODO 1.7 - Can be optimized, this triggers for the full map for RecalcAll
  private void ClearCache(IntVec3 _)
  {
    cache.Clear();
  }

  /// <summary>
  /// Clear reachability cache
  /// </summary>
  public void ClearCache()
  {
    cache.Clear();
  }

  /// <summary>
  /// Clear reachability cache for specific vehicle
  /// </summary>
  /// <param name="vehicle"></param>
  public void ClearCacheFor(VehiclePawn vehicle)
  {
    cache.ClearFor(vehicle);
  }

  /// <summary>
  /// Clear reachability cache for targets retaining hostile Pawn
  /// </summary>
  /// <param name="hostileTo"></param>
  public void ClearCacheForHostile(Thing hostileTo)
  {
    cache.ClearForHostile(hostileTo);
  }

  /// <summary>
  /// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
  /// </summary>
  /// <param name="start"></param>
  /// <param name="dest"></param>
  /// <param name="peMode"></param>
  /// <param name="traverseMode"></param>
  /// <param name="maxDanger"></param>
  public bool CanReachVehicleNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode,
    TraverseMode traverseMode, Danger maxDanger)
  {
    return (dest.Map is null || dest.Map == map) && CanReachVehicle(start,
      (LocalTargetInfo)dest, peMode, traverseMode, maxDanger);
  }

  /// <summary>
  /// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
  /// </summary>
  public bool CanReachVehicleNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode,
    TraverseParms traverseParms)
  {
    return (dest.Map is null || dest.Map == map) &&
      CanReachVehicle(start, (LocalTargetInfo)dest, peMode, traverseParms);
  }

  /// <summary>
  /// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
  /// </summary>
  public bool CanReachVehicle(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode,
    TraverseMode traverseMode, Danger maxDanger)
  {
    return CanReachVehicle(start, dest, peMode, TraverseParms.For(traverseMode, maxDanger));
  }

  /// <summary>
  /// Traverse by cell or by region to determine reachability for Vehicle
  /// </summary>
  public bool CanReachVehicle(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode,
    TraverseParms traverseParms)
  {
    if (!ValidateCanStart(start, dest, traverseParms, out VehicleDef vehicleDef))
    {
      return false;
    }

    if (!pathGrid.WalkableFast(start))
    {
      Debug.Message($"Unable to start pathing from {start} to {dest}. Not walkable at {start}");
      return false;
    }

    bool freeTraversal = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater &&
      traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
    if ((peMode is PathEndMode.OnCell or PathEndMode.Touch or PathEndMode.ClosestTouch) && freeTraversal)
    {
      VehicleRoom room = VehicleRegionAndRoomQuery.RoomAtFast(start, map, createdFor);
      if (room != null &&
        room == VehicleRegionAndRoomQuery.RoomAtFast(dest.Cell, map, createdFor))
      {
        return true;
      }
    }

    // NOTE - I don't know if I'll ever enable door-capabilities for vehicles but right now the
    // region type will never be Portal so this is essentially just TraverseMode.ByPawn. Keeping
    // TraverseMode.PassDoors to retain what would be a vanilla entry point for door reachability.
    //if (traverseParms.mode is TraverseMode.PassAllDestroyableThings or TraverseMode.PassAllDestroyablePlayerOwnedThings
    //  && CanReachVehicle(start, dest, peMode, traverseParms with { mode = TraverseMode.PassDoors }))
    //{
    //  return true;
    //}

    dest = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicleDef, map, dest.ToTargetInfo(map),
      ref peMode);
    CalculatingReachability = true;
    try
    {
      ChunkSearch.Data searchData = new()
      {
        start = start,
        destination = dest,
        pathEndMode = peMode,
        traverseParms = traverseParms
      };

      if (pathFinder != null)
      {
        // TODO VF-343 - Implement BreachDestructibles for chunk vs. wall based breaching
        if (traverseParms.mode is TraverseMode.PassAllDestroyableThings)
        {
          return pathFinder.FindPath(start.ToPathNode(), dest.Cell.ToPathNode(), PathSettings.For(vehicleDef) with
          {
            search = PathSettings.GridSetting.BreachWalls | PathSettings.GridSetting.BreachDestructibles
          }) is { IsValid: true };
        }
        else if (traverseParms.mode is TraverseMode.PassAllDestroyablePlayerOwnedThings)
        {
          return pathFinder.FindPath(start.ToPathNode(), dest.Cell.ToPathNode(), PathSettings.For(vehicleDef) with
          {
            search = PathSettings.GridSetting.BreachWalls
          }) is { IsValid: true };
        }
      }
      else if (traverseParms.mode is TraverseMode.PassAllDestroyableThings or
               TraverseMode.PassAllDestroyablePlayerOwnedThings)
      {
        return chunkSearch.CanReachByCell(searchData);
      }

      if (traverseParms.mode is TraverseMode.PassAllDestroyableThings or
          TraverseMode.PassAllDestroyableThingsNotWater or
          TraverseMode.NoPassClosedDoorsOrWater)
      {
        return chunkSearch.CanReachByCell(searchData);
      }
      else
      {
        // TraverseMode.PassAllDestroyablePlayerOwnedThings has a separate grid for fast reachability checks, so we can use region
        // based search instead of cell based.
        return chunkSearch.CanReach(searchData);
      }
    }
    finally
    {
      CalculatingReachability = false;
    }
  }

  private bool ValidateCanStart(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms,
    out VehicleDef forVehicleDef)
  {
    if (CalculatingReachability)
    {
      Log.ErrorOnce(
        "Called CanReachVehicle while working. Suppressing further errors.",
        "CanReachVehicleWorkingError".GetHashCode());
      forVehicleDef = null;
      return false;
    }

    VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
    forVehicleDef = vehicle?.VehicleDef ?? createdFor;
    if (vehicle != null)
    {
      if (!vehicle.Spawned)
      {
        Log.Error($"Attempting reachability check for unspawned vehicle {vehicle}.");
        return false;
      }

      if (vehicle.Map != map)
      {
        Log.Error(
          $"Called CanReach with a vehicle not spawned on this map. This means that we can't check " +
          $"its reachability here. Vehicle's current map should have been used instead. vehicle={vehicle} " +
          $"vehicle.Map={vehicle.Map} map={map}");
        return false;
      }
    }

    if (!dest.IsValid)
    {
      Debug.Warning("Destination Invalid.");
      return false;
    }

    if (dest.HasThing && dest.Thing.Map != map)
    {
      Log.Error(
        $"Called CanReach for regions of a different map than destination.  Destination={dest} Map={map} " +
        $"Destination.Map={dest.Thing.Map}");
      return false;
    }

    if (!start.InBounds(map) || !dest.Cell.InBounds(map))
    {
      Debug.Warning("Start or Destination out of bounds for reachability check.");
      return false;
    }

    return true;
  }

  /// <summary>
  /// Can reach colony at cell <paramref name="cell"/>
  /// </summary>
  public bool CanReachBase(IntVec3 cell, VehicleDef vehicleDef)
  {
    TraverseParms traverseParms = TraverseParms.For(TraverseMode.ByPawn);
    if (Current.ProgramState != ProgramState.Playing)
    {
      return CanReachVehicle(cell, MapGenerator.PlayerStartSpot, PathEndMode.OnCell, traverseParms);
    }

    if (!pathing.Walkable(cell, vehicleDef))
    {
      return false;
    }

    Faction faction = map.ParentFaction ?? Faction.OfPlayer;
    List<Pawn> list = map.mapPawns.SpawnedPawnsInFaction(faction);
    foreach (Pawn pawn in list)
    {
      if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
      {
        return true;
      }
    }

    if (faction == Faction.OfPlayer)
    {
      List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
      foreach (Building b in allBuildingsColonist)
      {
        if (CanReachVehicle(cell, b, PathEndMode.Touch, traverseParms))
        {
          return true;
        }
      }
    }
    else
    {
      List<Thing> artificials = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
      foreach (Thing thing in artificials)
      {
        if (thing.Faction == faction &&
          CanReachVehicle(cell, thing, PathEndMode.Touch, traverseParms))
        {
          return true;
        }
      }
    }

    return CanReachBiggestMapEdgeRoom(cell, VehicleRegionGridManager.GetGridType(traverseParms));
  }

  /// <summary>
  /// Reachability to largest <see cref="VehicleRoom"/> touching map edge
  /// </summary>
  public bool CanReachBiggestMapEdgeRoom(IntVec3 cell, RegionGridType gridType)
  {
    VehicleRoom usableRoom = null;
    // ConcurrentDictionary.Keys snapshots, but ConcurrentDictionary.GetEnumerator does not.
    // Must utilize Key or Value collections for thread safe enumeration
    foreach (VehicleRoom room in regionGridManager[gridType].allRooms.Keys)
    {
      if (!room.TouchesMapEdge)
        continue;

      if (usableRoom is null || room.RegionCount > usableRoom.RegionCount)
      {
        usableRoom = room;
      }
    }

    return usableRoom != null && CanReachVehicle(cell,
      usableRoom.Regions.FirstOrDefault().Key.AnyCell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors));
  }

  /// <summary>
  /// Can reach map edge from <paramref name="cell"/>
  /// </summary>
  /// <param name="cell"></param>
  /// <param name="traverseParms"></param>
  public bool CanReachMapEdge(IntVec3 cell, TraverseParms traverseParms)
  {
    if (traverseParms.pawn is VehiclePawn vehicle)
    {
      if (!vehicle.Spawned)
      {
        return false;
      }

      if (vehicle.Map != map)
      {
        Log.Error(
          $"Called CanReachMapEdge with vehicle not spawned on this map. Pawn's current map should have been used instead of this one. vehicle={vehicle} vehicle.Map={vehicle.Map} map={map}");
        return false;
      }
    }

    VehicleRegion region =
      VehicleRegionAndRoomQuery.RegionAt(cell, pathing, createdFor, RegionType.Set_Passable);
    if (region is null)
    {
      return false;
    }

    if (region.Room.TouchesMapEdge)
    {
      return true;
    }

    bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParms);
    bool foundReg = false;

    bool regionProcessor(VehicleRegion r)
    {
      if (r.Room.TouchesMapEdge)
      {
        foundReg = true;
        return true;
      }

      return false;
    }

    VehicleRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor);
    return foundReg;
  }

  /// <summary>
  /// Can reach <paramref name="cell"/> with Unfogged constraint
  /// </summary>
  /// <param name="cell"></param>
  /// <param name="traverseParms"></param>
  public bool CanReachUnfogged(IntVec3 cell, TraverseParms traverseParms)
  {
    if (traverseParms.pawn != null)
    {
      if (!traverseParms.pawn.Spawned)
      {
        return false;
      }

      if (traverseParms.pawn.Map != map)
      {
        Log.Error(string.Concat(new object[]
        {
            "Called CanReachUnfogged() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
            traverseParms.pawn,
            " pawn.Map=",
            traverseParms.pawn.Map,
            " map=",
            map
        }));
        return false;
      }
    }

    if (!cell.InBounds(map))
    {
      return false;
    }

    if (!cell.Fogged(map))
    {
      return true;
    }

    VehicleRegion region =
      VehicleRegionAndRoomQuery.RegionAt(cell, pathing, createdFor);
    if (region == null)
    {
      return false;
    }

    bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParms);
    bool foundReg = false;

    bool regionProcessor(VehicleRegion r)
    {
      if (!r.AnyCell.Fogged(map))
      {
        foundReg = true;
        return true;
      }

      return false;
    }

    VehicleRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor);
    return foundReg;
  }
}