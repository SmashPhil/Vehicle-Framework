using System;
using HarmonyLib;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_DualWield : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.DualWield;

	public override PatchSequence PatchAt => PatchSequence.Async;

	public override void PatchAll(ModMetaData mod)
	{
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), "UpdateRotation"),
			finalizer: new HarmonyMethod(typeof(Compatibility_DualWield),
				nameof(NoRotationCallForVehicles)));
	}

	/// <summary>
	/// Suppress DualWield errors for vehicles. Should not be applied regardless, disabling the vehicle
	/// </summary>
	private static Exception NoRotationCallForVehicles(Pawn ___pawn, Exception __exception)
	{
		if (___pawn is VehiclePawn && __exception != null)
		{
			return null;
		}
		return __exception;
	}
}