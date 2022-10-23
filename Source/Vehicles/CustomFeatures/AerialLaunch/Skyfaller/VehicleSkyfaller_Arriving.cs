using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	//REDO - CACHE LAUNCH PROTOCOL
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
			skyfallerLoc = vehicle.CompVehicleLauncher.launchProtocol.AnimateLanding(drawLoc.y, flip);
			vehicle.CompVehicleLauncher.launchProtocol.DrawAdditionalLandingTextures(drawLoc.y);
			DrawDropSpotShadow();
		}

		public override void Tick()
		{
			base.Tick();
			if (vehicle.CompVehicleLauncher.launchProtocol.FinishedLanding(this))
			{
				delayLandingTicks--;
				if (delayLandingTicks <= 0 && Position.InBounds(Map))
				{
					Position = skyfallerLoc.ToIntVec3();
					FinalizeLanding();
				}
			}
			if (Find.TickManager.TicksGame % NotificationSquishInterval == 0 && Map != null && vehicle.VehicleDef.HasComp(typeof(CompProperties_VehicleDamager)))
			{
				foreach (IntVec3 cell in vehicle.PawnOccupiedCells(Position, Rotation))
				{
					GenVehicleDamager.NotifyNearbyPawnsOfDangerousPosition(Map, cell);
				}
			}
		}

		protected virtual void FinalizeLanding()
		{
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
				vehicle.CompVehicleLauncher.launchProtocol.SetPositionArriving(new Vector3(DrawPos.x, DrawPos.y + 1, DrawPos.z), Rotation, map);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(true);
				delayLandingTicks = vehicle.CompVehicleLauncher.launchProtocol.landingProperties?.delayByTicks ?? 0;
			}
		}
	}
}
