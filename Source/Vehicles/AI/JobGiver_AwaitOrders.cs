using Vehicles.Defs;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class JobGiver_AwaitOrders : ThinkNode_JobGiver
    {
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AwaitOrders jobGiver_AwaitOrders = (JobGiver_AwaitOrders)base.DeepCopy(resolve);
            jobGiver_AwaitOrders.ticks = this.ticks;
            return jobGiver_AwaitOrders;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if((pawn as VehiclePawn).vPather.Moving)
                return null;
            return new Job(JobDefOf_Vehicles.IdleShip, pawn);
        }

        public int ticks = 250;
    }
}