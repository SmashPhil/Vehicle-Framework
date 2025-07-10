using JetBrains.Annotations;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public abstract class VehicleArrivalAction : IArrivalAction
{
  protected VehiclePawn vehicle;

  /// <summary>
  /// XML Save/Load initialization
  /// </summary>
  protected VehicleArrivalAction()
  {
  }

  /// <summary>
  /// Use for programmatic instantiation
  /// </summary>
  /// <param name="vehicle"></param>
  protected VehicleArrivalAction(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  public virtual bool DestroyOnArrival => false;

  public AerialVehicleInFlight AerialVehicle => vehicle.GetAerialVehicle();

  public virtual void Arrived(GlobalTargetInfo target)
  {
    Assert.IsNotNull(AerialVehicle);
    if (DestroyOnArrival)
      AerialVehicle.ClearAndDestroy();
  }

  public virtual void ExposeData()
  {
    Scribe_References.Look(ref vehicle, nameof(vehicle), true);
  }
}