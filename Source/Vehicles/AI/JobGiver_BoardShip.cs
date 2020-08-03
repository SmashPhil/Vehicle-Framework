using System.Linq;
using RimWorld;
using Vehicles.Defs;
using Vehicles.Lords;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Jobs
{
    public class  JobGiver_BoardShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) return null;
            if(pawn.GetLord().LordJob is LordJob_FormAndSendVehicles)
            {
                Pair<VehiclePawn, VehicleHandler> vehicle = ((LordJob_FormAndSendVehicles)pawn.GetLord().LordJob).GetVehicleAssigned(pawn);

                if(vehicle.Second is null)
                {
                    if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, vehicle.First, FollowRadius))
                        return null;
                    return new Job(JobDefOf.FollowClose, vehicle.First)
                    {
                        lord = pawn.GetLord(),
                        expiryInterval = 140,
                        checkOverrideOnExpire = true,
                        followRadius = FollowRadius
                    };
                }
                return new Job(JobDefOf_Vehicles.Board, vehicle.First);
            }
            return null;
        }

        public const float FollowRadius = 5;
    }
}
