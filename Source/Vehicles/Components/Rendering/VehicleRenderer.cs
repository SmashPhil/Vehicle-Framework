using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class VehicleRenderer
	{
		private const float SubInterval = 0.003787879f;
		private const float YOffset_Body = 0.007575758f;
		private const float YOffset_Wounds = 0.018939395f;
		private const float YOffset_Shell = 0.022727273f;
		private const float YOffset_Head = 0.026515152f;
		private const float YOffset_Status = 0.041666668f;

		private readonly VehiclePawn vehicle;

		public VehicleGraphicSet graphics;

		private readonly PawnHeadOverlays statusOverlays;

		private VehicleStatusEffecters effecters;

		private PawnWoundDrawer woundOverlays;

		private Graphic_Shadow shadowGraphic;

		public VehicleRenderer(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			statusOverlays = new PawnHeadOverlays(vehicle);
			woundOverlays = new PawnWoundDrawer(vehicle);
			graphics = new VehicleGraphicSet(vehicle);
			effecters = new VehicleStatusEffecters(vehicle);
		}

		public void RenderPawnAt(Vector3 drawLoc, float angle)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}

			RenderPawnInternal(drawLoc, angle);

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

		public void RenderPortrait()
		{
			Vector3 zero = Vector3.zero;
			float angle;
			if (vehicle.Dead || vehicle.Downed)
			{
				angle = 85f;
				zero.x -= 0.18f;
				zero.z -= 0.18f;
			}
			else
			{
				angle = 0f;
			}
			RenderPawnInternal(zero, angle, Rot4.South, true);
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle)
		{
			vehicle.UpdateRotationAndAngle();
			RenderPawnInternal(rootLoc, angle, vehicle.Rotation, false);
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, Rot4 bodyFacing, bool portrait)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);

			Vector3 loc = rootLoc + vehicle.VehicleGraphic.DrawOffset(bodyFacing);
			loc.y += YOffset_Body;

			Mesh mesh = graphics.vehicle.VehicleGraphic.MeshAt(bodyFacing);
			List<Material> list = graphics.MatsBodyBaseAt(bodyFacing, RotDrawMode.Fresh);

			for (int i = 0; i < list.Count; i++)
			{
				GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, list[i], portrait);
				loc.y += SubInterval;
			}

			Vector3 drawLoc = rootLoc;
			drawLoc.y += YOffset_Wounds;
			woundOverlays.RenderOverBody(drawLoc, mesh, quaternion, portrait);

			Vector3 vector = rootLoc;
			Vector3 a = rootLoc;
			if (bodyFacing != Rot4.North)
			{
				a.y += YOffset_Head;
				vector.y += YOffset_Shell;
			}
			else
			{
				a.y += YOffset_Shell;
				vector.y += YOffset_Head;
			}
			//REDO
			if (!portrait && vehicle.RaceProps.Animal && vehicle.inventory != null && vehicle.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
			{
				Graphics.DrawMesh(mesh, vector, quaternion, graphics.packGraphic.MatAt(bodyFacing, null), 0);
			}
			if (!portrait)
			{
				Vector3 bodyLoc = rootLoc;
				bodyLoc.y += YOffset_Status;
				statusOverlays.RenderStatusOverlays(bodyLoc, quaternion, MeshPool.humanlikeHeadSet.MeshAt(bodyFacing));
			}
		}

		public Rot4 LayingFacing()
		{
			if (vehicle.GetPosture() == PawnPosture.LayingOnGroundFaceUp)
			{
				return Rot4.South;
			}
			if (vehicle.RaceProps.Humanlike)
			{
				switch (vehicle.thingIDNumber % 4)
				{
				case 0:
					return Rot4.South;
				case 1:
					return Rot4.South;
				case 2:
					return Rot4.East;
				case 3:
					return Rot4.West;
				}
			}
			else
			{
				switch (vehicle.thingIDNumber % 4)
				{
				case 0:
					return Rot4.South;
				case 1:
					return Rot4.East;
				case 2:
					return Rot4.West;
				case 3:
					return Rot4.West;
				}
			}
			return Rot4.Random;
		}

		public float BodyAngle()
		{
			if (vehicle.GetPosture() == PawnPosture.Standing)
			{
				return 0f;
			}
			Building_Bed building_Bed = vehicle.CurrentBed();
			if (building_Bed != null && vehicle.RaceProps.Humanlike)
			{
				Rot4 rotation = building_Bed.Rotation;
				rotation.AsInt += 2;
				return rotation.AsAngle;
			}
			if (vehicle.RaceProps.Humanlike)
			{
				return LayingFacing().AsAngle;
			}
			Rot4 rot = Rot4.West;
			int num = vehicle.thingIDNumber % 2;
			if (num != 0)
			{
				if (num == 1)
				{
					rot = Rot4.East;
				}
			}
			else
			{
				rot = Rot4.West;
			}
			return rot.AsAngle;
		}

		public Vector3 BaseHeadOffsetAt(Rot4 rotation)
		{
			Vector2 headOffset = vehicle.story.bodyType.headOffset;
			switch (rotation.AsInt)
			{
			case 0:
				return new Vector3(0f, 0f, headOffset.y);
			case 1:
				return new Vector3(headOffset.x, 0f, headOffset.y);
			case 2:
				return new Vector3(0f, 0f, headOffset.y);
			case 3:
				return new Vector3(-headOffset.x, 0f, headOffset.y);
			default:
				Log.Error("BaseHeadOffsetAt error in " + vehicle);
				return Vector3.zero;
			}
		}

		public void RendererTick()
		{
			effecters.EffectersTick();
		}
	}
}
