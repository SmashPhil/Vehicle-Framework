using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace Vehicles
{
    public class LordToil_AssaultColonyWithArmor
    {
        public class LordToil_AssaultColony : LordToil
		{
			public override bool ForceHighStoryDanger
			{
				get
				{
					return true;
				}
			}

			public LordToil_AssaultColony(bool attackDownedIfStarving = false)
			{
				this.attackDownedIfStarving = attackDownedIfStarving;
			}

			public override bool AllowSatisfyLongNeeds
			{
				get
				{
					return false;
				}
			}

			public override void Init()
			{
				base.Init();
				LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
			}

			public override void UpdateAllDuties()
			{
				for (int i = 0; i < lord.ownedPawns.Count; i++)
				{
					lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
					lord.ownedPawns[i].mindState.duty.attackDownedIfStarving = attackDownedIfStarving;
				}
			}

			private bool attackDownedIfStarving;
		}
    }
}
