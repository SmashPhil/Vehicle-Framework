using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[UsedWithReflection]
public class JobGiver_BoardVehicle : ThinkNode_JobGiver
{
  private const float FollowRadius = 5;

  protected override Job TryGiveJob(Pawn pawn)
  {
    if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
      return null;

    if (pawn.GetLord().LordJob is IVehicleAssigner assigner)
    {
      AssignedSeat assignedSeat = assigner.GetAssignedSeat(pawn);
      if (assignedSeat?.Vehicle is null)
        return null;

      if (assignedSeat.handler is null)
      {
        if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, assignedSeat.Vehicle,
          FollowRadius))
          return null;
        Job job = JobMaker.MakeJob(JobDefOf.FollowClose, assignedSeat.Vehicle);
        job.lord = pawn.GetLord();
        job.expiryInterval = 140;
        job.checkOverrideOnExpire = true;
        job.followRadius = FollowRadius;
      }
      return JobMaker.MakeJob(JobDefOf_Vehicles.Board, assignedSeat.Vehicle);
    }
    return null;
  }
}