using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class AerialVehicleArrivalModeWorker_EdgeDrop : AerialVehicleArrivalModeWorker
	{
		public override void VehicleArrived(VehiclePawn vehicle, LaunchProtocol launchProtocol, Map map)
		{
			Rot4 vehicleRotation = launchProtocol.landingProperties.forcedRotation ?? Rot4.Random;
			IntVec2 vehicleSize = vehicle.VehicleDef.Size;
			IntVec3 cell = CellFinderExtended.RandomEdgeCell(vehicleRotation.Opposite, map, (IntVec3 cell) => 
				!MapHelper.VehicleBlockedInPosition(vehicle, Current.Game.CurrentMap, cell, vehicleRotation), vehicleSize.x > vehicleSize.z ? vehicleSize.x : vehicleSize.z);
			VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
			skyfaller.vehicle = vehicle;
			GenSpawn.Spawn(skyfaller, cell, map, vehicleRotation);
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
