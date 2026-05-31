using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class JobGiver_SendSlavesToVehicle : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
				return null;
			Pawn pawn2 = FindPrisoner(pawn);
			if (pawn2 is null)
				return null;
			VehiclePawn vehicle = FindShipToDeposit(pawn, pawn2);
			VehicleRoleHandler handler = vehicle.handlers.Find(x => x.role.HandlingTypes == HandlingType.None);
      Job job = JobMaker.MakeJob(JobDefOf.PrepareCaravan_GatherDownedPawns, pawn2);
      job.count = 1;
      return job;
    }

		private Pawn FindPrisoner(Pawn pawn)
		{
			Lord lord = pawn.GetLord();
			List<Pawn> prisoners = ((LordJob_FormAndSendVehicles)lord.LordJob).prisoners;
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

		private VehiclePawn FindShipToDeposit(Pawn pawn, Pawn downedPawn)
		{
			List<VehiclePawn> vehicles = pawn.GetLord().ownedPawns.Where(x => x is VehiclePawn).Cast<VehiclePawn>().ToList();
			return vehicles.MaxBy(x => x.VehicleDef.properties.roles.Find(y => y.HandlingTypes == HandlingType.None).Slots);
		}
	}
}
