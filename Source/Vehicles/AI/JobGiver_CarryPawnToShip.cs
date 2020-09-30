using System.Collections.Generic;
using System.Linq;
using Vehicles.Defs;
using Vehicles.Lords;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Jobs
{
    public class JobGiver_CarryPawnToShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;
            if(!(pawn.GetLord().LordJob is LordJob_FormAndSendVehicles))
                return null;
            Pawn pawn2 = FindDownedPawn(pawn);
            if (pawn2 is null)
                return null;
            VehiclePawn vehicle = FindShipToDeposit(pawn, pawn2);
            VehicleHandler handler = vehicle.GetCachedComp<CompVehicle>().handlers.Find(x => x.role.handlingTypes.NullOrEmpty());
            return new Job(JobDefOf_Vehicles.CarryPawnToVehicle, pawn2, vehicle)
            {
                count = 1
            };
        }

        private Pawn FindDownedPawn(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            List<Pawn> downedPawns = ((LordJob_FormAndSendVehicles)lord.LordJob).downedPawns;
            foreach(Pawn comatose in downedPawns)
            {
                if(comatose.Downed && comatose != pawn && comatose.Spawned)
                {
                    if(pawn.CanReserveAndReach(comatose, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
                    {
                        return comatose;
                    }
                }
            }
            return null;
        }

        private VehiclePawn FindShipToDeposit(Pawn pawn, Pawn downedPawn)
        {
            List<VehiclePawn> vehicles = pawn.GetLord().ownedPawns.Where(x => x is VehiclePawn).Cast<VehiclePawn>().ToList();
            return vehicles.MaxBy(x => x.GetCachedComp<CompVehicle>().Props.roles.Find(y => y.handlingTypes.NullOrEmpty()).slots);
        }
    }
}
