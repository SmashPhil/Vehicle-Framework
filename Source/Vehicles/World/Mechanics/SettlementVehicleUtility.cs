using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class SettlementVehicleUtility
	{
		public static void Settle(AerialVehicleInFlight aerialVehicle)
		{
			Faction faction = aerialVehicle.Faction;
			if (faction != Faction.OfPlayer)
			{
				Log.Error("Cannot settle with non-player faction.");
				return;
			}
			Settlement newHome = SettleUtility.AddNewHome(aerialVehicle.Tile, faction);
			LongEventHandler.QueueLongEvent(delegate ()
			{
				GetOrGenerateMapUtility.GetOrGenerateMap(aerialVehicle.Tile, Find.World.info.initialMapSize, null);
			}, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), true);
			LongEventHandler.QueueLongEvent(delegate ()
			{
				IntVec3 landingCell = GetLandingCell(newHome.Map, aerialVehicle);
				//AerialVehicleArrivalAction_LandSpecificCell arrivalAction = new AerialVehicleArrivalAction_LandSpecificCell(aerialVehicle.vehicle, newHome, aerialVehicle.Tile,
				//	aerialVehicle.vehicle.CompVehicleLauncher.launchProtocols.FirstOrDefault(), landingCell, Rot4.North);
				//arrivalAction.Arrived(aerialVehicle.Tile);
				VehiclePawn vehicle = (VehiclePawn)GenSpawn.Spawn(aerialVehicle.vehicle, landingCell, newHome.Map);
				CameraJumper.TryJump(vehicle);
				aerialVehicle.Destroy();
			}, "SpawningColonists", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap), true);
		}

		private static IntVec3 GetLandingCell(Map map, AerialVehicleInFlight aerialVehicle)
		{
			bool validator(IntVec3 c)
			{
				bool flag = aerialVehicle.vehicle.PawnOccupiedCells(c, Rot4.East).All(c2 => c2.Standable(map) && !c.Roofed(map) && !c.Fogged(map) && c.InBounds(map));
				return flag;
			}
			IntVec3 RandomCentercell()
			{
				RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(validator, map, out IntVec3 result);
				return result;
			}
			IntVec3 cell = RandomCentercell();
			return cell;
		}
	}
}
