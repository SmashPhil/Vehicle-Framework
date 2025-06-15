using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using SmashTools;
using Verse;
using Verse.Sound;

namespace Vehicles;

internal static class DebugActions
{
  [DebugAction(VehicleHarmony.VehiclesLabel, allowedGameStates = AllowedGameStates.Playing)]
  private static void ClearRegionCache()
  {
    LongEventHandler.QueueLongEvent(delegate()
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      foreach (Map map in Find.Maps)
      {
        VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
        foreach (VehicleDef vehicleDef in VehicleHarmony.AllMoveableVehicleDefs)
        {
          mapping[vehicleDef].VehicleReachability.ClearCache();
        }
      }
    }, "Clearing Region Cache", false, null);
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, allowedGameStates = AllowedGameStates.Playing)]
  private static void FlashPathCosts()
  {
    List<DebugMenuOption> options = [];
    options.Add(new DebugMenuOption("Vanilla", DebugMenuOptionMode.Action, delegate()
    {
      FlashPathCostsFor(null);
      Find.WindowStack.WindowOfType<Dialog_RadioButtonMenu>()?.Close();
    }));

    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading.OrderBy(def =>
        def.modContentPack.ModMetaData.SamePackageId(VehicleHarmony.VehiclesUniqueId,
          ignorePostfix: true))
     .ThenBy(def => def.modContentPack.Name)
     .ThenBy(d => d.defName))
    {
      options.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Action,
        delegate() { FlashPathCostsFor(vehicleDef); }));
    }

    Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, "Vehicle Defs"));
    return;

    static void FlashPathCostsFor(VehicleDef vehicleDef)
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      SoundDefOf.Click.PlayOneShotOnCamera();
      if (Find.CurrentMap is Map map)
      {
        if (vehicleDef == null)
        {
          foreach (IntVec3 cell in map.AllCells)
          {
            int cost = map.pathing.Normal.pathGrid.Cost(cell);
            map.debugDrawer.FlashCell(cell, cost / 500f, cost.ToString());
          }
        }
        else
        {
          VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
          foreach (IntVec3 cell in map.AllCells)
          {
            int cost = mapping[vehicleDef].VehiclePathGrid.PerceivedPathCostAt(cell);
            map.debugDrawer.FlashCell(cell, cost / 500f, cost.ToString());
          }
        }
      }
    }
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, allowedGameStates = AllowedGameStates.Playing)]
  private static void RegenerateAllGrids()
  {
    LongEventHandler.QueueLongEvent(delegate()
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      foreach (Map map in Find.Maps)
      {
        VehiclePathingSystem mapping = MapComponentCache<VehiclePathingSystem>.GetComponent(map);
        mapping.RegenerateGrids(VehiclePathingSystem.GridSelection.All,
          VehiclePathingSystem.GridDeferment.Forced);
      }
    }, "Regenerating Regions", true, null);
  }
}