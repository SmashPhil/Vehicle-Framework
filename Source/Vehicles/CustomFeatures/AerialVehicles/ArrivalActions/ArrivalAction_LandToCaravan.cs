using JetBrains.Annotations;
using RimWorld.Planet;

namespace Vehicles.World;

public class ArrivalAction_LandToCaravan : VehicleArrivalAction
{
  [UsedWithReflection]
  public ArrivalAction_LandToCaravan()
  {
  }

  public ArrivalAction_LandToCaravan(VehiclePawn vehicle) : base(vehicle)
  {
  }

  public override void Arrived(GlobalTargetInfo _)
  {
    // SwitchToCaravan handles destroying aerial vehicle object
    AerialVehicle.SwitchToCaravan();
  }
}