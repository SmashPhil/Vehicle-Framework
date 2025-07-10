using JetBrains.Annotations;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public abstract class ArrivalAction_LandInMap : VehicleArrivalAction
{
  protected MapParent mapParent;

  protected ArrivalAction_LandInMap()
  {
  }

  protected ArrivalAction_LandInMap(VehiclePawn vehicle, MapParent mapParent)
    : base(vehicle)
  {
    this.mapParent = mapParent;
  }

  public override bool DestroyOnArrival => true;

  protected virtual void ExecuteEvents()
  {
    vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_References.Look(ref mapParent, nameof(mapParent));
  }
}