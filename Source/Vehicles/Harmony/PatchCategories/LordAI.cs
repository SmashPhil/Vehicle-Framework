using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine;

namespace Vehicles
{
	internal class LordAI : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GatheringsUtility), nameof(GatheringsUtility.ShouldGuestKeepAttendingGathering)),
				prefix: new HarmonyMethod(typeof(LordAI),
				nameof(VehiclesDontParty)));
		}

		public static bool VehiclesDontParty(Pawn p, ref bool __result)
		{
			if (p is VehiclePawn)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}
