using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Lords
{
    public class LordToil_PrepareCaravan_GatherDownedPawnsVehicle : LordToil
    {
        public LordToil_PrepareCaravan_GatherDownedPawnsVehicle(IntVec3 meetingPoint, IntVec3 exitSpot)
        {
            this.meetingPoint = meetingPoint;
            this.exitSpot = exitSpot;
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
                if(p.IsColonist)
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.PrepareCaravan_GatherDownedPawns, this.meetingPoint, this.exitSpot, -1f);
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint, -1f);
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                List<Pawn> downedPawns = ((LordJob_FormAndSendVehicles)lord.LordJob).downedPawns;
                List<VehiclePawn> vehicles = ((LordJob_FormAndSendVehicles)lord.LordJob).vehicles;
                List<Pawn> pawnsOnShips = new List<Pawn>();

                foreach(VehiclePawn p in vehicles)
                {
                    pawnsOnShips.AddRange(p.GetCachedComp<CompVehicle>().AllPawnsAboard);
                }

                if(pawnsOnShips.Intersect(downedPawns).Count() == downedPawns.Count())
                { 
                    this.lord.ReceiveMemo("AllDownedPawnsGathered");
                }

            }
        }

        private IntVec3 meetingPoint;

        private IntVec3 exitSpot;
    }
}
