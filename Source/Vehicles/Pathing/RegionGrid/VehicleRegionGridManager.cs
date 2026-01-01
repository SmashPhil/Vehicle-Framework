using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.Config;
using Verse;

namespace Vehicles;

public sealed class VehicleRegionGridManager : VehicleGridManager
{
  public static readonly RegionGridType[] AllGridTypes;

  private readonly VehicleRegionGrid normal;
  private readonly VehicleRegionGrid breach;

  private VehiclePathingSystem.VehiclePathData pathData;

  static VehicleRegionGridManager()
  {
    if (FeatureFlags.RaidersEnabled)
    {
      AllGridTypes = [.. Enum.GetValues(typeof(RegionGridType)).Cast<RegionGridType>()];
    }
    else
    {
      AllGridTypes = [RegionGridType.Normal];
    }
  }
  public VehicleRegionGridManager(VehiclePathingSystem pathingSystem, VehicleDef vehicleDef) : base(pathingSystem, vehicleDef)
  {
    normal = new VehicleRegionGrid(pathingSystem, vehicleDef, new RegionSourceNormal());
    if (FeatureFlags.RaidersEnabled)
    {
      breach = new VehicleRegionGrid(pathingSystem, vehicleDef, new RegionSourceBreach());
    }
  }

  public VehicleRegionGrid this[RegionGridType gridType]
  {
    get
    {
      return gridType switch
      {
        RegionGridType.Normal => normal,
        RegionGridType.Breach => breach,
        _ => throw new NotImplementedException(gridType.ToString()),
      };
    }
  }

  public static RegionGridType GetGridType(TraverseParms traverseParms)
  {
    if (!FeatureFlags.RaidersEnabled)
      return RegionGridType.Normal;

    if (traverseParms.mode is TraverseMode.PassAllDestroyableThings or TraverseMode.PassAllDestroyablePlayerOwnedThings)
      return RegionGridType.Breach;

    return RegionGridType.Normal;
  }

  public void Init()
  {
    normal.Init();
    if (FeatureFlags.RaidersEnabled)
    {
      breach.Init();
    }
  }

  public override void PostInit()
  {
    pathData = mapping[createdFor];

    normal.PostInit();
    if (FeatureFlags.RaidersEnabled)
    {
      breach.PostInit();
    }
  }

  public void Release()
  {
    normal.Release();
    if (FeatureFlags.RaidersEnabled)
    {
      breach.Release();
    }
  }

  public void RegenerateDirtyRegions(List<VehicleRegion> newRegions)
  {
    foreach (IntVec3 cell in pathData.VehicleRegionDirtyer.ConsumeDirtyCells())
    {
      if (!cell.InBounds(mapping.map))
      {
        Trace.Fail($"Dirtied invalid cell at {cell}");
        continue;
      }
      RegenerateAt(cell, RegionGridType.Normal, newRegions);
      if (FeatureFlags.RaidersEnabled)
      {
        RegenerateAt(cell, RegionGridType.Breach, newRegions);
      }
    }
  }

  private void RegenerateAt(IntVec3 cell, RegionGridType gridType, List<VehicleRegion> newRegions)
  {
    VehicleRegionGrid regionGrid = this[gridType];
    VehicleRegion region = regionGrid.GetRegionAt(cell);

    // ObjectPool should never hold a region which still has references in the region grid.
    Assert.IsTrue(region is not { InPool: true }, $"{region} has been returned to pool prematurely.");

    if (region is { valid: true })
      return;

    RegionResult result = pathData.VehicleRegionMaker.TryGenerateRegionFrom(cell, gridType, ref region);
    switch (result)
    {
      case RegionResult.Success:
      {
        newRegions.Add(region);
      }
        break;
      case RegionResult.NoRegion:
      {
        // Clean immediately rather than following RimWorld convention of delayed
        // Update-based clean.
        if (region != null)
        {
          regionGrid.SetRegionAt(cell, null);
        }
      }
        break;
      case RegionResult.Failed:
        Log.Error($"Failed to create region at {cell}");
        break;
      default:
        throw new NotImplementedException(result.ToString());
    }
  }
}