using System.Collections.Generic;
using RimShips.Lords;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips.Jobs
{
    public class JobDriver_IdleShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override string GetReport()
        {
            return  ( !(Ship is null) ) ? "AwaitOrders".Translate() : base.GetReport();
        }

        private CompShips Ship
        {
            get
            {
                Thing thing = job.GetTarget(TargetIndex.B).Thing;
                if (thing is null) return null;
                return thing.TryGetComp<CompShips>();
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = new Toil();
            wait.initAction = delegate ()
            {
                base.Map.pawnDestinationReservationManager.Reserve(this.pawn, this.job, this.pawn.Position);
                this.pawn.pather.StopDead();
            };
            wait.tickAction = delegate ()
            {
                if((Find.TickManager.TicksGame + this.pawn.thingIDNumber) % JobSearchInterval == 0)
                { 
                    this.CheckForCaravan();
                }
            };
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            yield return wait;
            yield break;
        }

        private void CheckForCaravan()
        {
            if(!(this.pawn.GetLord() is null) && this.pawn.GetLord().LordJob is LordJob_FormAndSendCaravanShip)
            {
                if(this.pawn.GetLord().CurLordToil is LordToil_PrepareCaravan_LeaveShip)
                {
                    if(this.pawn.GetComp<CompShips>().CanMove)
                    {
                        this.pawn.drafter.Drafted = true;
                    } 
                }
            }
        }

        private const int JobSearchInterval = 100;
    }
}