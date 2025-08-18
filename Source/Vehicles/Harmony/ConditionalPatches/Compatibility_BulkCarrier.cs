using System;
using HarmonyLib;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_BulkCarrier : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.BulkCarrier;

	public override PatchSequence PatchAt => PatchSequence.Async;

	public override void PatchAll(ModMetaData mod)
	{
		Type classType = AccessTools.TypeByName("BulkCarrier.BulkCarrier");
		HarmonyPatcher.Patch(original: AccessTools.Method(classType, "Capacity_Prefix"),
			postfix: new HarmonyMethod(typeof(Compatibility_BulkCarrier),
				nameof(NoBulkCapacityForVehicles)));
	}

	/// <summary>
	/// Disable BulkCarrier destructive prefix for vehicles
	/// </summary>
	private static void NoBulkCapacityForVehicles(ref bool __result, Pawn p)
	{
		if (p is VehiclePawn)
		{
			__result = true;
		}
	}
}