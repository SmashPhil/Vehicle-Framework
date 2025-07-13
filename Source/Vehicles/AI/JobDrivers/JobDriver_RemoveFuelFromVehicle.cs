using System.Collections.Generic;
using Verse.AI;

namespace Vehicles;

public class JobDriver_RemoveFuelFromVehicle : JobDriver
{
  private const int RemovingFuelDuration = 120;

  private VehiclePawn Vehicle => job.GetTarget(TargetIndex.A).Thing as VehiclePawn;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(Vehicle, job, 1, -1, null, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    this.FailOnMoving(TargetIndex.A);
    AddEndCondition(delegate
    {
      if (Vehicle.CompFueledTravel.CanEjectFuel)
        return JobCondition.Ongoing;

      return JobCondition.Succeeded;
    });
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
     .FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
    yield return Toils_General.Wait(RemovingFuelDuration).WithProgressBarToilDelay(TargetIndex.A)
     .FailOnDespawnedOrNull(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    yield return Toils_General.Do(Vehicle.CompFueledTravel.EjectFuel);
  }
}