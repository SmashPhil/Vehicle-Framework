using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	//REDO - CACHE LAUNCH PROTOCOl
	public abstract class VehicleSkyfaller_FlyOver : VehicleSkyfaller
	{
		public IntVec3 start;
		public IntVec3 end;

		public AerialVehicleInFlight aerialVehicle;

		public VehicleSkyfaller_FlyOver()
		{

		}

		public VehicleSkyfaller_FlyOver(AerialVehicleInFlight aerialVehicle)
		{
			this.aerialVehicle = aerialVehicle;
		}

		public override Vector3 DrawPos
		{
			get
			{
				switch (def.skyfaller.movementType)
				{
					case SkyfallerMovementType.Accelerate:
						return SkyfallerHelper.DrawPos_Accelerate(base.DrawPos, vehicle.CompVehicleLauncher.launchProtocol.TicksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.ConstantSpeed:
						return SkyfallerHelper.DrawPos_ConstantSpeed(base.DrawPos, vehicle.CompVehicleLauncher.launchProtocol.TicksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.Decelerate:
						return SkyfallerHelper.DrawPos_Decelerate(base.DrawPos, vehicle.CompVehicleLauncher.launchProtocol.TicksPassed, angle, CurrentSpeed);
					default:
						Log.ErrorOnce("SkyfallerMovementType not handled: " + def.skyfaller.movementType, thingIDNumber);
						return SkyfallerHelper.DrawPos_Accelerate(base.DrawPos, vehicle.CompVehicleLauncher.launchProtocol.TicksPassed, angle, CurrentSpeed);
				}
			}
		}

		protected virtual float CurrentSpeed
		{
			get
			{
				if (def.skyfaller.speedCurve is null)
				{
					return def.skyfaller.speed;
				}
				return def.skyfaller.speedCurve.Evaluate(TimeInAnimation) * def.skyfaller.speed;
			}
		}

		protected virtual float TimeInAnimation
		{
			get
			{
				if (def.skyfaller.reversed)
				{
					return (float)vehicle.CompVehicleLauncher.launchProtocol.TicksPassed / def.skyfaller.ticksToImpactRange.max;
				}
				return 1f - (float)vehicle.CompVehicleLauncher.launchProtocol.TicksPassed / def.skyfaller.ticksToImpactRange.max;
			}
		}

		public Vector3 DistanceAtMin
		{
			get
			{
				return SkyfallerHelper.DrawPos_ConstantSpeed(base.DrawPos, -def.skyfaller.ticksToImpactRange.min, angle, CurrentSpeed);
			}
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			float num = 0f;
			if (def.skyfaller.rotateGraphicTowardsDirection)
			{
				num = angle;
			}
			if (def.skyfaller.angleCurve != null)
			{
				angle = def.skyfaller.angleCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.rotationCurve != null)
			{
				num += def.skyfaller.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.xPositionCurve != null)
			{
				drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (def.skyfaller.zPositionCurve != null)
			{
				drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(TimeInAnimation);
			}
			vehicle.DrawAt(drawLoc, num + Rotation.AsInt * 90, flip);
			//DrawDropSpotShadow(); //Add tracing shadow;
		}

		public override void Tick()
		{
			vehicle.CompVehicleLauncher.launchProtocol.Tick();
			if (!DrawPos.InBounds(Map) && vehicle.CompVehicleLauncher.launchProtocol.TicksPassed > def.skyfaller.ticksToImpactRange.max)
			{
				ExitMap();
			}
		}

		protected virtual void ExitMap()
		{
			AerialVehicleInFlight flyingVehicle = (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.AerialVehicle);
			flyingVehicle.vehicle = vehicle;
			flyingVehicle.Tile = Map.Tile;
			flyingVehicle.SetFaction(vehicle.Faction);
			flyingVehicle.OrderFlyToTiles(aerialVehicle.flightPath.Path, WorldHelper.GetTilePos(Map.Tile), aerialVehicle.arrivalAction);
			//Recon edge case?
			flyingVehicle.Initialize();
			Find.WorldObjects.Add(flyingVehicle);
			Destroy();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref start, "start");
			Scribe_Values.Look(ref end, "end");
			Scribe_References.Look(ref aerialVehicle, "aerialVehicle");
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vehicle.CompVehicleLauncher.launchProtocol.Prepare(Map, Position, Rot4.North);
				vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
				vehicle.CompVehicleLauncher.launchProtocol.SetTickCount(-def.skyfaller.ticksToImpactRange.max);
			}
		}
	}
}
