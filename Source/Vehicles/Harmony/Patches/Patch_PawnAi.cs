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
}