using CoreLib;
using SmashTools;
using SmashTools.Burst;
using Unity.Mathematics;
using UnityEngine;
using Verse;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

public static class FlashMapGrid
{
  public static void FlashListerThings(this Map map)
  {
    foreach (Region region in map.regionGrid.AllRegions)
    {
      if (region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn).Exists(static pawn => pawn is VehiclePawn))
      {
        Draw(region);
      }
    }
    return;

    static void Draw(Region region)
    {
      float a = 1f - (Find.TickManager.TicksGame % 60) / 60f;
      GenDraw.DrawFieldEdges([.. region.Cells], new Color(0f, 0f, 1f, a));
    }
  }

  public static void FlashCoverGrid(this Map map)
  {
    foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
    {
      if (!cell.InBounds(map))
        continue;

      float cover = CoverUtility.TotalSurroundingCoverScore(cell, map);
      map.debugDrawer.FlashCell(cell, cover / 8, cover.ToString("F2"), duration: 1);
    }
  }

  public static void FlashGasGrid(this Map map)
  {
    foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
    {
      if (!cell.InBounds(map))
        continue;
      if (!map.gasGrid.GasCanMoveTo(cell))
        continue;

      float gas = map.gasGrid.DensityPercentAt(cell, GasType.BlindSmoke);
      map.debugDrawer.FlashCell(cell, gas / 8, gas.ToString("F2"), duration: 1);
    }
  }

  public static void FlashClaimants(this Map map)
  {
    VehiclePositionManager manager = map.GetDetachedMapComponent<VehiclePositionManager>();
    //foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
    //{
    //  if (!cell.InBounds(map))
    //    continue;
    //  if (manager.ClaimedBy(cell) is not { } claimant)
    //    continue;

    //  float colorPct = claimant.Position == cell ? 1 : 0.5f;
    //  map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
    //}

    foreach (VehiclePawn vehicle in manager.AllClaimants)
    {
      if (vehicle.FullRotation.IsDiagonal)
      {
        IntVec3 pos = vehicle.Position;
        IntVec2 size = vehicle.VehicleDef.Size;
        foreach (int2 cellVec in new EntityRect(pos.x, pos.z, size.x, size.z, vehicle.FullRotation))
        {
          IntVec3 cell = new(cellVec.x, 0, cellVec.y);
          float colorPct = vehicle.Position == cell ? 1 : 0.5f;
          map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
        }
      }
      else
      {
        foreach (IntVec3 cell in vehicle.VehicleRect())
        {
          float colorPct = vehicle.Position == cell ? 1 : 0.5f;
          map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
        }
      }
    }
  }

  public static void FlashThingGrid(this Map map)
  {
    foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
    {
      if (!cell.InBounds(map))
        continue;

      Thing thing = map.thingGrid.ThingAt(cell, ThingCategory.Pawn);
      if (thing is not VehiclePawn)
        continue;

      map.debugDrawer.FlashCell(cell, 1, duration: 1);
    }
  }

  public static void FlashModifierGrid(this Map map)
  {
    VehiclePathingSystem pathing = map.GetCachedMapComponent<VehiclePathingSystem>();
    if (!IsFeatureEnabled(PathFinderV2))
      return;

    foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
    {
      if (!cell.InBounds(map))
        continue;

      Modifier mod = pathing.PathFinderManager.ModifierGrid.GetModifier(map.cellIndices.CellToIndex(cell));
      if (mod.type == ModifierType.None)
        continue;

      float colorPct = ((float)mod.value + short.MaxValue) / (short.MaxValue * 2f);
      map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
    }
  }
}
