using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_Arriving : VehicleSkyfaller
	{
		public const int NotificationSquishInterval = 50;
		public int delayLandingTicks;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref delayLandingTicks, "delayLandingTicks");
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			(launchProtocolDrawPos, _) = vehicle.CompVehicleLauncher.launchProtocol.Draw(RootPos, 0);
			//DrawDropSpotShadow();
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
				}
			}
			if (Find.TickManager.TicksGame % NotificationSquishInterval == 0 && Map != null)
			{
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
				if (VehicleMod.settings.main.deployOnLanding)
				{
					vehicle.CompVehicleLauncher.SetTimedDeployment();
				}
			}
			Destroy();
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
