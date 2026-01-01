using System.Collections.Generic;
using CoreLib;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace Vehicles;

/// <summary>
/// Region and room update handler
/// </summary>
public class VehicleRegionAndRoomUpdater : VehicleGridManager
{
  private readonly List<VehicleRegion> newRegions = [];
  private readonly List<VehicleRegion> currentRegionGroup = [];

  private readonly HashSet<VehicleRoom> reusedOldRooms = [];

  private VehicleRegionGridManager regionGridManager;

  public VehicleRegionAndRoomUpdater(VehiclePathingSystem mapping, VehicleDef createdFor)
    : base(mapping, createdFor)
  {
  }

  /// <summary>
  /// Updater has been initialized
  /// </summary>
  public bool Initialized { get; private set; }

  /// <summary>
  /// Updater is currently updating dirty regions
  /// </summary>
  public bool UpdatingRegion { get; private set; }

  /// <summary>
  /// Updater has finished initial build
  /// </summary>
  public bool Enabled { get; private set; }

  /// <summary>
  /// Anything in RegionGrid that needs to be rebuilt
  /// </summary>
  public bool AnythingToRebuild
  {
    get
    {
      if (UpdatingRegion || !Enabled) return false;
      return !Initialized || mapping[createdFor].VehicleRegionDirtyer.AnyDirty;
    }
  }

  public void Init()
  {
    if (!mapping[createdFor].VehiclePathGrid.Enabled &&
      !mapping.GridOwners.TryForfeitOwnership(createdFor))
    {
      Trace.Fail("Trying to initialize region grids with no vehicle to claim ownership.");
      return;
    }

    Enabled = true;
    regionGridManager = mapping[createdFor].VehicleRegionGridManager;
    regionGridManager.Init();
  }

  public void Release()
  {
    Initialized = false;
    Enabled = false;
    regionGridManager.Release();
  }

  /// <summary>
  /// Should only be called for map generation so spawn events don't attempt to rebuild regions. 
  /// </summary>
  public void Disable()
  {
    Enabled = false;
  }

  /// <summary>
  /// Rebuild all regions
  /// </summary>
  public void RebuildAllVehicleRegions()
  {
    if (!Enabled)
    {
      Log.Warning(
        $"Called RebuildAllVehicleRegions but VehicleRegionAndRoomUpdater is disabled. " +
        $"VehicleRegions won't be rebuilt. StackTrace: {StackTraceUtility.ExtractStackTrace()}");
    }

    mapping[createdFor].VehicleRegionDirtyer.SetAllDirty();
    TryRebuildVehicleRegions();
  }

  /// <summary>
  /// Rebuild all regions on the map and generate associated rooms
  /// </summary>
  public void TryRebuildVehicleRegions()
  {
    if (UpdatingRegion || !Enabled)
      return;

    UpdatingRegion = true;
    if (!Initialized)
    {
      mapping[createdFor].VehicleRegionDirtyer.SetAllDirty();
    }
    else if (!mapping[createdFor].VehicleRegionDirtyer.AnyDirty)
    {
      UpdatingRegion = false;
      return;
    }

    try
    {
      RegenerateNewVehicleRegions();
      CreateOrUpdateVehicleRooms();
    }
    finally
    {
      Initialized = true;
      UpdatingRegion = false;
    }
  }

  /// <summary>
  /// Generate regions with dirty cells
  /// </summary>
  [Profile]
  private void RegenerateNewVehicleRegions()
  {
    newRegions.Clear();
    regionGridManager.RegenerateDirtyRegions(newRegions);
  }

  /// <summary>
  /// Update procedure for Rooms associated with Vehicle based regions
  /// </summary>
  [Profile]
  private void CreateOrUpdateVehicleRooms()
  {
    using ClearOnDispose<VehicleRoom> cod = new(reusedOldRooms);
    int numRegionGroups = CombineNewRegions();
    CreateOrAttachToExistingRooms(numRegionGroups);
    CombineNewAndReusedRooms();
  }

  /// <summary>
  /// Combine rooms together with room group criteria met
  /// </summary>
  private void CombineNewAndReusedRooms()
  {
    int count = 0;
    foreach (VehicleRegion region in newRegions)
    {
      if (region.newRegionGroupIndex >= 0)
        continue;

      VehicleRegionTraverser.FloodAndSetNewRegionIndex(region, count);
      count++;
    }
  }

  /// <summary>
  /// Create new room or attach to existing room with predetermined number of region groups
  /// </summary>
  /// <param name="numRegionGroups"></param>
  private void CreateOrAttachToExistingRooms(int numRegionGroups)
  {
    for (int i = 0; i < numRegionGroups; i++)
    {
      currentRegionGroup.Clear();
      foreach (VehicleRegion newRegion in newRegions)
      {
        if (newRegion.newRegionGroupIndex == i)
        {
          currentRegionGroup.Add(newRegion);
        }
      }

      VehicleRegion firstRegion = currentRegionGroup[0];
      if (!firstRegion.type.AllowsMultipleRegionsPerDistrict())
      {
        if (currentRegionGroup.Count != 1)
        {
          Log.Error(
            "Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
        }

        VehicleRoom portalRoom = VehicleRoom.MakeNew(mapping.map, createdFor, firstRegion.gridType);
        firstRegion.Room = portalRoom;
        return;
      }

      VehicleRoom room = FindCurrentRegionGroupNeighborWithMostRegions(out bool multipleOldNeighborRooms);
      if (room is null)
      {
        room = VehicleRoom.MakeNew(mapping.map, createdFor, firstRegion.gridType);
        VehicleRegionTraverser.FloodAndSetRooms(firstRegion, mapping.map, createdFor, room);
      }
      else if (!multipleOldNeighborRooms)
      {
        foreach (VehicleRegion region in currentRegionGroup)
        {
          region.Room = room;
        }

        reusedOldRooms.Add(room);
      }
      else
      {
        VehicleRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], mapping.map, createdFor, room);
        reusedOldRooms.Add(room);
      }
    }
  }

  /// <summary>
  /// Combine regions that meet region group criteria
  /// </summary>
  private int CombineNewRegions()
  {
    int count = 0;
    foreach (VehicleRegion region in newRegions)
    {
      if (region.newRegionGroupIndex >= 0)
        continue;

      VehicleRegionTraverser.FloodAndSetNewRegionIndex(region, count);
      count++;
    }
    return count;
  }

  /// <summary>
  /// Find neighboring region group with most regions
  /// </summary>
  /// <param name="multipleOldNeighborRooms"></param>
  private VehicleRoom FindCurrentRegionGroupNeighborWithMostRegions(
    out bool multipleOldNeighborRooms)
  {
    multipleOldNeighborRooms = false;
    VehicleRoom room = null;
    foreach (VehicleRegion root in currentRegionGroup)
    {
      foreach (VehicleRegion region in root.NeighborsOfSameType)
      {
        if (region.Room == null || reusedOldRooms.Contains(region.Room))
          continue;

        if (room == null)
        {
          room = region.Room;
        }
        else if (region.Room != room)
        {
          multipleOldNeighborRooms = true;
          if (region.Room.RegionCount > room.RegionCount)
          {
            room = region.Room;
          }
        }
      }
    }
    return room;
  }
}