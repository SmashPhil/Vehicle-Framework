using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_Arriving : VehicleSkyfaller
	{
		public const int NotificationSquishInterval = 60;
		public int delayLandingTicks;
		
		private static readonly Color GhostColor = new Color(1, 1, 1, 0.5f);
		
		public bool LandingSpotOccupied { get; private set; }

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref delayLandingTicks, "delayLandingTicks");
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			(launchProtocolDrawPos, _) = vehicle.CompVehicleLauncher.launchProtocol.Draw(RootPos, 0);
			DrawLandingGhost();
		}

		public override void Tick()
		{
			base.Tick();
			if (vehicle.CompVehicleLauncher.launchProtocol.FinishedAnimation(this))
			{
				delayLandingTicks--;
				if (delayLandingTicks <= 0 && Position.InBounds(Map))
				{
					FinalizeLanding();
					return;
				}
			}
			if (Find.TickManager.TicksGame % NotificationSquishInterval == 0 && Map != null)
			{
				LandingSpotOccupied = false;
				foreach (Thing thing in Map.thingGrid.ThingsAt(Position))
				{
					LandingSpotOccupied |= thing is VehiclePawn;
				}
				foreach (IntVec3 cell in vehicle.PawnOccupiedCells(Position, Rotation))
				{
					VehicleDamager.NotifyNearbyPawnsOfDangerousPosition(Map, cell);
				}
			}
		}

		protected virtual void FinalizeLanding()
		{
			vehicle.CompVehicleLauncher.launchProtocol.Release();
			vehicle.CompVehicleLauncher.inFlight = false;
			if (VehicleReservationManager.AnyVehicleInhabitingCells(vehicle.PawnOccupiedCells(Position, Rotation), Map))
			{
				GenExplosion.DoExplosion(Position, Map, Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z), DamageDefOf.Bomb, vehicle);
			}
			else
			{
				GenSpawn.Spawn(vehicle, Position, Map, Rotation);
				vehicle.TryDamageObstructions();
				if (VehicleMod.settings.main.deployOnLanding)
				{
					vehicle.CompVehicleLauncher.SetTimedDeployment();
				}
			}
			Destroy();
		}

		private void DrawLandingGhost()
		{
			if (VehicleMod.settings.main.drawLandingGhost)
			{
				GhostDrawer.DrawGhostThing(Position, Rotation, vehicle.VehicleDef, vehicle.VehicleGraphic, LandingSpotOccupied ? LandingTargeter.GhostOccupiedColor : GhostColor, AltitudeLayer.Blueprint, vehicle);
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vehicle.CompVehicleLauncher.launchProtocol.Prepare(map, Position, Rotation);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Landing);
				delayLandingTicks = vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.delayByTicks;
			}
		}
	}
}
