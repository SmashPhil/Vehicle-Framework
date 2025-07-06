using Verse.AI;

namespace Vehicles;

public class JobDriver_GiveItemToVehicle : JobDriver_LoadVehicle
{
  protected override string ListerTag => ReservationType.LoadTurret;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return base.TryMakePreToilReservations(errorOnFailed) && pawn.Reserve(Vehicle, job, errorOnFailed: errorOnFailed);
  }
}