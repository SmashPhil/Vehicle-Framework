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
        return new Job(JobDefOf.FollowClose, assignedSeat.Vehicle)
        {
          lord = pawn.GetLord(),
          expiryInterval = 140,
          checkOverrideOnExpire = true,
          followRadius = FollowRadius
        };
      }
      return new Job(JobDefOf_Vehicles.Board, assignedSeat.Vehicle);
    }
    return null;
  }
}