using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
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
			if (___pawn.ParentHolder is VehicleHandler handler)
			{
				if (___pawn.IsCaravanMember())
				{
					if (handler.RequiredForMovement)
					{
						Messages.Message(TranslatorFormattedStringExtensions.Translate("Vehicles_VehicleCaravanMentalBreakMovementRole", ___pawn),MessageTypeDefOf.NegativeEvent);
					}
				}
				else
				{
					handler.vehicle.DisembarkPawn(___pawn);
				}
			}
		}
	}
}
