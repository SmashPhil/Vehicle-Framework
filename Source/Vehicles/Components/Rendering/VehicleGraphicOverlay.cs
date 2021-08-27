using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleGraphicOverlay
	{
		public List<Graphic> graphics = new List<Graphic>();

		public VehiclePawn vehicle;

		public ExtraRotationRegistry rotationRegistry = new ExtraRotationRegistry();

		public VehicleGraphicOverlay(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			graphics.AddRange(vehicle.VehicleDef.drawProperties.OverlayGraphics);
		}

		public virtual void RenderGraphicOverlays(Vector3 drawPos, Rot4 rot)
		{
			float extraAngle;
			foreach (Graphic graphic in graphics)
			{
				extraAngle = 0;
				if (graphic is Graphic_Rotator rotator)
				{
					extraAngle = rotationRegistry[rotator.RegistryKey].ClampAndWrap(0, 359);
				}
				graphic.DrawWorker(drawPos, rot, null, null, vehicle.Angle + extraAngle);
			}
		}
	}
}
