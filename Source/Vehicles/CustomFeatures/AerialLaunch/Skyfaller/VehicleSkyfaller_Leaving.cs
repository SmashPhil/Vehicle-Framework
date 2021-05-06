using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Vehicles.Defs;
using UnityEngine;

namespace Vehicles
{
	public class VehicleSkyfaller_Leaving : VehicleSkyfaller
	{
		public AerialVehicleArrivalAction arrivalAction;

		public List<int> flightPath;

		public bool orderRecon;

		public bool createWorldObject = true;

		private int delayLaunchingTicks;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref arrivalAction, "arrivalAction", Array.Empty<object>());
			Scribe_Collections.Look(ref flightPath, "flightPath");
			Scribe_Values.Look(ref orderRecon, "orderRecon");
			Scribe_Values.Look(ref createWorldObject, "createWorldObject", true, false);
			Scribe_Values.Look(ref delayLaunchingTicks, "delayLaunchingTicks");
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			skyfallerLoc = launchProtocol.AnimateTakeoff(drawLoc.y, flip);
			launchProtocol.DrawAdditionalLaunchTextures(drawLoc.y);
			DrawDropSpotShadow();
		}

		public override void Tick()
		{
			delayLaunchingTicks--;
			if (delayLaunchingTicks <= 0)
			{
				base.Tick();
				if (launchProtocol.FinishedTakeoff(this))
				{
					LeaveMap();
				}
			}
		}

		protected override void LeaveMap()
		{
			if (!createWorldObject)
			{
				base.LeaveMap();
				return;
			}
			if (flightPath.Any(p => p < 0))
			{
				Log.Error("AerialVehicle left the map but has a flight path Tile that is invalid. Removing node from path.");
				flightPath.RemoveAll(p => p < 0);
				if (flightPath.NullOrEmpty())
				{
					//REDO - Handle better here
					return;
				}
			}
			Messages.Message($"{vehicle.LabelShort} LEFT", MessageTypeDefOf.PositiveEvent);
			if (flightPath.LastOrDefault() == Map.Tile)
			{
				arrivalAction?.Arrived(flightPath.LastOrDefault());
			}
			else if (createWorldObject)
			{
				AerialVehicleInFlight flyingVehicle = (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.AerialVehicle);
				flyingVehicle.vehicle = vehicle;
				flyingVehicle.Tile = Map.Tile;
				flyingVehicle.SetFaction(vehicle.Faction);
				flyingVehicle.OrderFlyToTiles(new List<int>(flightPath), Find.WorldGrid.GetTileCenter(Map.Tile), arrivalAction);
				if (orderRecon)
				{
					flyingVehicle.flightPath.ReconCircleAt(flightPath.LastOrDefault());
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
				launchProtocol.SetPositionLeaving(new Vector3(DrawPos.x, DrawPos.y + 1, DrawPos.z), Rotation, map);
				launchProtocol.OrderProtocol(false);
				delayLaunchingTicks = launchProtocol.launchProperties.delayByTicks;
			}
		}
	}
}
