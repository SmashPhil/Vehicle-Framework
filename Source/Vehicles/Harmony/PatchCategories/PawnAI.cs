using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using SmashTools;

namespace Vehicles
{
	internal class PawnAI : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.ThreatDisabled)),
				postfix: new HarmonyMethod(typeof(PawnAI),
				nameof(VehicleThreatDisabled)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState)),
				prefix: new HarmonyMethod(typeof(PawnAI),
				nameof(EjectPawnForMentalState)));
		}

		private static void VehicleThreatDisabled(Pawn __instance, IAttackTargetSearcher disabledFor, ref bool __result)
		{
			if (!__result && __instance is VehiclePawn vehicle)
			{
				__result = !vehicle.IsThreatToAttackTargetSearcher(disabledFor);
			}
		}

		private static void EjectPawnForMentalState(MentalStateDef stateDef, Pawn ___pawn)
		{
			if (___pawn.ParentHolder is VehicleHandler handler)
			{
				if (___pawn.IsCaravanMember())
				{
					if (handler.RequiredForMovement)
					{
						Messages.Message(TranslatorFormattedStringExtensions.Translate("VF_VehicleCaravanMentalBreakMovementRole", ___pawn),MessageTypeDefOf.NegativeEvent);
					}
				}
				else if (!handler.vehicle.vehiclePather.Moving)
				{
					handler.vehicle.DisembarkPawn(___pawn);
				}
			}
		}
	}
}
