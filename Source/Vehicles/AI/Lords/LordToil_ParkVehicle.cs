using Verse;

namespace Vehicles;

public sealed class LordToil_ParkVehicle : LordToil_TravelToDestination
{
  public LordToil_ParkVehicle(VehiclePawn vehicle, IntVec3 destination) : base(vehicle, destination)
  {
  }

  protected override void ArrivedAtDest()
  {
    base.ArrivedAtDest();
    vehicle.ignition.Drafted = false;
    vehicle.DisembarkAll();
    // TODO - Mark all new inventory items as removable
  }
}
