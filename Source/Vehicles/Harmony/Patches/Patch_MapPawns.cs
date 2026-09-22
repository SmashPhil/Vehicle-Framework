using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SmashTools.Patching;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_MapPawns : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(PawnsFinder),
      nameof(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead)),
      postfix: new HarmonyMethod(typeof(Patch_MapPawns),
      nameof(AllAerialVehicles_AliveOrDead)));
  }

  private static void AllAerialVehicles_AliveOrDead(ref List<Pawn> __result)
  {
    var worldObjHolder = Find.World?.GetComponent<VehicleWorldObjectsHolder>();

    // NOTE: PawnsFinder checks for null World and being in MainMenu scene, presumably because the music manager
    // reaches into PawnsFinder. This causes issues with mods doing something similar, silently skipping over
    // vanilla and throwing a null reference exception in this patch.
    if (worldObjHolder == null)
      return;

    foreach (AerialVehicleInFlight aerialVehicle in worldObjHolder.AerialVehicles)
    {
      __result.AddRange(aerialVehicle.Vehicle.AllPawnsAboard);
    }
  }

}