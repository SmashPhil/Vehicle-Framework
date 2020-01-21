using System.Linq;
using RimShips.Defs;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_LeaveShip : LordToil
    {
        public LordToil_PrepareCaravan_LeaveShip(IntVec3 exitSpot)
        {
            this.exitSpot = exitSpot;
        }

        public override bool AllowSatisfyLongNeeds => false;

        public override float? CustomWakeThreshold => new float?(0.5f);

        public override bool AllowRestingInBed => false;

        public override bool AllowSelfTend => false;

        public override void UpdateAllDuties()
        {
            foreach(Pawn p in this.lord.ownedPawns)
            {
                if(!ShipHarmony.IsShip(p))
                    this.lord.LordJob.Notify_PawnLost(p, PawnLostCondition.LeftVoluntarily);
                p.mindState.duty = new PawnDuty(DutyDefOf_Ships.TravelOrWaitOcean, this.exitSpot, -1f)
                {
                    locomotion = LocomotionUrgency.Jog
                };
                p.GetComp<CompShips>().ResolveSeating();
                p.drafter.Drafted = true;
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                GatherAnimalsAndSlavesForShipsUtility.CheckArrived(this.lord, this.lord.ownedPawns.Where(x => ShipHarmony.IsShip(x)).ToList(), this.exitSpot, "ReadyToExitMap", (Pawn x) => true, true, null);
            }
        }

        private IntVec3 exitSpot;
    }
}
