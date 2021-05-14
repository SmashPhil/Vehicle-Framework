using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_FlyOver : VehicleSkyfaller
	{
		public override Vector3 DrawPos
		{
			get
			{
				switch (def.skyfaller.movementType)
				{
					case SkyfallerMovementType.Accelerate:
						return SkyfallerHelper.DrawPos_Accelerate(base.DrawPos, launchProtocol.TicksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.ConstantSpeed:
						return SkyfallerHelper.DrawPos_ConstantSpeed(base.DrawPos, launchProtocol.TicksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.Decelerate:
						return SkyfallerHelper.DrawPos_Decelerate(base.DrawPos, launchProtocol.TicksPassed, angle, CurrentSpeed);
					default:
						Log.ErrorOnce("SkyfallerMovementType not handled: " + def.skyfaller.movementType, thingIDNumber);
						return SkyfallerHelper.DrawPos_Accelerate(base.DrawPos, launchProtocol.TicksPassed, angle, CurrentSpeed);
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
				return def.skyfaller.speedCurve.Evaluate(launchProtocol.TimeInAnimation) * def.skyfaller.speed;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
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
				angle = def.skyfaller.angleCurve.Evaluate(launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.rotationCurve != null)
			{
				num += def.skyfaller.rotationCurve.Evaluate(launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.xPositionCurve != null)
			{
				drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(launchProtocol.TimeInAnimation);
			}
			if (def.skyfaller.zPositionCurve != null)
			{
				drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(launchProtocol.TimeInAnimation);
			}
			vehicle.DrawAt(drawLoc, num + Rotation.AsInt * 90, flip);
			//DrawDropSpotShadow(); //Add tracing shadow;
		}

		public override void Tick()
		{
			launchProtocol.Tick();
			if (!DrawPos.InBounds(Map) && launchProtocol.TicksPassed > def.skyfaller.ticksToImpactRange.max)
			{
				ExitMap();
			}
		}

		protected virtual void FireTurrets()
		{

		}

		protected virtual void ExitMap()
		{
			Log.Message("Exit");
			Destroy();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			Log.Message("DESPAWNED");
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
			Log.Message("DESTROYED");
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				launchProtocol = vehicle.CompVehicleLauncher.SelectedLaunchProtocol ?? vehicle.CompVehicleLauncher.launchProtocols.FirstOrDefault();
				launchProtocol.SetPositionArriving(DrawPos, Rot8.North, Map);
				launchProtocol.OrderProtocol(false);
				launchProtocol.SetTickCount(-def.skyfaller.ticksToImpactRange.max);
			}
		}
	}
}
