using System.Linq;
using RimWorld;
using Vehicles.Lords;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class JobGiver_BoardVehicle : ThinkNode_JobGiver
	{
		public const float FollowRadius = 5;

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) return null;
			if(pawn.GetLord().LordJob is LordJob_FormAndSendVehicles)
			{
				(VehiclePawn vehicle, VehicleHandler handler) vehicleMapping = ((LordJob_FormAndSendVehicles)pawn.GetLord().LordJob).GetVehicleAssigned(pawn);

				if (vehicleMapping.handler is null)
				{
					if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, vehicleMapping.vehicle, FollowRadius))
						return null;
					return new Job(JobDefOf.FollowClose, vehicleMapping.vehicle)
					{
						lord = pawn.GetLord(),
						expiryInterval = 140,
						checkOverrideOnExpire = true,
						followRadius = FollowRadius
					};
				}
				return new Job(JobDefOf_Vehicles.Board, vehicleMapping.vehicle);
			}
			return null;
		}
	}
}
