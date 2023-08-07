using System;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;
using RimWorld;

namespace Vehicles
{
	internal class Compatibility_RimHUD : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.RimHUD;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type inspectPaneUtilityType = AccessTools.TypeByName("RimHUD.Patch.RimWorld_InspectPaneUtility_InspectPaneOnGUI");
			harmony.Patch(AccessTools.Method(inspectPaneUtilityType, "Prefix"),
				prefix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(DontRenderRimHUDForVehicles_InspectPaneUtility)));

			Type inspectPaneFillerType = AccessTools.TypeByName("RimHUD.Patch.RimWorld_InspectPaneFiller_DoPaneContentsFor");
			harmony.Patch(AccessTools.Method(inspectPaneFillerType, "Prefix"),
				prefix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(DontRenderRimHUDForVehicles_InspectPaneFiller)));
		}

		private static bool DontRenderRimHUDForVehicles_InspectPaneUtility(Rect inRect, IInspectPane pane, ref bool __result)
		{
			if (Find.Selector.SingleSelectedThing is VehiclePawn)
			{
				__result = true;
				return false;
			}
			return true;
		}

		private static bool DontRenderRimHUDForVehicles_InspectPaneFiller(ISelectable sel, Rect rect, ref bool __result)
		{
			if (sel is VehiclePawn)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}
}
