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
		}

		public static void AerialVehicleInFlightAltimeter(ISelectable sel, Rect rect)
		{
			if (sel is AerialVehicleInFlight aerialVehicle)
			{
				AltitudeMeter.DrawAltitudeMeter(aerialVehicle);
			}
		}
	}
}
