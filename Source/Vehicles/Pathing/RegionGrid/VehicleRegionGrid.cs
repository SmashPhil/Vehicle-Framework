using SmashTools;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

/// <summary>
/// Region grid for vehicle specific regions
/// </summary>
public sealed class VehicleRegionGrid : VehicleGridManager
{
  private const int CleanSquaresPerFrame = 16;

  private readonly ThreadLocal<HashSet<VehicleRegion>> allRegionsYielded = new(() => []);

  // Thread safe - Only used inside UpdateClean
  private int curCleanIndex;

  private VehicleRegion[] regionGrid;

  public readonly ConcurrentSet<VehicleRoom> allRooms = [];

  private VehicleRegionAndRoomUpdater regionUpdater;

  public VehicleRegionGrid(IPathingManager pathing, VehicleDef createdFor, IRegionSource source) : base(pathing,
    createdFor)
  {
    Source = source;
  }

  /// <summary>
  /// Region grid getter
  /// </summary>
  public VehicleRegion[] DirectGrid => regionGrid;

  /// <summary>
  /// Generation config for determining which cells can be joined to region.
  /// </summary>
  public IRegionSource Source { get; private set; }

  /// <summary>
  /// Yield all non-null regions
  /// </summary>
  public IEnumerable<VehicleRegion> AllRegionsNoRebuildInvalidAllowed
  {
    get
    {
      if (regionUpdater is { Enabled: false } or null)
        yield break;

      Assert.IsTrue(allRegionsYielded.Value.Count == 0);
      try
      {
        int count = map.cellIndices.NumGridCells;
        for (int i = 0; i < count; i++)
        {
          VehicleRegion region = GetRegionAt(i);
          if (region != null && allRegionsYielded.Value.Add(region))
          {
            yield return region;
          }
        }
      }
      finally
      {
        allRegionsYielded.Value.Clear();
      }
    }
  }

  internal bool AnyInvalidRegions
  {
    get
    {
      Assert.IsTrue(TestWatcher.RunningTests);
      if (regionUpdater is not { Enabled: true })
        return false;

      foreach (VehicleRegion region in regionGrid)
      {
        if (region is { valid: false })
          return true;
      }
      return false;
    }
  }

  /// <summary>
  /// All valid regions in grid
  /// </summary>
  public void GetAllRegions(List<VehicleRegion> regions)
  {
    if (regionUpdater is { Enabled: false } or null)
      return;
    Assert.IsTrue(allRegionsYielded.Value.Count == 0);
    try
    {
      Parallel.ForEach(Partitioner.Create(0, map.cellIndices.NumGridCells), 
        (range, _) =>
        {
          for (int i = range.Item1; i < range.Item2; i++)
          {
            VehicleRegion region = GetRegionAt(i);
            if (region is { valid: true } && allRegionsYielded.Value.Add(region))
            {
              regions.Add(region);
            }
          }
        });
    }
    finally
    {
      allRegionsYielded.Value.Clear();
    }
  }

  public void Release()
  {
    regionGrid = null;
    allRooms.Clear();
  }

  public void Init(VehicleRegionAndRoomUpdater regionAndRoomUpdater)
  {
    // RegionGrid is large in size and could still be in-use if rebuild-all is called
    // from debug menu. No need to reallocate the entire array if this is the case.
    regionGrid ??= new VehicleRegion[map.cellIndices.NumGridCells];
    regionUpdater = regionAndRoomUpdater;
  }

  /// <summary>
  /// Retrieve valid region at <paramref name="cell"/>
  /// </summary>
  public VehicleRegion GetValidRegionAt(IntVec3 cell, bool rebuild = true)
  {
    if (!cell.InBounds(map))
    {
      Log.Error($"Tried to get valid vehicle region for {createdFor} out of bounds at {cell}");
      return null;
    }

    if (rebuild)
    {
      if (!regionUpdater.Enabled && regionUpdater.AnythingToRebuild)
      {
        Log.Warning($"Trying to get valid vehicle region for {createdFor} at {cell} but " +
          $"RegionAndRoomUpdater is disabled. The result may be incorrect.");
      }

      regionUpdater.TryRebuildVehicleRegions();
    }

    VehicleRegion region = GetRegionAt(cell);
    return region is { valid: true } ? region : null;
  }

