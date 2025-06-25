using JetBrains.Annotations;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public abstract class AerialVehicleArrivalAction_LandInMap : AerialVehicleArrivalAction
{
  protected int tile;
  protected MapParent mapParent;

  protected AerialVehicleArrivalAction_LandInMap()
  {
  }

  protected AerialVehicleArrivalAction_LandInMap(VehiclePawn vehicle, MapParent mapParent, int tile)
    : base(vehicle)
  {
    this.tile = tile;
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
    Scribe_Values.Look(ref tile, nameof(tile));
    Scribe_References.Look(ref mapParent, nameof(mapParent));
  }
}