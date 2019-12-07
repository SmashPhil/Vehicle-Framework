using System.Collections.Generic;
using System.Linq;
using RimShips.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_BoardShip : LordToil
    {
        public LordToil_PrepareCaravan_BoardShip(IntVec3 meetingPoint)
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
            foreach(Pawn p in this.lord.ownedPawns)
            {
                if(!ShipHarmony.IsShip(p))
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_BoardShip)
                    {
                        locomotion = LocomotionUrgency.Jog
                    };
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_WaitShip);
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 200 == 0)
            {
                Lord lord = this.lord;
                List<Pawn> pawnsLeft = this.lord.ownedPawns.Where(x => !ShipHarmony.IsShip(x)).ToList();
                IntVec3 intVec = this.meetingPoint;
                
                if(!pawnsLeft.Any(x => x.Spawned))
                {
                    this.lord.ownedPawns.RemoveAll(x => !ShipHarmony.IsShip(x));
                    this.lord.ReceiveMemo("AllPawnsOnboard");
                }
            }
        }

        private IntVec3 meetingPoint;
    }
}
