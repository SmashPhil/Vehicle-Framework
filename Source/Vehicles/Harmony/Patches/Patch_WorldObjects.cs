using HarmonyLib;
using RimWorld.Planet;
using SmashTools.Patching;
using UnityEngine;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_WorldObjects : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    //HarmonyPatcher.Patch(original: AccessTools.Method(typeof(InspectPaneFiller), nameof(InspectPaneFiller.DoPaneContentsFor)),
    //	postfix: new HarmonyMethod(typeof(WorldObjects),
    //	nameof(AerialVehicleInFlightAltimeter)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.GetAdjustedTarget)),
      postfix: new HarmonyMethod(typeof(Patch_WorldObjects),
        nameof(GetAdjustedTargetForAerialVehicle)));
  }

  private static void AerialVehicleInFlightAltimeter(ISelectable sel)
  {
    if (sel is AerialVehicleInFlight aerialVehicle)
    {
      AltitudeMeter.DrawAltitudeMeter(aerialVehicle);
    }
  }

  private static void GetAdjustedTargetForAerialVehicle(GlobalTargetInfo target,
    ref GlobalTargetInfo __result)
  {
    if (target.HasThing && target.Thing.ParentHolder is VehicleRoleHandler handler &&
      handler.vehicle.GetAerialVehicle() is { } aerialVehicle)
    {
      __result = aerialVehicle;
    }
  }
}