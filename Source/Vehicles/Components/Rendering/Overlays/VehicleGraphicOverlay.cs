using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleGraphicOverlay
	{
		public List<GraphicOverlay> graphics = new List<GraphicOverlay>();

		public VehiclePawn vehicle;

		public ExtraRotationRegistry rotationRegistry = new ExtraRotationRegistry();

		public VehicleGraphicOverlay(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			graphics = new List<GraphicOverlay>(vehicle.VehicleDef.drawProperties.OverlayGraphics);
		}

		public virtual void RenderGraphicOverlays(Vector3 drawPos, Rot4 rot)
		{
			float extraAngle;
			foreach (GraphicOverlay graphicOverlay in graphics)
			{
				extraAngle = graphicOverlay.rotation;
				if (graphicOverlay.graphic is Graphic_Rotator rotator)
				{
					extraAngle += rotationRegistry[rotator.RegistryKey].ClampAndWrap(0, 359);
				}
				graphicOverlay.graphic.DrawWorker(drawPos, rot, null, null, vehicle.Angle + extraAngle);
			}
		}
	}
}
