using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld.Planet;
using HarmonyLib;

namespace Vehicles
{
	internal class PawnAI : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState)),
				prefix: new HarmonyMethod(typeof(PawnAI),
				nameof(EjectPawnForMentalState)));
		}

		private static void EjectPawnForMentalState(MentalStateDef stateDef, Pawn ___pawn)
		{
			if (___pawn.GetVehicle() is VehiclePawn vehicle)
			{
				if (___pawn.IsCaravanMember())
				{
					//Switch seats? Hault caravan? Eject?
				}
				else
				{
					vehicle.DisembarkPawn(___pawn);
				}
			}
		}
	}
}
