using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_BulkCarrier : ConditionalVehiclePatch
	{
		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type classType = AccessTools.TypeByName("BulkCarrier.BulkCarrier");
			harmony.Patch(original: AccessTools.Method(classType, "Capacity_Prefix"),
				postfix: new HarmonyMethod(typeof(Compatibility_BulkCarrier),
				nameof(NoBulkCapacityForVehicles)));
		}

		public override string PackageId => CompatibilityPackageIds.BulkCarrier;

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
}
