using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using SmashTools.Patching;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_NpcPathing : IPatchCategory
{
  private static readonly Type jobDriverGotoDisplayClassType;

  static Patch_NpcPathing()
  {
    jobDriverGotoDisplayClassType =
      typeof(JobDriver_Goto).GetNestedTypes(AccessTools.all).FirstOrDefault();
    Assert.IsNotNull(jobDriverGotoDisplayClassType);
  }

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
#if RAIDERS
    if (VehicleMod.settings.debug.debugAllowRaiders)
    {
      // Compiler generated methods from JobDriver_Goto::<>c__DisplayClass1_0
      List<MethodInfo> gotoMethods = jobDriverGotoDisplayClassType.GetDeclaredMethods();
      // <MakeNewToils>b__0
      MethodInfo makeToilsDelegate0 = gotoMethods[0];
      Assert.IsTrue(makeToilsDelegate0.Name == "<MakeNewToils>b__0");
      HarmonyPatcher.Patch(original: makeToilsDelegate0,
        postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
          nameof(GotoToilsFirstExit)));
      // <MakeNewToils>b__6
      MethodInfo makeToilsDelegate6 = gotoMethods[6];
      Assert.IsTrue(makeToilsDelegate6.Name == "<MakeNewToils>b__6");
      HarmonyPatcher.Patch(original: makeToilsDelegate6,
        postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
          nameof(GotoToilsSecondExit)));
    }
#endif
  }

  private static void GotoToilsFirstExit(
    JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */)
  {
    TryExitMapForVehicle(__instance, false, true);
  }

  private static void GotoToilsSecondExit(
    JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */)
  {
    TryExitMapForVehicle(__instance, true, true);
  }

  private static void TryExitMapForVehicle(
    JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */,
    bool onEdge, bool onExitCell)
  {
    // Sticking with compiler generated notation here for ease of debugging
    JobDriver_Goto __this =
      Traverse.Create(__instance).Field("<>4__this").GetValue<JobDriver_Goto>();
    if (__this.pawn is VehiclePawn vehicle && __this.job.exitMapOnArrival && vehicle.Spawned)
    {
      Rot4 rot = CellRect.WholeMap(vehicle.Map).GetClosestEdge(vehicle.Position);
      // Only need to check 1 cell per edge, if 1 is touching then all on that edge will be.
      if (vehicle.PawnOccupiedCells(vehicle.Position, rot).Corners.Any(cell =>
        (onEdge && cell.OnEdge(vehicle.Map)) ||
        (onExitCell && vehicle.Map.exitMapGrid.IsExitCell(cell))))
      {
        PathingHelper.ExitMapForVehicle(vehicle, __this.job);
      }
    }
  }
}