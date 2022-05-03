using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	internal class PawnAI : IPatchCategory
	{
		public void PatchMethods()
		{
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(AnimalPenUtility), nameof(AnimalPenUtility.RopeAttachmentInteractionCell)),
			//	prefix: new HarmonyMethod(typeof(PawnAI),
			//	nameof(RopeAttachmentInteractionCellVehicle)));
		}
	}
}
