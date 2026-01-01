using CoreLib;
using JetBrains.Annotations;
using RimWorld.Planet;
using SmashTools;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public class ArrivalAction_LandToCell : ArrivalAction_LandInMap
{
  protected IntVec3 landingCell;
  protected Rot4 landingRot;

  public ArrivalAction_LandToCell()
  {
  }

  public ArrivalAction_LandToCell(VehiclePawn vehicle, MapParent mapParent, IntVec3 landingCell, Rot4 landingRot)
    : base(vehicle, mapParent)
  {
    this.mapParent = mapParent;
    this.landingCell = landingCell;
    this.landingRot = landingRot;
  }

  public virtual bool CanArriveInMap => mapParent?.Map != null;

  public override void Arrived(GlobalTargetInfo target)
  {
    if (!mapParent.HasMap)
    {
      Trace.Fail(
        $"Unable to land {vehicle} at destination. Map no longer exists, spawning as caravan nearby...");
      AerialVehicle.SwitchToCaravan();
      return;
    }
    base.Arrived(target);
    SpawnSkyfaller();
    ExecuteEvents();
  }

  protected virtual void SpawnSkyfaller()
  {
    VehicleSkyfaller_Arriving skyfaller =
      (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(
        vehicle.CompVehicleLauncher.Props.skyfallerIncoming, vehicle);
    skyfaller.rotatePostLanding = landingRot;
    Rot4 vehicleRotation =
      vehicle.CompVehicleLauncher.launchProtocol.LandingProperties?.forcedRotation ?? landingRot;
    GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, vehicleRotation);
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref landingCell, nameof(landingCell));
    Scribe_Values.Look(ref landingRot, nameof(landingRot));
  }
}