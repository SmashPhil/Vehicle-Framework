using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public abstract class AerialVehicleArrivalAction : IExposable
{
  protected VehiclePawn vehicle;

  /// <summary>
  /// XML Save/Load initialization
  /// </summary>
  protected AerialVehicleArrivalAction()
  {
  }

  /// <summary>
  /// Use for programmatic instantiation
  /// </summary>
  /// <param name="vehicle"></param>
  protected AerialVehicleArrivalAction(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  public virtual bool DestroyOnArrival => false;

  public virtual FloatMenuAcceptanceReport StillValid(int destinationTile)
  {
    return true;
  }

  public virtual bool ShouldUseLongEvent(int tile)
  {
    return false;
  }

  public virtual void Arrived(AerialVehicleInFlight aerialVehicle, int tile)
  {
    if (DestroyOnArrival)
    {
      aerialVehicle.Destroy();
    }
  }

  public virtual void ExposeData()
  {
    Scribe_References.Look(ref vehicle, nameof(vehicle), true);
  }
}