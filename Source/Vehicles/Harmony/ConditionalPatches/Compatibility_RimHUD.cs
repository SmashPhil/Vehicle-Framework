using System;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	internal class Compatibility_RimHUD : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.RimHUD;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type classType = AccessTools.TypeByName("RimHUD.Interface.InspectPanePlus");
			harmony.Patch(AccessTools.Method(classType, "DrawMedicalButton"),
				prefix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(VehiclesDontDrawMedicalButton)));
		}

		private static bool VehiclesDontDrawMedicalButton(Pawn pawn, Rect rect)
		{
			if (pawn is VehiclePawn)
			{
				return false;
			}
			return true;
		}
	}
}
