using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public static class HediffHelper
	{
		public static bool AttemptToDrown(Pawn pawn)
		{
			if (pawn is VehiclePawn)
			{
				Log.Error($"You cant drown vehicles dummy. Stop it.");
				return false;
			}
			float movementCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
			float manipulationCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
			float capacity = (movementCapacity + manipulationCapacity) / 2;
			if (capacity <= 1.15f)
			{
				float fillage = Mathf.Clamp01(WaterInhaled(capacity));
				if (fillage >= 1 && Rand.Chance(capacity / 2))
				{
					//Small chance to escape insta-death, even smaller chance to hit insta-death again from random value
					fillage = Rand.Range(0.85f, 1);
				}
				foreach (BodyPartRecord bodyPartRecord in pawn.health.hediffSet.GetNotMissingParts().Where(part => part.def.tags.Contains(BodyPartTagDefOf.BreathingSource)))
				{
					Hediff hediff = HediffMaker.MakeHediff(HediffDefOf_Vehicles.VF_Drowning, pawn, bodyPartRecord);
					hediff.Severity = fillage;
					pawn.health.AddHediff(hediff, bodyPartRecord);
				}
				return fillage >= 1f;
			}
			return false;
		}

		public static float WaterInhaled(float movementCapacity)
		{
			if (movementCapacity <= 0.65f)
			{
				return 1;
			}
			else if (movementCapacity >= 1.15f)
			{
				return 0f;
			}
			//1.5 - 1.1x : 1.65 - 1.2x
			return Rand.Range(1.5f, 1.65f) - Rand.Range(1.1f, 1.2f) * movementCapacity;
		}
	}
}
