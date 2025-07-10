using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class ArrivalAction_LandToCaravan : VehicleArrivalAction
{
  /// <summary>
  /// Required for Xml deserialization
  /// </summary>
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