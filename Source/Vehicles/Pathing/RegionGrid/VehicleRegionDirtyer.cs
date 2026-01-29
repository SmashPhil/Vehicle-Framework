using System;
using System.Collections.Generic;
using CoreLib.Performance;
using SmashTools;
using Verse;

namespace Vehicles;

/// <summary>
/// Region dirtyer handler for recaching
/// </summary>
public class VehicleRegionDirtyer : VehicleGridManager
{
  private VehicleRegionMaker regionMaker;
  private VehicleRegionGridManager regionGridManager;

  private readonly ConcurrentSet<IntVec3> dirtyCells = [];

  public VehicleRegionDirtyer(VehiclePathingSystem mapping, VehicleDef createdFor) : base(mapping,
    createdFor)
  {
  }

  /// <summary>
  /// Any dirty cells registered
  /// </summary>
  public bool AnyDirty => dirtyCells.Count > 0;

  internal IEnumerable<IntVec3> ConsumeDirtyCells()
  {
    // It's fine if this isn't a snapshot as this enumeration only occurs for cells being used for region generation.
    // New dirty cells will be picked up in another pass.
    foreach ((IntVec3 cell, _) in dirtyCells)
    {
      yield return cell;
    }
    dirtyCells.Clear();
  }

  public override void PostInit()
  {
    regionMaker = mapping[createdFor].VehicleRegionMaker;
    regionGridManager = mapping[createdFor].VehicleRegionGridManager;
  }

  /// <summary>
  /// Set all cells and regions to dirty status
  /// </summary>
  internal void SetAllDirty()
  {
    dirtyCells.Clear();
    foreach (IntVec3 cell in mapping.map)
    {
      dirtyCells.Add(cell);
    }

    foreach (RegionGridType gridType in VehicleRegionGridManager.AllGridTypes)
    {
      foreach (VehicleRegion region in regionGridManager[gridType].AllRegionsNoRebuildInvalidAllowed)
      {
        SetRegionDirty(region, addCellsToDirtyCells: false);
      }
    }
  }

  /// <summary>
  /// Notify that the walkable status at <paramref name="cell"/> has changed.
  /// </summary>
  public void NotifyWalkabilityChanged(IntVec3 cell)
  {
    int padding = createdFor.SizePadding;
    if (padding == 0 && createdFor.Size.x == 2)
    {
      // For 2 width vehicles we need to dirty surrounding tiles for region edges in order to regenerate links properly.
      // South edges will have padding applied, so we must pad by 2 in order to reach across in those cases, otherwise
      // those regions won't be dirtied, and the southern link won't connect.
      padding = 2;
    }
    CellRect paddingRect = CellRect.CenteredOn(cell, padding);
    foreach (IntVec3 adjCell in paddingRect)
    {
      if (!adjCell.InBounds(mapping.map))
        continue;

      foreach (RegionGridType gridType in VehicleRegionGridManager.AllGridTypes)
      {
        VehicleRegion region = regionGridManager[gridType].GetRegionAt(adjCell);
        if (region is { valid: true })
        {
          SetRegionDirty(region);
        }
        else
        {
          dirtyCells.Add(adjCell);
        }
      }
    }
  }

  public void NotifyThingAffectingRegionsSpawned(CellRect occupiedRect)
  {
    if (mapping[createdFor].Suspended) 
      return;

    foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1).ClipInsideMap(mapping.map))
    {
      foreach (RegionGridType gridType in VehicleRegionGridManager.AllGridTypes)
      {
        VehicleRegion validRegion = regionGridManager[gridType].GetValidRegionAt(cell, rebuild: false);
        if (validRegion != null)
        {
          SetRegionDirty(validRegion);
        }
      }
    }
  }

  public void NotifyThingAffectingRegionsDespawned(CellRect occupiedRect)
  {
    if (mapping[createdFor].Suspended) 
      return;

    foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1)
     .ClipInsideMap(mapping.map))
    {
      foreach (RegionGridType gridType in VehicleRegionGridManager.AllGridTypes)
      {
        VehicleRegion validRegion = regionGridManager[gridType].GetValidRegionAt(cell, rebuild: false);
        if (validRegion != null)
        {
          SetRegionDirty(validRegion);
        }
      }
    }
  }

  /// <summary>
  /// Set <paramref name="region"/> to dirty status, marking it for update
  /// </summary>
  private void SetRegionDirty(VehicleRegion region, bool addCellsToDirtyCells = true,
    bool dirtyLinkedRegions = false)
  {
    try
    {
      if (!region.valid) 
        return;

      region.valid = false;
      region.Room = null;

      using ListSnapshot<VehicleRegionLink> links = region.Links;
      foreach (VehicleRegionLink regionLink in links)
      {
        regionLink.Deregister(region);
        if (!regionLink.IsValid)
        {
          regionMaker.Return(regionLink);
        }

        VehicleRegion otherRegion = regionLink.GetOtherRegion(region);
        if (otherRegion != null && dirtyLinkedRegions)
        {
          SetRegionDirty(otherRegion, addCellsToDirtyCells: addCellsToDirtyCells,
            dirtyLinkedRegions: false);
        }
      }

      if (addCellsToDirtyCells)
      {
        foreach (IntVec3 intVec in region.Cells)
        {
          dirtyCells.Add(intVec);
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown in SetRegionDirty. Exception={ex}");
    }
  }
}