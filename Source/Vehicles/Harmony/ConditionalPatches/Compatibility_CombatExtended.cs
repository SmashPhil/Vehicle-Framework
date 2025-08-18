using System;
using HarmonyLib;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_CombatExtended : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.CombatExtended;

	public override PatchSequence PatchAt => PatchSequence.Async;

	public override void PatchAll(ModMetaData mod)
	{
		Type classType =
			AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_MassUtility_Capacity");
		HarmonyPatcher.Patch(original: AccessTools.Method(classType, "Postfix"),
			prefix: new HarmonyMethod(typeof(Compatibility_CombatExtended),
				nameof(DontOverrideVehicleCapacity)));
	}

	/// <summary>
	/// Suppress DualWield errors for vehicles. Should not be applied regardless, disabling the vehicle
	/// </summary>
	private static bool DontOverrideVehicleCapacity(Pawn p)
	{
		if (p is VehiclePawn)
		{
			return false;
		}
		return true;
	}
}