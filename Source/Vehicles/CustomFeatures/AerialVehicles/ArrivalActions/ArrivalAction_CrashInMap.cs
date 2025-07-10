using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public class ArrivalAction_CrashInMap : ArrivalAction_LandToCell
{
  public ArrivalAction_CrashInMap()
  {
  }

  public ArrivalAction_CrashInMap(VehiclePawn vehicle, MapParent mapParent, IntVec3 landingCell, Rot4 landingRot) :
    base(vehicle, mapParent, landingCell, landingRot)
  {
  }

  protected override void SpawnSkyfaller()
  {
    VehicleSkyfaller_Crashing skyfaller =
      (VehicleSkyfaller_Crashing)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props
       .skyfallerCrashing);
    skyfaller.vehicle = vehicle;
    skyfaller.rotCrashing = Rot4.East;
    GenSpawn.Spawn(skyfaller, landingCell, mapParent.Map, landingRot);
  }

  protected override void ExecuteEvents()
  {
    vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleCrashLanding].ExecuteEvents();
  }
}