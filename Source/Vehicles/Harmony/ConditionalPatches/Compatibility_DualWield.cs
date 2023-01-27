using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_DualWield : ConditionalVehiclePatch
	{
		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), "UpdateRotation"), prefix: null, postfix: null, transpiler: null,
				finalizer: new HarmonyMethod(typeof(Compatibility_DualWield),
				nameof(NoRotationCallForVehicles)));
		}

		public override string PackageId => CompatibilityPackageIds.DualWield;

		/// <summary>
		/// Suppress DualWield errors for vehicles. Should not be applied regardless, disabling the vehicle
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="___pawn"></param>
		/// <param name="exception"></param>
		/// <returns></returns>
		private static Exception NoRotationCallForVehicles(Pawn ___pawn, Exception __exception)
		{
			if (___pawn is VehiclePawn && __exception != null)
			{
				return null;
			}
			return __exception;
		}
	}
}
