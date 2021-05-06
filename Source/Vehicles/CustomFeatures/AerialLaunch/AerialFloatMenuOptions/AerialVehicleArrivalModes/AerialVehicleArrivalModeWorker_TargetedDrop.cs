using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class AerialVehicleArrivalModeWorker_TargetedDrop : AerialVehicleArrivalModeWorker
	{
		public override void VehicleArrived(AerialVehicleInFlight aerialVehicle, LaunchProtocol launchProtocol, Map map)
		{
			LaunchProtocol protocol = aerialVehicle.vehicle.CompVehicleLauncher.launchProtocols.FirstOrDefault();
			CameraJumper.TryJump(map.Center, map);
			Targeters.LandingTargeter.BeginTargeting(aerialVehicle.vehicle, aerialVehicle.vehicle.CompVehicleLauncher.launchProtocols.FirstOrDefault(), map, delegate (LocalTargetInfo target, Rot4 rot)
			{
				VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(aerialVehicle.vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
				skyfaller.vehicle = aerialVehicle.vehicle;
				skyfaller.launchProtocol = launchProtocol;
				GenSpawn.Spawn(skyfaller, target.Cell, map, rot);
			}, null, null, null, aerialVehicle.vehicle.VehicleDef.rotatable && protocol.landingProperties.forcedRotation is null, true);
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
				if (Rand.Chance(0.4f) && !flag && map.listerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
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
}
