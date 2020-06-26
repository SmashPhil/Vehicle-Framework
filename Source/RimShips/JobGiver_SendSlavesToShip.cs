using System.Collections.Generic;
using System.Linq;
using Vehicles.Lords;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles.Jobs
{
    public class JobGiver_SendSlavesToShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;
            Pawn pawn2 = this.FindPrisoner(pawn);
            if (pawn2 is null)
                return null;
            Pawn ship = this.FindShipToDeposit(pawn, pawn2);
            VehicleHandler handler = ship.GetComp<CompVehicle>().handlers.Find(x => x.role.handlingTypes.NullOrEmpty());
            return new Job(JobDefOf.PrepareCaravan_GatherPawns, pawn2)
            {
                count = 1
            };
        }

        private Pawn FindPrisoner(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            List<Pawn> prisoners = ((LordJob_FormAndSendCaravanShip)lord.LordJob).prisoners;
            foreach (Pawn slave in prisoners)
            {
                if(slave != pawn && slave.Spawned)
                {
                    if (pawn.CanReserveAndReach(slave, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
                    {
                        return slave;
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
