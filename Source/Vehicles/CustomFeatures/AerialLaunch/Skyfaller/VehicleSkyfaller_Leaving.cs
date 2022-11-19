using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class VehicleSkyfaller_Leaving : VehicleSkyfaller
	{
		public AerialVehicleArrivalAction arrivalAction;

		public List<FlightNode> flightPath;

		public bool orderRecon;

		public bool createWorldObject = true;

		private int delayLaunchingTicks;

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			vehicle.CompVehicleLauncher.launchProtocol.Draw(drawLoc, 0);
			DrawDropSpotShadow();
		}

		public override void Tick()
		{
			delayLaunchingTicks--;
			if (delayLaunchingTicks <= 0)
			{
				base.Tick();
				if (vehicle.CompVehicleLauncher.launchProtocol.FinishedAnimation(this))
				{
					LeaveMap();
				}
			}
		}

		protected override void LeaveMap()
		{
			vehicle.CompVehicleLauncher.launchProtocol.Release();
			if (!createWorldObject)
			{
				base.LeaveMap();
				return;
			}
			if (flightPath.Any(node => node.tile < 0))
			{
				Log.Error("AerialVehicle left the map but has a flight path Tile that is invalid. Removing node from path.");
				flightPath.RemoveAll(node => node.tile < 0);
				if (flightPath.NullOrEmpty())
				{
					//REDO - Handle better here
					return;
				}
			}
			Messages.Message($"{vehicle.LabelShort} LEFT", MessageTypeDefOf.PositiveEvent);
			if (createWorldObject)
			{
				AerialVehicleInFlight flyingVehicle = (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.AerialVehicle);
				flyingVehicle.vehicle = vehicle;
				flyingVehicle.Tile = Map.Tile;
				flyingVehicle.SetFaction(vehicle.Faction);
				flyingVehicle.OrderFlyToTiles(new List<FlightNode>(flightPath), WorldHelper.GetTilePos(Map.Tile), arrivalAction);
				if (orderRecon)
				{
					flyingVehicle.flightPath.ReconCircleAt(flightPath.LastOrDefault().tile);
				}
				flyingVehicle.Initialize();
				Find.WorldObjects.Add(flyingVehicle);
			}
			Destroy(DestroyMode.Vanish);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vehicle.CompVehicleLauncher.launchProtocol.Prepare(map, Position, Rotation);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
				delayLaunchingTicks = vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.delayByTicks;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref arrivalAction, "arrivalAction", Array.Empty<object>());
			Scribe_Collections.Look(ref flightPath, "flightPath");
			Scribe_Values.Look(ref orderRecon, "orderRecon");
			Scribe_Values.Look(ref createWorldObject, "createWorldObject", true, false);
			Scribe_Values.Look(ref delayLaunchingTicks, "delayLaunchingTicks");
		}
	}
}
