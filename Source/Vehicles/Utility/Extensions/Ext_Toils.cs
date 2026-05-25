using Verse;
using Verse.AI;

namespace Vehicles;

public static class Ext_Toils
{
  public static T FailOnMoving<T>(this T jobEndable, TargetIndex index) where T : IJobEndable
  {
    jobEndable.AddEndCondition(delegate
    {
      if (jobEndable.GetActor().jobs.curJob?.GetTarget(index).Thing is not Pawn pawn)
        return JobCondition.Errored;

      if (pawn is VehiclePawn vehicle)
      {
        return vehicle.vehiclePather.Moving ? JobCondition.InterruptForced : JobCondition.Ongoing;
      }
      return pawn.pather.Moving ? JobCondition.InterruptForced : JobCondition.Ongoing;
    });
    return jobEndable;
  }
}