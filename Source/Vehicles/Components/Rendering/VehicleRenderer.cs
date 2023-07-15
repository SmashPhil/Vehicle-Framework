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
		private const float SubInterval = 0.003787879f;
		private const float YOffset_Body = 0.007575758f;
		private const float YOffset_Damage = 0.018939395f;

		private readonly VehiclePawn vehicle;

		public VehicleGraphicSet graphics;

		private Graphic_Shadow shadowGraphic;

		public VehicleRenderer(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			graphics = new VehicleGraphicSet(vehicle);
		}

		private PawnRenderFlags DefaultRenderFlags
		{
			get
			{
				PawnRenderFlags pawnRenderFlags = PawnRenderFlags.None;
				if (vehicle.IsInvisible())
				{
					pawnRenderFlags |= PawnRenderFlags.Invisible;
				}
				return pawnRenderFlags;
			}
		}

		public void RenderPawnAt(Vector3 drawLoc, float angle, bool northSouthRotation)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}

			RenderPawnInternal(drawLoc, angle, northSouthRotation, DefaultRenderFlags);
			
			if (vehicle.def.race.specialShadowData != null)
			{
				if (shadowGraphic == null)
				{
					shadowGraphic = new Graphic_Shadow(vehicle.def.race.specialShadowData);
				}
				shadowGraphic.Draw(drawLoc, Rot4.North, vehicle, 0f);
			}
			if (graphics.vehicle.VehicleGraphic != null && graphics.vehicle.VehicleGraphic.ShadowGraphic != null)
			{
				graphics.vehicle.VehicleGraphic.ShadowGraphic.Draw(drawLoc, Rot4.North, vehicle, 0f);
			}
			if (vehicle.Spawned && !vehicle.Dead)
			{
				//vehicle.stances.StanceTrackerDraw();
				vehicle.vPather.PatherDraw();
			}
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, bool northSouthRotation, PawnRenderFlags flags)
		{
			vehicle.UpdateRotationAndAngle();
			(Vector3 aboveBodyPos, Rot8 rot) = RenderPawnInternal(rootLoc, angle, vehicle.Rotation, northSouthRotation, flags);
			//Log.Message($"Rendering {vehicle} at {rootLoc} angle={angle}");
			vehicle.graphicOverlay.RenderGraphicOverlays(aboveBodyPos, angle, rot);
		}

		private (Vector3 aboveBodyPos, Rot8 rot) RenderPawnInternal(Vector3 rootLoc, float angle, Rot4 bodyFacing, bool northSouthRotation, PawnRenderFlags flags)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			bool portraitDraw = !flags.FlagSet(PawnRenderFlags.Portrait) && !flags.FlagSet(PawnRenderFlags.Cache);

			Quaternion quaternion = Quaternion.AngleAxis(angle * (northSouthRotation ? -1 : 1), Vector3.up);

			Vector3 aboveBodyPos = rootLoc + vehicle.VehicleGraphic.DrawOffset(bodyFacing);
			aboveBodyPos.y += YOffset_Body;
			Rot8 vehicleRot = new Rot8(bodyFacing, angle);
			Mesh mesh = graphics.vehicle.VehicleGraphic.MeshAtFull(vehicleRot);
			List<Material> list = graphics.MatsBodyBaseAt(bodyFacing, RotDrawMode.Fresh);

			for (int i = 0; i < list.Count; i++)
			{
				GenDraw.DrawMeshNowOrLater(mesh, aboveBodyPos, quaternion, list[i], flags.FlagSet(PawnRenderFlags.DrawNow));
				aboveBodyPos.y += SubInterval;
			}

			Vector3 drawLoc = rootLoc;
			drawLoc.y += YOffset_Damage;
			//TODO - Render overlays for vehicle damage

			//TODO - pack graphics?
			if (!portraitDraw && vehicle.inventory != null && vehicle.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
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
