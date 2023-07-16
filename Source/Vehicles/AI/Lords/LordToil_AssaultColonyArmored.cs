using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class LordToil_AssaultColonyArmored : LordToil
	{
		public override bool ForceHighStoryDanger => true;

		public override bool AllowSatisfyLongNeeds => false;

		public override void Init()
		{
			base.Init();
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
		}

		public override void UpdateAllDuties()
		{
			var pawns = lord.ownedPawns.Where(p => !(p is VehiclePawn));
			var vehicles = lord.ownedPawns.Where(p => p is VehiclePawn);
			foreach (VehiclePawn vehicle in vehicles)
			{
				vehicle.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony); //CHANGE
			}
			foreach (Pawn pawn in pawns)
			{
				pawn.mindState.duty = new PawnDuty(DutyDefOf.Follow);
			}
		}
	}
}
