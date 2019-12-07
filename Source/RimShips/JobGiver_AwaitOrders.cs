using RimShips.Defs;
using Verse;
using Verse.AI;

namespace RimShips.Jobs
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
            if(pawn?.pather?.Moving ?? false)
                return null;
            return new Job(JobDefOf_Ships.IdleShip);
        }

        public int ticks = 250;
    }
}