using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Debugging;

namespace Vehicles
{
	public static class StartupTesting
	{
		[UnitTest(Active = true)]
		private static void UnitTestStrafing()
		{
			Map sourceMap = null;
			Map targetMap = null;
			VehiclePawn vehicle = null;
			foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer))
			{
				Map map = GetOrGenerateMapUtility.GetOrGenerateMap(settlement.Tile, null);
				if (map.spawnedThings.FirstOrDefault(t => t is VehiclePawn vehicleCheck && vehicleCheck.CompVehicleLauncher != null) is VehiclePawn vehicleTarget)
				{
					sourceMap = map;
					vehicle = vehicleTarget;
				}
				else
				{
					targetMap = map;
				}
			}
			if (sourceMap is null || targetMap is null)
			{
				SmashLog.Error($"Unable to execute unit test <method>UnitTestStrafing</method> post load.");
				return;
			}
			Current.Game.CurrentMap = targetMap;
			vehicle.CompVehicleLauncher.SelectedLaunchProtocol = vehicle.CompVehicleLauncher.launchProtocols.FirstOrDefault();
			StrafeTargeter.Instance.BeginTargeting(vehicle, vehicle.CompVehicleLauncher.SelectedLaunchProtocol, delegate (IntVec3 start, IntVec3 end)
			{
				if (vehicle.Spawned)
				{
					Current.Game.CurrentMap = vehicle.Map;
					vehicle.CompVehicleLauncher.TryLaunch(targetMap.Tile, new AerialVehicleArrivalAction_StrafeMap(vehicle, targetMap.Parent, start, end));
				}
				else
				{
					CameraJumper.TryShowWorld();
					AerialVehicleInFlight aerial = Find.World.GetCachedWorldComponent<VehicleWorldObjectsHolder>().AerialVehicleObject(vehicle);
					vehicle.inFlight = true;
					aerial.OrderFlyToTiles(new List<int>() { targetMap.Tile }, aerial.DrawPos, new AerialVehicleArrivalAction_StrafeMap(vehicle, targetMap.Parent, start, end));
				}
			}, null, null, null, false);
		}
	}
}
