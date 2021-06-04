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
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(InspectPaneFiller), nameof(InspectPaneFiller.DoPaneContentsFor)),
				postfix: new HarmonyMethod(typeof(WorldObjects),
				nameof(AerialVehicleInFlightAltimeter)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(World), nameof(World.WorldUpdate)),
				postfix: new HarmonyMethod(typeof(WorldObjects),
				nameof(TextMeshWorldUpdate)));
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
