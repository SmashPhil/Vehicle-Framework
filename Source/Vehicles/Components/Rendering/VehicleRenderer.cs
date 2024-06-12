using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public sealed class VehicleRenderer
	{
		public const float SubInterval = 0.003787879f;
		public const float YOffset_Body = 0.007575758f;
		public const float YOffset_Damage = 0.018939395f;
		public const float YOffset_CoveredInOverlay = 0.033301156f;

		private readonly VehiclePawn vehicle;

		public VehicleGraphicSet graphics;

		//private Graphic_DynamicShadow shadowGraphic;

		//FOR TESTING ONLY
		private PawnFirefoamDrawer firefoamOverlays;

		public VehicleRenderer(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			graphics = new VehicleGraphicSet(vehicle);

			//firefoamOverlays = new PawnFirefoamDrawer(vehicle);
		}

		[Obsolete("Not currently implemented, still WIP. Do not reference.", error: true)]
		public PawnFirefoamDrawer FirefoamOverlays => firefoamOverlays;

		public void RenderPawnAt(Vector3 drawLoc, float angle, bool northSouthRotation)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}

			RenderPawnInternal(drawLoc, angle, northSouthRotation);
			
			if (vehicle.def.race.specialShadowData != null)
			{
				//if (shadowGraphic == null)
				//{
				//	shadowGraphic = new Graphic_DynamicShadow(vehicle.def.race.specialShadowData);
				//}
				//shadowGraphic.Draw(drawLoc, Rot4.North, vehicle, 0f);
			}
			if (graphics.vehicle.VehicleGraphic?.ShadowGraphic != null)
			{
				graphics.vehicle.VehicleGraphic.ShadowGraphic.Draw(drawLoc, vehicle.FullRotation, vehicle, 0f);
			}
			if (vehicle.Spawned && !vehicle.Dead)
			{
				//vehicle.stances.StanceTrackerDraw();
				vehicle.vehiclePather.PatherDraw();
			}
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, bool northSouthRotation)
		{
			vehicle.UpdateRotationAndAngle();
			(Vector3 aboveBodyPos, Rot8 rot) = RenderPawnInternal(rootLoc, angle, vehicle.Rotation, northSouthRotation);
			vehicle.DrawExplosiveWicks(aboveBodyPos, rot);
			vehicle.graphicOverlay.RenderGraphicOverlays(aboveBodyPos, angle, rot);
		}

		private (Vector3 aboveBodyPos, Rot8 rot) RenderPawnInternal(Vector3 rootLoc, float angle, Rot4 bodyFacing, bool northSouthRotation)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			
			Quaternion quaternion = Quaternion.AngleAxis(angle * (northSouthRotation ? -1 : 1), Vector3.up);

			Vector3 aboveBodyPos = rootLoc + vehicle.VehicleGraphic.DrawOffset(bodyFacing);
			aboveBodyPos.y += YOffset_Body;
			Rot8 vehicleRot = new Rot8(bodyFacing, angle);
			Mesh mesh = graphics.vehicle.VehicleGraphic.MeshAtFull(vehicleRot);
			List<Material> list = graphics.MatsBodyBaseAt(vehicleRot);

			for (int i = 0; i < list.Count; i++)
			{
				GenDraw.DrawMeshNowOrLater(mesh, aboveBodyPos, quaternion, list[i], false);
				aboveBodyPos.y += SubInterval;
			}

			Vector3 drawLoc = rootLoc;
			drawLoc.y += YOffset_Damage;
			//TODO - Render overlays for vehicle
			//if (firefoamOverlays.IsCoveredInFoam)
			//{
			//	Vector3 overlayPos = rootLoc;
			//	overlayPos.y += YOffset_CoveredInOverlay;
			//	firefoamOverlays.RenderPawnOverlay(overlayPos, mesh, quaternion, flags.FlagSet(PawnRenderFlags.DrawNow), PawnOverlayDrawer.OverlayLayer.Body, bodyFacing);
			//}

			//TODO - pack graphics?
			if (vehicle.inventory != null && vehicle.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
			{
				Graphics.DrawMesh(mesh, drawLoc, quaternion, graphics.packGraphic.MatAt(bodyFacing, null), 0);
			}
			return (aboveBodyPos, vehicleRot);
		}

		public void ProcessPostTickVisuals(int ticksPassed)
		{
		}
	}
}
