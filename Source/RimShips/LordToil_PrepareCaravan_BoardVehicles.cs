using System.Collections.Generic;
using System.Linq;
using Vehicles.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Lords
{
    public class LordToil_PrepareCaravan_BoardVehicles : LordToil
    {
        public LordToil_PrepareCaravan_BoardVehicles(IntVec3 meetingPoint)
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
                if(!HelperMethods.IsVehicle(p))
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
                List<Pawn> pawnsLeft = this.lord.ownedPawns.Where(x => !HelperMethods.IsVehicle(x)).ToList();
                IntVec3 intVec = this.meetingPoint;
                
                if(!pawnsLeft.Any(x => x.Spawned))
                {
                    this.lord.ownedPawns.RemoveAll(x => !HelperMethods.IsVehicle(x));
                    this.lord.ReceiveMemo("AllPawnsOnboard");
                }
            }
        }

        private IntVec3 meetingPoint;
    }
}
