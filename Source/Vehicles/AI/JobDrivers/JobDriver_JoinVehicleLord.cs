using System.Collections.Generic;
using JetBrains.Annotations;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[UsedWithReflection]
internal class JobDriver_JoinVehicleLord : JobDriver
{
  private VehiclePawn Vehicle => TargetB.Thing as VehiclePawn;

  private void AssignLord(JobCondition condition)
  {
    if (condition is not JobCondition.Succeeded)
      return;

    Lord lord = Vehicle.GetLord();
    if (!lord.ownedPawns.Contains(pawn) && lord.CanAddPawn(pawn))
    {
      lord.AddPawn(pawn);
    }
  }

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return Vehicle.GetLord() != null;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnCannotReach(TargetIndex.A, PathEndMode.Touch);
    this.FailOnCannotReach(TargetIndex.B, PathEndMode.ClosestTouch);
    this.FailOnDestroyedOrNull(TargetIndex.B);
    this.FailOnMoving(TargetIndex.B);

    AddFinishAction(AssignLord);

    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
  }
}
