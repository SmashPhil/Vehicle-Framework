using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleGraphicOverlay
	{
		public readonly VehiclePawn vehicle;

		public readonly ExtraRotationRegistry rotationRegistry;

		public VehicleGraphicOverlay(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			rotationRegistry = new ExtraRotationRegistry(this);
		}

		public List<GraphicOverlay> Overlays => vehicle.VehicleDef.drawProperties.overlays;

		public virtual void RenderGraphicOverlays(Vector3 drawPos, float angle, Rot8 rot)
		{
			float extraAngle;
			foreach (GraphicOverlay graphicOverlay in Overlays)
			{
				float overlayAngle = angle;
				extraAngle = graphicOverlay.data.rotation;
				Vector3 overlayDrawPos = drawPos;
				if (graphicOverlay.data.component != null)
				{
					float healthPercent = vehicle.statHandler.GetComponentHealthPercent(graphicOverlay.data.component.key);
					if (!graphicOverlay.data.component.comparison.Compare(healthPercent, graphicOverlay.data.component.healthPercent))
					{
						continue; //Skip rendering if health percent is below set amount for rendering
					}
				}
				if (!graphicOverlay.data.graphicData.AboveBody)
				{
					overlayDrawPos -= new Vector3(0, VehicleRenderer.YOffset_Body + VehicleRenderer.SubInterval, 0);
				}
				if (graphicOverlay.data.graphicData.Graphic is Graphic_Rotator rotator)
				{
					extraAngle += rotationRegistry[rotator.RegistryKey].ClampAndWrap(0, 359);
				}
				if (overlayAngle != 0)
				{
					Rot8 graphicRot = rot;
					if (rot == Rot8.NorthEast && vehicle.VehicleGraphic.EastDiagonalRotated)
					{
						graphicRot = Rot8.North;
						overlayAngle *= -1; //Flip angle for clockwise rotation facing north
					}
					else if (rot == Rot8.SouthEast && vehicle.VehicleGraphic.EastDiagonalRotated)
					{
						graphicRot = Rot8.South;
						overlayAngle *= -1; //Flip angle for clockwise rotation facing south
					}
					else if (rot == Rot8.SouthWest && vehicle.VehicleGraphic.WestDiagonalRotated)
					{
						graphicRot = Rot8.South;
						overlayAngle *= -1; //Flip angle for clockwise rotation facing south
					}
					else if (rot == Rot8.NorthWest && vehicle.VehicleGraphic.WestDiagonalRotated)
					{
						graphicRot = Rot8.North;
						overlayAngle *= -1; //Flip angle for clockwise rotation facing north
					}

					Vector3 drawOffset = graphicOverlay.data.graphicData.Graphic.DrawOffset(rot);
					Vector2 drawOffsetNoY = new Vector2(drawOffset.x, drawOffset.z); //p0

					Vector3 drawOffsetActual = graphicOverlay.data.graphicData.Graphic.DrawOffset(graphicRot);
					Vector2 drawOffsetActualNoY = new Vector2(drawOffsetActual.x, drawOffsetActual.z); //p1

					Vector2 drawOffsetAdjusted = Ext_Math.RotatePointClockwise(drawOffsetActualNoY, overlayAngle); //p2
					drawOffsetAdjusted -= drawOffsetNoY; //p3
														 //Adds p3 (p2 - p0) which offsets the drawOffset being added in the draw worker, resulting in the drawOffset being p2 or the rotated p1 
					overlayDrawPos = new Vector3(overlayDrawPos.x + drawOffsetAdjusted.x, overlayDrawPos.y, overlayDrawPos.z + drawOffsetAdjusted.y); 
				}
				graphicOverlay.data.graphicData.Graphic.DrawWorker(overlayDrawPos, rot, null, null, vehicle.Angle + extraAngle);
			}
		}
	}
}
