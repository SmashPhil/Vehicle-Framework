using Verse;
using Verse.AI;

namespace Vehicles
{
	public class JobGiver_AwaitOrders : ThinkNode_JobGiver
	{
		public int ticks = 250;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AwaitOrders jobGiver_AwaitOrders = (JobGiver_AwaitOrders)base.DeepCopy(resolve);
			jobGiver_AwaitOrders.ticks = ticks;
			return jobGiver_AwaitOrders;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn is VehiclePawn vehicle)
			{
				if (vehicle.vehiclePather.Moving)
				{
					return null;
				}
				return new Job(JobDefOf_Vehicles.IdleVehicle, pawn);
			}
			return null;
		}
	}
}