using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles
{
  public class JobGiver_AwaitOrders : ThinkNode_JobGiver
  {
    private int overrideExpiryInterval = -1;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
      JobGiver_AwaitOrders jobGiverAwaitOrders = (JobGiver_AwaitOrders)base.DeepCopy(resolve);
      jobGiverAwaitOrders.overrideExpiryInterval = overrideExpiryInterval;
      return jobGiverAwaitOrders;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
      VehiclePawn vehicle = pawn as VehiclePawn;
      Assert.IsNotNull(vehicle, "Trying to assign vehicle job to non-vehicle pawn.");

      if (vehicle.vehiclePather.Moving)
      {
        vehicle.vehiclePather.EngageBrakes();
      }

      Job job = JobMaker.MakeJob(JobDefOf_Vehicles.IdleVehicle, vehicle);
      job.checkOverrideOnExpire = true;
      job.expiryInterval = overrideExpiryInterval > 0 ? overrideExpiryInterval : 180;
      return job;
    }
  }
}