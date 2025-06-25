using Vehicles.World;

namespace Vehicles;

public abstract class BaseVehicleWorldTargeter : BaseWorldTargeter
{
  protected VehiclePawn vehicle;
  protected AerialVehicleInFlight aerialVehicle;

  public abstract void RegisterActionOnTile(int tile, AerialVehicleArrivalAction arrivalAction);
}