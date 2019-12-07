using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimShips.Defs;
using RimShips.Jobs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_GatherShip : LordToil
    {
        public LordToil_PrepareCaravan_GatherShip(IntVec3 meetingPoint)
        {
            this.meetingPoint = meetingPoint;
        }

        public override float? CustomWakeThreshold
        {
            get
            {
                return new float?(0.5f);
            }
        }

        public override bool AllowRestingInBed
        {
            get
            {
                return false;
            }
        }

        public override void UpdateAllDuties()
        {
            foreach(Pawn pawn in this.lord.ownedPawns)
            {
                if(pawn.IsColonist)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_GatherShip);
                }
                else if(pawn.RaceProps.Animal)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_BoardShip);
                }
                else if(!(pawn.GetComp<CompShips>() is null))
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_WaitShip);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                }
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if(Find.TickManager.TicksGame % 120 == 0)
            {
                bool flag = true;
                List<Pawn> capablePawns = this.lord.ownedPawns.Where(x => !x.Downed && !x.Dead).ToList();
                foreach(Pawn pawn in capablePawns)
                {
                    if(pawn.IsColonist && pawn.mindState.lastJobTag != JobTag.WaitingForOthersToFinishGatheringItems)
                    {
                        flag = false;
                        break;
                    }
                }
                if(flag)
                {
                    foreach(Pawn pawn in base.Map.mapPawns.AllPawnsSpawned)
                    {
                        if(pawn.CurJob != null && pawn.jobs.curDriver is JobDriver_PrepareCaravan_GatheringShip && pawn.CurJob.lord == this.lord)
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if(flag)
                {
                    this.lord.ReceiveMemo("AllItemsGathered");
                }
            }
        }

        private IntVec3 meetingPoint;
    }
}
