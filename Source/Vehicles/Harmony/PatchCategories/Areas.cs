using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Areas : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(AreaManager), nameof(AreaManager.AddStartingAreas)),
				postfix: new HarmonyMethod(typeof(Areas),
				nameof(AddVehicleAreas)));
		}

		private static void AddVehicleAreas(AreaManager __instance)
		{
			Ext_Map.TryAddAreas(__instance.map);
		}
	}
}