using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Patching;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_PawnAi : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.ThreatDisabled)),
      postfix: new HarmonyMethod(typeof(Patch_PawnAi),
        nameof(VehicleThreatDisabled)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MentalStateHandler),
        nameof(MentalStateHandler.TryStartMentalState)),
      prefix: new HarmonyMethod(typeof(Patch_PawnAi),
        nameof(EjectPawnForMentalState)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(FloatMenuOptionProvider_CarryingPawn),
      nameof(FloatMenuOptionProvider_CarryingPawn.GetOptionsFor),
      parameters: [ typeof(Thing), typeof(FloatMenuContext)]),
      postfix: new HarmonyMethod(typeof(Patch_PawnAi),
        nameof(GetCarryToVehicle)));
  }

  private static void VehicleThreatDisabled(Pawn __instance, IAttackTargetSearcher disabledFor,
    ref bool __result)
  {
    if (!__result && __instance is VehiclePawn vehicle)
    {
      __result = !vehicle.IsThreatToAttackTargetSearcher(disabledFor);
    }
  }

  private static void EjectPawnForMentalState(MentalStateDef stateDef, Pawn ___pawn)
  {
    if (___pawn.ParentHolder is VehicleRoleHandler handler)
    {
      if (___pawn.IsCaravanMember())
      {
        if (handler.RequiredForMovement)
        {
          Messages.Message(
            TranslatorFormattedStringExtensions.Translate(
              "VF_VehicleCaravanMentalBreakMovementRole", ___pawn),
            MessageTypeDefOf.NegativeEvent);
        }
      }
      else if (!handler.vehicle.vehiclePather.Moving)
      {
        handler.vehicle.DisembarkPawn(___pawn);
      }
    }
  }

  private static IEnumerable<FloatMenuOption> GetCarryToVehicle(IEnumerable<FloatMenuOption> options,
    Thing clickedThing, FloatMenuContext context)
  {
    foreach (FloatMenuOption option in options)
    {
      yield return option;
    }

    if (clickedThing is not VehiclePawn vehicle)
      yield break;

    Pawn actor = context.FirstSelectedPawn;
    if (actor.carryTracker.CarriedThing is not Pawn carriedPawn)
      yield break;

    if (CanCarryPawnToVehicle(actor, carriedPawn, vehicle) is {} carryOption)
    {
      yield return carryOption;
    }
    yield break;

    static FloatMenuOption CanCarryPawnToVehicle(Pawn actor, Pawn carriedPawn, VehiclePawn vehicle)
    {
      if (!actor.CanReach(vehicle, PathEndMode.ClosestTouch, Danger.Deadly))
      {
        return new FloatMenuOption($"{"VF_CantLoadInVehicle".Translate(carriedPawn, vehicle)}: " +
                                   "NoPath".Translate().CapitalizeFirst(), action: null);
      }
      if (!carriedPawn.CanBoardVehicle(vehicle) && !carriedPawn.CanAddToCargo(vehicle))
      {
        return new FloatMenuOption($"{"VF_CantLoadInVehicle".Translate(carriedPawn, vehicle)}: " +
                                   "VF_NoRoleAvailable".Translate().CapitalizeFirst(), action: null);
      }
      return FloatMenuUtility.DecoratePrioritizedTask(
        new FloatMenuOption("VF_CarryToVehicle".Translate(carriedPawn, vehicle), delegate
        {
          Job job = JobMaker.MakeJob(JobDefOf_Vehicles.CarryPawnToVehicle, carriedPawn, vehicle);
          job.ignoreForbidden = true;
          job.count = 1;
          actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }), actor, vehicle);
    }
  }
}