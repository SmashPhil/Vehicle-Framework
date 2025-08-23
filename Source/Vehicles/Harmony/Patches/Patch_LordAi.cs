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
		// NOTE - Hospitality patches over this completely with the assumption that only androids would skip food needs checks.
		// Setting this to higher priority will at least let vehicles exit early before Hospitality's destructive patch.
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(GatheringsUtility),
				nameof(GatheringsUtility.ShouldGuestKeepAttendingGathering)),
			prefix: new HarmonyMethod(AccessTools.Method(typeof(Patch_LordAi), nameof(VehiclesDontParty)),
				priority: Priority.First));
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