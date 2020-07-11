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
            foreach(Pawn p in lord.ownedPawns)
            {
                if(!HelperMethods.IsVehicle(p))
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_BoardShip)
                    {
                        locomotion = LocomotionUrgency.Jog
                    };
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitShip);
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 200 == 0)
            {
                bool flag = true;
                List<Pawn> pawns = new List<Pawn>(lord.ownedPawns.Where(p => !p.IsVehicle()));
                foreach(Pawn pawn in pawns)
                {
                    var vehicle = (lord.LordJob as LordJob_FormAndSendVehicles).GetVehicleAssigned(pawn);
                    if(vehicle.Second != null)
                    {
                        if(vehicle.First.GetComp<CompVehicle>().AllPawnsAboard.Contains(pawn))
                        {
                            lord.ownedPawns.Remove(pawn);
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                }
                if(flag)
                {
                    lord.ReceiveMemo("AllPawnsOnboard");
                }
            }
        }

        private IntVec3 meetingPoint;
    }
}
