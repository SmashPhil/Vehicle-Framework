using Vehicles.Defs;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;


namespace Vehicles.Jobs
{
    public class JobGiver_PrepareCaravan_GatheringShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            Lord lord = pawn.GetLord();
            Thing thing = GatherItemsForShipCaravanUtility.FindThingToHaul(pawn, lord);
            if (thing is null)
            {
                return null;
            }
            return new Job(JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle, thing)
            {
                lord = lord
            };
        }
    }
}
