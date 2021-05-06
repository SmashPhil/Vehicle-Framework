using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class WorldObjects : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectDef), nameof(WorldObjectDef.ConfigErrors)), prefix: null,
				postfix: new HarmonyMethod(typeof(WorldObjects),
				nameof(SettlementAirDefenseConfigError)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(InspectPaneFiller), nameof(InspectPaneFiller.DoPaneContentsFor)),
				postfix: new HarmonyMethod(typeof(WorldObjects),
				nameof(AerialVehicleInFlightAltimeter)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(World), nameof(World.WorldUpdate)),
				postfix: new HarmonyMethod(typeof(WorldObjects),
				nameof(TextMeshWorldUpdate)));
		}

		public static IEnumerable<string> SettlementAirDefenseConfigError(IEnumerable<string> __result, WorldObjectDef __instance)
		{
			foreach (string prevError in __result)
			{
				yield return prevError;
			}
			if (__instance.comps.NotNullAndAny(c => c is WorldObjectCompProperties_SettlementAirDefense) && (__instance.worldObjectClass is null || !__instance.worldObjectClass.SameOrSubclass(typeof(Settlement))))
			{
				yield return "cannot assign \"SettlementAirDefense\" WorldObjectComp to WorldObject not using or inheriting from Settlement class.";
			}
		}

		public static void AerialVehicleInFlightAltimeter(ISelectable sel, Rect rect)
		{
			if (sel is AerialVehicleInFlight aerialVehicle)
			{
				AltitudeMeter.DrawAltitudeMeter(aerialVehicle);
			}
		}

		public static void TextMeshWorldUpdate()
		{
			if (VehicleMod.settings.debug.debugGenerateWorldPathCostTexts)
			{
				WorldPathTextMeshGenerator.UpdateVisibility();
			}
		}
	}
}
