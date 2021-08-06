using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class CompatibilityPatch_DualWield : IConditionalPatch
	{
		public void PatchAll(ModMetaData mod, Harmony harmony)
		{
			harmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), "UpdateRotation"), prefix: null, postfix: null, transpiler: null,
				finalizer: new HarmonyMethod(typeof(CompatibilityPatch_DualWield),
				nameof(NoRotationCallForVehicles)));
		}

		public string PackageId => "Roolo.DualWield";

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
