using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class VehicleTrack_Wake : VehicleTrack
	{
		private const float DefaultSizeSplash = 10;
		private const float DefaultSizePassiveSplash = 2;
		private const float WakeDistanceInterval = 0.39942405f;

		public float size = 1;
		public float speed = 1.6f;

		public override void TryPlaceTrack(VehiclePawn vehicle, ref Vector3 lastTrackPlacePos)
		{
			if ((vehicle.Drawer.DrawPos - lastTrackPlacePos).MagnitudeHorizontalSquared() > WakeDistanceInterval)
			{
				Vector3 drawPos = vehicle.Drawer.DrawPos;
				if (drawPos.ToIntVec3().InBounds(vehicle.Map) && !vehicle.beached)
				{
					FleckMaker.WaterSplash(drawPos, vehicle.Map, DefaultSizeSplash * size, speed);
					lastTrackPlacePos = drawPos;
				}
			}
			else if (VehicleMod.settings.main.passiveWaterWaves && Find.TickManager.TicksGame % 360 == 0)
			{
				float offset = Mathf.PingPong(Find.TickManager.TicksGame / 10, vehicle.VehicleDef.graphicData.drawSize.y / 4);
				FleckMaker.WaterSplash(vehicle.Drawer.DrawPos - new Vector3(0, 0, offset), vehicle.Map, DefaultSizePassiveSplash * size, speed);
			}
		}
	}
}
