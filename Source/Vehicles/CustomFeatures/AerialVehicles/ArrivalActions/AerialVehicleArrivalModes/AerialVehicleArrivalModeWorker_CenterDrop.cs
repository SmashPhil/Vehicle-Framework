using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[UsedImplicitly]
public class AerialVehicleArrivalModeWorker_CenterDrop : AerialVehicleArrivalModeWorker
{
  public override void VehicleArrived(VehiclePawn vehicle, LaunchProtocol launchProtocol, Map map)
  {
    Rot4 vehicleRotation = launchProtocol.LandingProperties?.forcedRotation ?? Rot4.Random;
    bool found = CellFinderExtended.TryFindRandomCenterCell(map,
      cell => !MapHelper.ImpassableOrVehicleBlocked(vehicle, Current.Game.CurrentMap, cell, vehicleRotation),
      out IntVec3 result);
    if (!found)
    {
      AerialVehicleArrivalModeDefOf.TargetedLanding.Worker.VehicleArrived(vehicle, launchProtocol, map);
      return;
    }

    VehicleSkyfaller_Arriving skyfaller =
      (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(
        vehicle.CompVehicleLauncher.Props.skyfallerIncoming, vehicle);
    GenSpawn.Spawn(skyfaller, result, map, vehicleRotation);
  }

  public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
  {
    Map map = (Map)parms.target;
    if (!parms.raidArrivalModeForQuickMilitaryAid)
    {
      parms.podOpenDelay = 520;
    }
    parms.spawnRotation = Rot4.Random;
    if (!parms.spawnCenter.IsValid)
    {
      bool flag = parms.faction == Faction.OfMechanoids;
      bool flag2 = parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer);
      if (Rand.Chance(0.4f) && !flag &&
        map.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
      {
        parms.spawnCenter = DropCellFinder.TradeDropSpot(map);
      }
      else if (!DropCellFinder.TryFindRaidDropCenterClose(out parms.spawnCenter, map, !flag && flag2, !flag, true, -1))
      {
        parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
        return parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
      }
    }
    return true;
  }
}