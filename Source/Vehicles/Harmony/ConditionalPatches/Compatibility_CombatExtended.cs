using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_CombatExtended : ConditionalVehiclePatch
	{
		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type classType = AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_MassUtility_Capacity");
			harmony.Patch(original: AccessTools.Method(classType, "Postfix"),
				prefix: new HarmonyMethod(typeof(Compatibility_CombatExtended),
				nameof(DontOverrideVehicleCapacity)));
		}

		public override string PackageId => CompatibilityPackageIds.CombatExtended;

		/// <summary>
		/// Suppress DualWield errors for vehicles. Should not be applied regardless, disabling the vehicle
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="___pawn"></param>
		/// <param name="exception"></param>
		private static bool DontOverrideVehicleCapacity(Pawn p)
		{
			if (p is VehiclePawn)
			{
				return false;
			}
			return true;
		}
	}
}
