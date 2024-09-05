using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;
using System.Security.Cryptography;

namespace Vehicles
{
	public static class VehicleGhostUtility
	{
		public static readonly Color whiteGhostColor = new Color(1, 1, 1, 0.5f);

		public static Dictionary<int, Graphic> cachedGhostGraphics = new Dictionary<int, Graphic>();

		public static void DrawGhostVehicleDef(IntVec3 center, Rot8 rot, VehicleDef vehicleDef, Color ghostCol, AltitudeLayer drawAltitude, VehiclePawn vehicle = null)
		{
			Graphic baseGraphic = vehicleDef.graphic;
			Graphic graphic = GhostUtility.GhostGraphicFor(baseGraphic, vehicleDef, ghostCol);
			Vector3 loc = GenThing.TrueCenter(center, rot, vehicleDef.Size, drawAltitude.AltitudeFor());

			Rot8 baseRot = rot;
			float baseAngle = rot.AsRotationAngle;
			//If diagonal rotated from North / South, vanilla draw needs adjustment in order to use the right graphics
			if (baseRot.IsDiagonal) 
			{
				switch (baseRot.AsInt) 
				{
					case 4:
						baseRot = Rot8.North;
						baseAngle = 45;
						break;
					case 5:
						baseRot = Rot8.South;
						baseAngle = -45;
						break;
					case 6:
						baseRot = Rot8.South;
						baseAngle = 45;
						break;
					case 7:
						baseRot = Rot8.North;
						baseAngle = -45;
						break;
				}
			}
			graphic.DrawFromDef(loc, baseRot, vehicleDef, baseAngle);
			
			DrawGhostOverlays(center, rot, vehicleDef, baseGraphic, ghostCol, drawAltitude, thing: vehicle);
		}

		public static void DrawGhostOverlays(IntVec3 center, Rot8 rot, VehicleDef vehicleDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
		{
			Vector3 loc = GenThing.TrueCenter(center, rot, vehicleDef.Size, drawAltitude.AltitudeFor());
			foreach ((Graphic graphic, float rotation) in vehicleDef.GhostGraphicOverlaysFor(ghostCol))
			{
				graphic.DrawWorker(loc + baseGraphic.DrawOffsetFull(rot), rot, vehicleDef, thing, rotation);
			}
			if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets)
			{
				vehicleDef.DrawGhostTurretTextures(loc, rot, ghostCol);
			}
		}

		public static Graphic_Turret GhostGraphicFor(this VehicleDef vehicleDef, VehicleTurret turret, Color ghostColor)
		{
			int num = 0;
			num = Gen.HashCombine(num, vehicleDef);
			num = Gen.HashCombine(num, turret);
			num = Gen.HashCombineStruct(num, ghostColor);
			if (!cachedGhostGraphics.TryGetValue(num, out Graphic graphic))
			{
				turret.ResolveCannonGraphics(vehicleDef, true);
				graphic = turret.CannonGraphic;

				GraphicData graphicData = new GraphicData();
				graphicData.CopyFrom(graphic.data);
				graphicData.drawOffsetWest = graphic.data.drawOffsetWest; //TEMPORARY - Bug in vanilla copies South over to West
				graphicData.shadowData = null;
				graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
				_ = graphicData.Graphic;

				graphic = (Graphic_Turret)GraphicDatabase.Get(graphic.GetType(), graphic.path, ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white, graphicData, null);
				
				cachedGhostGraphics.Add(num, graphic);
			}
			return (Graphic_Turret)graphic;
		}

		public static IEnumerable<(Graphic graphic, float rotation)> GhostGraphicOverlaysFor(this VehicleDef vehicleDef, Color ghostColor)
		{
			int num = 0;
			num = Gen.HashCombine(num, vehicleDef);
			num = Gen.HashCombineStruct(num, ghostColor);
			foreach (GraphicOverlay graphicOverlay in vehicleDef.drawProperties.overlays)
			{
				int hash = Gen.HashCombine(num, graphicOverlay.data.graphicData);
				if (!cachedGhostGraphics.TryGetValue(hash, out Graphic graphic))
				{
					graphic = graphicOverlay.Graphic;
					GraphicData graphicData = new GraphicData();
					graphicData.CopyFrom(graphic.data);
					graphicData.drawOffsetWest = graphic.data.drawOffsetWest; //TEMPORARY - Bug in vanilla copies South over to West
					graphicData.shadowData = null;
					graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
					_ = graphicData.Graphic;

					graphic = GraphicDatabase.Get(graphic.GetType(), graphic.path, ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white, graphicData, null);

					cachedGhostGraphics.Add(hash, graphic);
				}
				yield return (graphic, graphicOverlay.data.rotation);
			}
		}

		public static void DrawGhostTurretTextures(this VehicleDef vehicleDef, Vector3 loc, Rot8 rot, Color ghostColor)
		{
			if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
			{
				foreach (VehicleTurret turret in props.turrets)
				{
					if (turret.NoGraphic)
					{
						continue;
					}
					if (!turret.parentKey.NullOrEmpty())
					{
						continue;
					}

					turret.ResolveCannonGraphics(vehicleDef);
					
					try
					{
						float locationRotation = turret.defaultAngleRotated + rot.AsAngle;
						if (turret.attachedTo != null)
						{
							locationRotation += turret.attachedTo.defaultAngleRotated;// + rot.AsAngle;
						}
						Vector3 turretDrawLoc = turret.TurretDrawLocFor(rot);
						Vector3 turretLoc = loc + turretDrawLoc;

						if (!turret.NoGraphic)
						{
							Graphic graphic = vehicleDef.GhostGraphicFor(turret, ghostColor);
							Mesh cannonMesh = graphic.MeshAt(rot);
							Graphics.DrawMesh(cannonMesh, turretLoc, locationRotation.ToQuat(), graphic.MatAt(rot), 0);
						}
						//DrawTurretGhostOverlays(vehicleDef, turret, ghostColor, turretLoc, rot, locationRotation);
					}
					catch(Exception ex)
					{
						Log.Error($"Failed to render Cannon=\"{turret.turretDef.defName}\" for VehicleDef=\"{vehicleDef.defName}\", Exception: {ex}");
					}
				}
			}
		}

		private static void DrawTurretGhostOverlays(VehicleDef vehicleDef, VehicleTurret turret, Color ghostColor, Vector3 drawPos, Rot8 rot, float extraRotation)
		{
			if (!turret.TurretGraphics.NullOrEmpty())
			{
				for (int i = 0; i < turret.TurretGraphics.Count; i++)
				{
					Graphic graphic = vehicleDef.GhostGraphicFor(turret, ghostColor);
					VehicleTurret.TurretDrawData turretDrawData = turret.TurretGraphics[i];
					Vector3 rootPos = turretDrawData.DrawOffset(drawPos, rot);
					Mesh cannonMesh = graphic.MeshAt(rot);
					Graphics.DrawMesh(cannonMesh, rootPos, extraRotation.ToQuat(), graphic.MatAt(rot), 0);
				}
			}
		}
	}
}