  /// <summary>
  /// Get region from grid at <paramref name="cell"/>
  /// </summary>
  public VehicleRegion GetRegionAt(IntVec3 cell)
  {
    int index = map.cellIndices.CellToIndex(cell);
    return GetRegionAt(index);
  }

  /// <summary>
  /// Get region from grid at <paramref name="index"/>
  /// </summary>
  public VehicleRegion GetRegionAt(int index)
  {
    // regionGrid will be null if region set has been suspended from updating
    return regionGrid?[index];
  }

  /// <summary>
  /// Set existing region at <paramref name="cell"/> to <paramref name="region"/>
  /// </summary>
  public void SetRegionAt(IntVec3 cell, VehicleRegion region)
  {
    SetRegionAt(map.cellIndices.CellToIndex(cell), region);
  }

  /// <summary>
  /// Set existing region at <paramref name="index"/> to <paramref name="region"/>
  /// </summary>
  public void SetRegionAt(int index, VehicleRegion region)
  {
    VehicleRegion other = regionGrid[index];
    other?.DecrementRefCount();
    region?.IncrementRefCount();
    Interlocked.CompareExchange(ref regionGrid[index], region, regionGrid[index]);
  }

  /// <summary>
  /// Remove all references of region from grid.
  /// </summary>
  /// <remarks>Prepares region for refresh without moving to object pool.</remarks>
  internal void ClearFromGrid(VehicleRegion region)
  {
    // NOTE - Reference count needs to be set to 0 after region is removed from grid.
    // or else we may encounter unexpected region pooling. Region should be reset
    // after all references have been removed from grid.
    foreach (IntVec3 cell in region.Cells)
    {
      int index = map.cellIndices.CellToIndex(cell);
      Interlocked.CompareExchange(ref regionGrid[index], null, regionGrid[index]);
    }
    region.Reset();
  }

  /// <summary>
  /// Update regionGrid and purge all invalid regions
  /// </summary>
  public void UpdateClean()
  {
    for (int i = 0; i < CleanSquaresPerFrame; i++)
    {
      if (curCleanIndex >= regionGrid.Length)
      {
        curCleanIndex = 0;
      }

      VehicleRegion region = regionGrid[curCleanIndex];
      if (region is { valid: false })
      {
        Trace.Fail("Cleaning region which should have already been returned to pool.");
        SetRegionAt(curCleanIndex, null);
      }
      curCleanIndex++;
    }
  }

  /// <summary>
  /// Draw debug data
  /// </summary>
  public void DebugDraw(DebugRegionType debugRegionType)
  {
    if (map != Find.CurrentMap)
      return;

    foreach (VehicleRoom room in allRooms.Keys)
    {
      room.DebugDraw(debugRegionType);
    }

    if (DebugProperties.DrawAllRegions)
    {
      foreach (VehicleRegion debugRegion in AllRegionsNoRebuildInvalidAllowed)
      {
        debugRegion.DebugDraw(debugRegionType);
      }
    }

    IntVec3 intVec = UI.MouseCell();
    if (intVec.InBounds(map))
    {
      VehicleRegion region = GetRegionAt(intVec);
      region?.DebugDraw(debugRegionType);
    }
  }

  /// <summary>
  /// Draw OnGUI label path costs
  /// </summary>
  public void DebugOnGUI(DebugRegionType debugRegionType)
  {
    IntVec3 intVec = UI.MouseCell();
    if (intVec.InBounds(map))
    {
      VehicleRegion region = GetRegionAt(intVec);
      region?.DebugOnGUIMouseover(debugRegionType);
    }
  }
}