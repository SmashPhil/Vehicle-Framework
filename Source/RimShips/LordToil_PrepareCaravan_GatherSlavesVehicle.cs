using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Vehicles.Defs;

namespace Vehicles.Lords
{
    public class LordToil_PrepareCaravan_GatherSlavesVehicle : LordToil
    {
        public LordToil_PrepareCaravan_GatherSlavesVehicle(IntVec3 meetingPoint)
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
                if(HelperMethods.IsVehicle(p))
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_WaitShip);
                }
                else if(!p.RaceProps.Animal && !p.IsColonist && (!HelperMethods.IsVehicle(p)))
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_SendSlavesToShip, this.meetingPoint, -1f);
                    p.mindState.duty.pawnsToGather = PawnsToGather.Slaves;
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, this.meetingPoint, -1f);
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                Lord lord = this.lord;
                List<Pawn> pawns = this.lord.ownedPawns.Where(x => !HelperMethods.IsVehicle(x)).ToList();

                if(!pawns.Any(x => !x.IsColonist && !x.RaceProps.Animal && x.Spawned))
                    lord.ReceiveMemo("AllSlavesGathered");
            }
        }

        private IntVec3 meetingPoint;
    }
}
