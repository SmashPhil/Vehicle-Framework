using System;
using System.Collections.Generic;
using CoreLib.Performance;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class ChunkSearch
{
  private readonly IPathingManager pathing;
  private readonly Map map;
  [CanBeNull]
  private readonly VehicleReachabilityCache cache;

  private VehicleDef createdFor;
  private VehiclePathGrid pathGrid;
  private VehicleRegionGridManager regionGridManager;

  private readonly Queue<VehicleRegion> openQueue = [];
  private readonly List<VehicleRegion> startingRegions = [];
  private readonly List<VehicleRegion> destRegions = [];

  private uint reachedIndex = 1;

  // TODO 1.7 - Remove
  public ChunkSearch(VehiclePathingSystem pathingSystem, VehicleDef vehicleDef,
    [CanBeNull] VehicleReachabilityCache cache)
  {
    this.pathing = pathingSystem;
    map = pathing.Map;
    this.cache = cache;
    createdFor = vehicleDef;
    pathGrid = pathingSystem[createdFor].VehiclePathGrid;
    regionGridManager = pathingSystem[createdFor].VehicleRegionGridManager;
  }

  public ChunkSearch(IPathingManager pathing, VehicleDef vehicleDef,
    [CanBeNull] VehicleReachabilityCache cache)
  {
    this.pathing = pathing;
    map = pathing.Map;
    this.cache = cache;
    createdFor = vehicleDef;
    pathGrid = pathing.GetPathGrid(createdFor);
    regionGridManager = pathing.GetRegionGridManager(createdFor);
  }

  public uint ReachedIndex => reachedIndex;

  private void Reset()
  {
    startingRegions.Clear();
    destRegions.Clear();
  }

  private bool Prepare(in Data data)
  {
    Reset();
    reachedIndex++;

    RegionGridType gridType = VehicleRegionGridManager.GetGridType(data.traverseParms);
    if (data.pathEndMode == PathEndMode.OnCell)
    {
      VehicleRegion region = VehicleRegionAndRoomQuery.RegionAt(data.destination.Cell, pathing, createdFor,
        gridType);
      if (region != null && region.Allows(data.traverseParms))
      {
        destRegions.Add(region);
      }
    }
    else if (data.pathEndMode == PathEndMode.Touch)
    {
      TouchPathEndModeUtilityVehicles.AddAllowedAdjacentRegions(data.destination, data.traverseParms,
        map, createdFor, gridType, destRegions);
    }

    if (destRegions.Count == 0 && data.traverseParms.mode != TraverseMode.PassAllDestroyableThings &&
        data.traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater &&
        data.traverseParms.mode != TraverseMode.PassAllDestroyablePlayerOwnedThings)
    {
      return false;
    }
    
    destRegions.RemoveDuplicates();
    openQueue.Clear();
    DetermineStartRegions(data.start, gridType);

    if (openQueue.Count > 0)
      return true;

    return data.traverseParms.mode is TraverseMode.PassAllDestroyableThings or
      TraverseMode.PassAllDestroyablePlayerOwnedThings or
      TraverseMode.PassAllDestroyableThingsNotWater;
  }

  private bool GetCachedResult(in Data data, out bool result)
  {
    result = false;
    if (!startingRegions.Any() || !destRegions.Any() || !CanUseCache(data.traverseParms.mode))
      return false;

    BoolUnknown cachedResult = GetResult(data.traverseParms);
    switch (cachedResult)
    {
      case BoolUnknown.True:
        result = true;
        return true;
      case BoolUnknown.False:
        result = false;
        return true;
      case BoolUnknown.Unknown:
        break;
      default:
        throw new NotImplementedException(nameof(BoolUnknown));
    }
    return false;

    BoolUnknown GetResult(TraverseParms traverseParms)
    {
      bool anyUnknown = false;
      foreach (VehicleRegion startRegion in startingRegions)
      {
        foreach (VehicleRegion destRegion in destRegions)
        {
          if (startRegion == destRegion)
          {
            return BoolUnknown.True;
          }

          BoolUnknown boolUnknown = cache!.CachedResultFor(startRegion.Room, destRegion.Room, traverseParms);
          if (boolUnknown == BoolUnknown.True)
            return BoolUnknown.True;

          if (boolUnknown == BoolUnknown.Unknown)
          {
            anyUnknown = true;
          }
        }
      }
      return anyUnknown ? BoolUnknown.Unknown : BoolUnknown.False;
    }
  }

  public bool CanReach(in Data data)
  {
    if (!Prepare(data))
      return false;
    if (GetCachedResult(data, out bool canReach))
      return canReach;

    while (openQueue.Count > 0)
    {
      VehicleRegion region = openQueue.Dequeue();
      using ListSnapshot<VehicleRegionLink> links = region.Links;
      foreach (VehicleRegionLink regionLink in links)
      {
        if (RegionReachable(regionLink.regionA, data.traverseParms) ||
            RegionReachable(regionLink.regionB, data.traverseParms))
        {
          return true;
        }
      }
    }

    if (cache != null)
    {
      foreach (VehicleRegion startRegion in startingRegions)
      {
        foreach (VehicleRegion destRegion in destRegions)
        {
          cache.AddCachedResult(startRegion.Room, destRegion.Room, data.traverseParms, false);
        }
      }
    }
    return false;
  }

  private bool RegionReachable(VehicleRegion linkedRegion, in TraverseParms traverseParms)
  {
    if (linkedRegion is null || linkedRegion.reachedIndex == reachedIndex)
      return false;

    if (!linkedRegion.type.Passable() || !linkedRegion.Allows(traverseParms))
      return false;

    if (destRegions.Contains(linkedRegion))
      return true;

    QueueNewOpenRegion(linkedRegion);
    return false;
  }

  private void QueueNewOpenRegion(VehicleRegion region)
  {
    if (region == null)
    {
      Log.Warning("Tried to queue null region (Vehicles).");
      return;
    }

    if (region.reachedIndex == reachedIndex)
    {
      Log.ErrorOnce(
        $"VehicleRegion is already reached; you can't open it. VehicleRegion={region}",
        region.GetHashCode());
      return;
    }

    openQueue.Enqueue(region);
    region.reachedIndex = reachedIndex;
  }

  private void DetermineStartRegions(IntVec3 start, RegionGridType gridType)
  {
    startingRegions.Clear();
    if (pathGrid.WalkableFast(start))
    {
      VehicleRegion validRegionAt = regionGridManager[gridType].GetValidRegionAt(start);
      QueueNewOpenRegion(validRegionAt);
      startingRegions.Add(validRegionAt);
      return;
    }

    for (int i = 0; i < 8; i++)
    {
      IntVec3 cell = start + GenAdj.AdjacentCells[i];
      if (!cell.InBounds(map) || !pathGrid.WalkableFast(cell))
        continue;

      VehicleRegion validRegionAt = regionGridManager[gridType].GetValidRegionAt(cell);
      if (validRegionAt != null && validRegionAt.reachedIndex != reachedIndex)
      {
        QueueNewOpenRegion(validRegionAt);
        startingRegions.Add(validRegionAt);
      }
    }
  }

  public bool CanReachByCell(Data data)
  {
    if (!Prepare(data))
      return false;
    if (GetCachedResult(data, out bool canReach))
      return canReach;

    IntVec3 foundCell = IntVec3.Invalid;
    map.floodFiller.FloodFill(data.start, cell => PassCheck(cell, map, data.traverseParms),
      delegate (IntVec3 cell)
      {
        VehiclePawn vehicle = data.traverseParms.pawn as VehiclePawn;
        if (VehicleReachabilityImmediate.CanReachImmediateVehicle(cell, data.destination, map,
              vehicle!.VehicleDef, data.pathEndMode))
        {
          foundCell = cell;
          return true;
        }

        return false;
      });
    VehicleRegionGrid regionGrid = regionGridManager[VehicleRegionGridManager.GetGridType(data.traverseParms)];
    if (foundCell.IsValid)
    {
      if (CanUseCache(data.traverseParms.mode))
      {
        VehicleRegion validRegionAt = regionGrid.GetValidRegionAt(foundCell);
        if (validRegionAt is not null)
        {
          foreach (VehicleRegion startRegion in startingRegions)
          {
            cache!.AddCachedResult(startRegion.Room, validRegionAt.Room, data.traverseParms, true);
          }
        }
      }
      return true;
    }
    if (CanUseCache(data.traverseParms.mode))
    {
      foreach (VehicleRegion startRegion in startingRegions)
      {
        foreach (VehicleRegion destRegion in destRegions)
        {
          cache!.AddCachedResult(startRegion.Room, destRegion.Room, data.traverseParms, false);
        }
      }
    }
    return false;
  }

  private bool PassCheck(IntVec3 cell, Map map, TraverseParms traverseParms)
  {
    int index = map.cellIndices.CellToIndex(cell);
    if (traverseParms.mode is TraverseMode.PassAllDestroyableThingsNotWater or TraverseMode.NoPassClosedDoorsOrWater
        && cell.GetTerrain(map).IsWater)
    {
      return false;
    }

    if (traverseParms.mode is TraverseMode.PassAllDestroyableThings or TraverseMode.PassAllDestroyableThingsNotWater)
    {
      if (!pathGrid.WalkableFast(index))
      {
        Building edifice = cell.GetEdifice(map);
        if (edifice is null || !VehiclePathFinder.IsDestroyable(edifice))
        {
          return false;
        }
      }
    }
    else if (traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && !pathGrid.WalkableFast(index))
    {
      return false;
    }

    VehicleRegion region = regionGridManager[VehicleRegionGridManager.GetGridType(traverseParms)].DirectGrid[index];
    return region is null || region.Allows(traverseParms);
  }

  private bool CanUseCache(TraverseMode mode)
  {
    if (cache is null)
      return false;

    return mode is not TraverseMode.PassAllDestroyableThingsNotWater
      and not TraverseMode.NoPassClosedDoorsOrWater
      and not TraverseMode.PassAllDestroyableThings;
  }

  public struct Data
  {
    public required IntVec3 start;
    public required LocalTargetInfo destination;
    public required TraverseParms traverseParms;

    public PathEndMode pathEndMode = PathEndMode.OnCell;

    public Data()
    {
    }
  }
}
