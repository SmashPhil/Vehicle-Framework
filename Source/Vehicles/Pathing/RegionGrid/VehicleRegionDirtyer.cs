using System;
using System.Collections.Generic;
using SmashTools;
using SmashTools.Performance;
using Verse;

namespace Vehicles
{
  /// <summary>
  /// Region dirtyer handler for recaching
  /// </summary>
  public class VehicleRegionDirtyer : VehicleGridManager
  {
    private VehicleRegionMaker regionMaker;

    private readonly ConcurrentSet<IntVec3> dirtyCells = [];

    // Thread Safe - only called accessible within the same thread through AsyncAction
    // or directly called from PathingHelper (w/ multithreading disabled)
    private readonly HashSet<VehicleRegion> regionsToDirty = [];

    public VehicleRegionDirtyer(VehiclePathingSystem mapping, VehicleDef createdFor) : base(mapping,
      createdFor)
    {
    }

    /// <summary>
    /// Any dirty cells registered
    /// </summary>
    public bool AnyDirty => dirtyCells.Count > 0;

    public IEnumerable<IntVec3> DirtyCells
    {
      get
      {
        // Lock-free enumeration of dirty cells. It's fine if this isn't a snapshot
        // as this enumeration only occurs for cells being used for region generation.
        foreach ((IntVec3 cell, _) in dirtyCells)
        {
          yield return cell;
        }

        dirtyCells.Clear();
      }
    }

    public override void PostInit()
    {
      regionMaker = mapping[createdFor].VehicleRegionMaker;
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

      foreach (VehicleRegion region in mapping[createdFor].VehicleRegionGrid
       .AllRegionsNoRebuildInvalidAllowed)
      {
        SetRegionDirty(region, addCellsToDirtyCells: false);
      }
    }

    /// <summary>
    /// Notify that the walkable status at <paramref name="cell"/> has changed
    /// </summary>
    public void NotifyWalkabilityChanged(IntVec3 cell)
    {
      // Pad 1 even if vehicle has no region padding, we still want to dirty
      // surrounding tiles for region edges and regenerating links.
      int padding = createdFor.SizePadding > 0 ? createdFor.SizePadding : 1;
      CellRect paddingRect = CellRect.CenteredOn(cell, padding);
      foreach (IntVec3 adjCell in paddingRect)
      {
        if (adjCell.InBounds(mapping.map))
        {
          VehicleRegion region = mapping[createdFor].VehicleRegionGrid.GetRegionAt(adjCell);
          if (region != null && region.valid)
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
      if (mapping[createdFor].Suspended) return;

      foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1)
       .ClipInsideMap(mapping.map))
      {
        VehicleRegion validRegion = mapping[createdFor].VehicleRegionGrid
         .GetValidRegionAt(cell, rebuild: false);
        if (validRegion != null)
        {
          SetRegionDirty(validRegion);
        }
      }
    }

    public void NotifyThingAffectingRegionsDespawned(CellRect occupiedRect)
    {
      if (mapping[createdFor].Suspended) return;

      foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1)
       .ClipInsideMap(mapping.map))
      {
        if (cell.InBounds(mapping.map))
        {
          VehicleRegion validRegion = mapping[createdFor].VehicleRegionGrid
           .GetValidRegionAt(cell, rebuild: false);
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
        if (!region.valid) return;

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
}