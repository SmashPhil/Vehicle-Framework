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

		public virtual void RenderGraphicOverlays(Vector3 drawPos, float angle, Rot8 rot)
		{
			float extraAngle;
			foreach (GraphicOverlay graphicOverlay in graphics)
			{
				extraAngle = graphicOverlay.rotation;
				if (graphicOverlay.graphic is Graphic_Rotator rotator)
				{
					extraAngle += rotationRegistry[rotator.RegistryKey].ClampAndWrap(0, 359);
				}
				if (angle != 0)
				{
					Rot8 graphicRot = rot;
					if (rot == Rot8.NorthEast && vehicle.VehicleGraphic.EastDiagonalRotated)
					{
						graphicRot = Rot8.North;
					}
					else if (rot == Rot8.SouthEast && vehicle.VehicleGraphic.EastDiagonalRotated)
					{
						graphicRot = Rot8.South;
					}
					else if (rot == Rot8.SouthWest && vehicle.VehicleGraphic.WestDiagonalRotated)
					{
						graphicRot = Rot8.South;
					}
					else if (rot == Rot8.NorthWest && vehicle.VehicleGraphic.WestDiagonalRotated)
					{
						graphicRot = Rot8.North;
					}

					Vector3 drawOffset = graphicOverlay.graphic.DrawOffset(rot);
					Vector2 drawOffsetNoY = new Vector2(drawOffset.x, drawOffset.z); //p0

					Vector3 drawOffsetActual = graphicOverlay.graphic.DrawOffset(graphicRot);
					Vector2 drawOffsetActualNoY = new Vector2(drawOffsetActual.x, drawOffsetActual.z); //p1

					Vector2 drawOffsetAdjusted = Ext_Math.RotatePointClockwise(drawOffsetActualNoY, angle); //p2
					drawOffsetAdjusted -= drawOffsetNoY; //p3
					//Adds p3 (p2 - p0) which offsets the drawOffset being added in the draw worker, resulting in the drawOffset being p2 or the rotated p1 
					drawPos = new Vector3(drawPos.x + drawOffsetAdjusted.x, drawPos.y, drawPos.z + drawOffsetAdjusted.y); 
				}
				graphicOverlay.graphic.DrawWorker(drawPos, rot, null, null, vehicle.Angle + extraAngle);
			}
		}
	}
}
