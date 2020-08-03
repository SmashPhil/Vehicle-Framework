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
            Pawn pawn2 = this.FindDownedPawn(pawn);
            if (pawn2 is null)
                return null;
            Pawn ship = this.FindShipToDeposit(pawn, pawn2);
            VehicleHandler handler = ship.GetComp<CompVehicle>().handlers.Find(x => x.role.handlingTypes.NullOrEmpty());
            return new Job(JobDefOf_Vehicles.CarryPawnToShip, pawn2, ship)
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

        private Pawn FindShipToDeposit(Pawn pawn, Pawn downedPawn)
        {
            List<Pawn> ships = pawn.GetLord().ownedPawns.Where(x => HelperMethods.IsVehicle(x)).ToList();
            return ships.MaxBy(x => x.GetComp<CompVehicle>().Props.roles.Find(y => y.handlingTypes.NullOrEmpty()).slots);
        }
    }
}
