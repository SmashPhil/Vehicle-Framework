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
				return true;
			}
			float movementCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
			float manipulationCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
			float capacity = (movementCapacity + manipulationCapacity) / 2;
			if (capacity <= 1.15f)
			{
				return Rand.Chance(InstantDeathChance(capacity));
			}
			return false;
		}

		public static float InstantDeathChance(float movementCapacity)
		{
			return Mathf.Clamp01(movementCapacity - 0.65f);
		}
	}
}
