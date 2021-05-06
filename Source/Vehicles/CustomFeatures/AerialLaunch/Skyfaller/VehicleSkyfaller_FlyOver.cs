using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleSkyfaller_FlyOver : VehicleSkyfaller
	{
		public const float DefaultAngle = -65;
		private const int LeaveMapAfterTicks = 220;

		public int ticksToImpact = LeaveMapAfterTicks;

		public override Vector3 DrawPos => SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(base.DrawPos, ticksToImpact, angle, CurrentSpeed);

		protected virtual float CurrentSpeed => vehicle.CompVehicleLauncher.FlySpeed;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksToImpact, "ticksToImpact");
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
			DrawDropSpotShadow();
		}

		public override void Tick()
		{
			ticksToImpact--;
			if (ticksToImpact <= 0)
			{

			}
		}

		private void ExitMap()
		{
			Destroy();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				launchProtocol.SetPositionArriving(new Vector3(DrawPos.x, DrawPos.y + 1, DrawPos.z), Rotation, map);
				launchProtocol.OrderProtocol(true);
				ticksToImpact = Mathf.CeilToInt(Ext_Map.Distance(new IntVec3(map.Size.x / 2, map.Size.y, map.Size.z / 2), Position) * 2 / vehicle.CompVehicleLauncher.FlySpeed);
			}
		}
	}
}
