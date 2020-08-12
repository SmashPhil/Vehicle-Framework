using RimWorld;
using System.Linq;
using Vehicles.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Lords
{
    public class LordToil_PrepareCaravan_LeaveWithVehicles : LordToil
    {
        public LordToil_PrepareCaravan_LeaveWithVehicles(IntVec3 exitSpot)
        {
            this.exitSpot = exitSpot;
        }

        public override bool AllowSatisfyLongNeeds => false;

        public override float? CustomWakeThreshold => new float?(0.5f);

        public override bool AllowRestingInBed => false;

        public override bool AllowSelfTend => false;

        public override void UpdateAllDuties()
        {
            foreach(Pawn p in lord.ownedPawns)
            {
                if(p.IsVehicle())
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle, exitSpot, -1f)
                    {
                        locomotion = LocomotionUrgency.Jog
                    };
                    p.drafter.Drafted = true;
                }
                //else
                //{
                //    p.mindState.duty = new PawnDuty(DutyDefOf.TravelOrWait, exitSpot)
                //    {
                //        locomotion = LocomotionUrgency.Jog
                //    };
                //}
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                GatherAnimalsAndSlavesForShipsUtility.CheckArrived(lord, lord.ownedPawns.Where(x => HelperMethods.IsVehicle(x)).ToList(), exitSpot, "ReadyToExitMap", (Pawn x) => true, lord.ownedPawns.AnyNullified(b => HelperMethods.IsBoat(b)), null);
            }
        }

        private IntVec3 exitSpot;
    }
}
