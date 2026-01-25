using SmashTools;
using SmashTools.Burst;
using UnityEngine;
using Verse;

namespace Vehicles;

public static class FlashMapGrid
{
  extension(Map map)
  {
    public void FlashListerThings()
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

    public void FlashCoverGrid()
    {

      foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
      {
        if (!cell.InBounds(map))
          continue;

        float cover = CoverUtility.TotalSurroundingCoverScore(cell, map);
        map.debugDrawer.FlashCell(cell, cover / 8, cover.ToString("F2"), duration: 1);
      }
    }

    public void FlashGasGrid()
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

    public void FlashClaimants()
    {
      VehiclePositionManager manager = map.GetDetachedMapComponent<VehiclePositionManager>();
      foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
      {
        if (!cell.InBounds(map))
          continue;
        if (manager.ClaimedBy(cell) is not { } claimant)
          continue;

        float colorPct = claimant.Position == cell ? 1 : 0.5f;
        map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
      }
    }

    public void FlashThingGrid()
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

    public void FlashModifierGrid()
    {
      VehiclePathingSystem pathing = map.GetCachedMapComponent<VehiclePathingSystem>();
      foreach (IntVec3 cell in Find.CameraDriver.CurrentViewRect)
      {
        if (!cell.InBounds(map))
          continue;

        Modifier mod = pathing.ModifierGrid.GetModifier(map.cellIndices.CellToIndex(cell));
        if (mod.type == ModifierType.None)
          continue;

        float colorPct = ((float)mod.value + short.MaxValue) / (short.MaxValue * 2f);
        map.debugDrawer.FlashCell(cell, colorPct, duration: 1);
      }
    }
  }
}
