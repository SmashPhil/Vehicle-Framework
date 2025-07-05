using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class AerialVehicleArrivalAction_FormVehicleCaravan : AerialVehicleArrivalAction
{
  public AerialVehicleArrivalAction_FormVehicleCaravan()
  {
  }

  public AerialVehicleArrivalAction_FormVehicleCaravan(VehiclePawn vehicle) : base(vehicle)
  {
  }

  public override FloatMenuAcceptanceReport StillValid(PlanetTile destinationTile)
  {
    return !Find.World.Impassable(destinationTile);
  }

  public override void Arrived(AerialVehicleInFlight aerialVehicle, PlanetTile tile)
  {
    // SwitchToCaravan handles destroying aerial vehicle object
    aerialVehicle.SwitchToCaravan();
  }

  public static bool CanFormCaravanAt(VehiclePawn vehicle, PlanetTile tile)
  {
    return WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef);
  }
}