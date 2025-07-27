using RimWorld;
using Verse;

namespace Vehicles;

public class AerialVehicleArrivalModeWorker_TargetedDrop : AerialVehicleArrivalModeWorker
{
  public override void VehicleArrived(VehiclePawn vehicle, LaunchProtocol launchProtocol, Map map)
  {
    CameraJumper.TryJump(map.Center, map);
    LandingTargeter.Instance.BeginTargeting(vehicle, map, delegate(LocalTargetInfo target, Rot4 rot)
    {
      VehicleSkyfaller_Arriving skyfaller =
        (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
      skyfaller.vehicle = vehicle;
      GenSpawn.Spawn(skyfaller, target.Cell, map, rot);
    }, allowRotating: vehicle.VehicleDef.rotatable, forcedTargeting: true);

    // Needs updating if spawning full colonist list into generated map. LandingTargeter will flag game ender to false
    // for the duration of targeting
    Find.GameEnder.CheckOrUpdateGameOver();
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
      bool mechFaction = parms.faction == Faction.OfMechanoids;
      bool hostile = parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer);
      if (Rand.Chance(0.4f) && !mechFaction &&
        map.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
      {
        parms.spawnCenter = DropCellFinder.TradeDropSpot(map);
      }
      else if (!DropCellFinder.TryFindRaidDropCenterClose(out parms.spawnCenter, map,
        canRoofPunch: !mechFaction && hostile, allowIndoors: !mechFaction))
      {
        parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
        return parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms);
      }
    }
    return true;
  }
}