using HarmonyLib;
using RimWorld;
using SmashTools.Patching;
using Verse;

namespace Vehicles;

internal class Patch_LordAi : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GatheringsUtility),
        nameof(GatheringsUtility.ShouldGuestKeepAttendingGathering)),
      prefix: new HarmonyMethod(typeof(Patch_LordAi),
        nameof(VehiclesDontParty)));
  }

  public static bool VehiclesDontParty(Pawn p, ref bool __result)
  {
    if (p is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }
}