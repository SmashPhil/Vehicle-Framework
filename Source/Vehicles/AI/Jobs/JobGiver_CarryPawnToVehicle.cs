using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class JobGiver_CarryPawnToVehicle : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				return null;
			}
			if (!(pawn.GetLord().LordJob is LordJob_FormAndSendVehicles lordJob))
			{
				return null;
			}
			Pawn downedPawn = FindDownedPawn(pawn);
			if (downedPawn is null)
			{ 
				return null;
			}
			(VehiclePawn vehicle, VehicleHandler handler) = lordJob.GetVehicleAssigned(downedPawn);
			if (vehicle is null || handler is null)
			{
				(vehicle, handler) = FindAvailableVehicle(downedPawn);
			}
			if (vehicle is null || handler is null)
			{
				Log.ErrorOnce($"Unable to locate assigned or available vehicle for {downedPawn} in Caravan. Removing from caravan.", lordJob.GetHashCode());
				lordJob.lord.RemovePawn(downedPawn);
				return null;
			}
			Job_Vehicle job = new Job_Vehicle(JobDefOf_Vehicles.CarryPawnToVehicle, downedPawn, vehicle)
			{
				handler = handler,
				count = 1
			};

			return job;
		}

		private Pawn FindDownedPawn(Pawn pawn)
		{
			Lord lord = pawn.GetLord();
			List<Pawn> downedPawns = ((LordJob_FormAndSendVehicles)lord.LordJob).downedPawns;
			foreach(Pawn comatose in downedPawns)
			{
				if (comatose.Downed && comatose != pawn && comatose.Spawned)
				{
					if (pawn.CanReserveAndReach(comatose, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
					{
						return comatose;
					}
				}
			}
			return null;
		}

		private (VehiclePawn vehicle, VehicleHandler handler) FindAvailableVehicle(Pawn pawn)
		{
			Lord lord = pawn.GetLord();
			LordJob_FormAndSendVehicles lordJob = (LordJob_FormAndSendVehicles)lord.LordJob;
			foreach (VehiclePawn vehicle in lordJob.vehicles)
			{
				foreach (VehicleHandler handler in vehicle.handlers)
				{
					if (handler.CanOperateRole(pawn) && !lordJob.SeatAssigned(vehicle, handler))
					{
						return (vehicle, handler);
					}
				}
			}
			return (null, null);
		}
	}
}
