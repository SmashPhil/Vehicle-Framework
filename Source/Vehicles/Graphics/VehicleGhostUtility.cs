using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class VehicleGhostUtility
	{
		public static Dictionary<int, Graphic> cachedGhostGraphics = new Dictionary<int, Graphic>();

		public static Graphic_Turret GhostGraphicFor(this VehicleDef vehicleDef, VehicleTurret cannon, Color ghostColor)
		{
			int num = 0;
			num = Gen.HashCombine(num, vehicleDef);
			num = Gen.HashCombine(num, cannon);
			num = Gen.HashCombineStruct(num, ghostColor);
			if (!cachedGhostGraphics.TryGetValue(num, out Graphic graphic))
			{
				cannon.ResolveCannonGraphics(vehicleDef, true);
				graphic = cannon.CannonGraphic;

				GraphicData graphicData = new GraphicData();
				AccessTools.Method(typeof(GraphicData), "Init").Invoke(graphicData, new object[] { });
				graphicData.CopyFrom(graphic.data);
				graphicData.shadowData = null;

				graphic = (Graphic_Turret)GraphicDatabase.Get(graphic.GetType(), graphic.path, ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white, graphicData, null);
				
				cachedGhostGraphics.Add(num, graphic);
			}
			return (Graphic_Turret)graphic;
		}

		public static IEnumerable<GraphicOverlay> GhostGraphicOverlaysFor(this VehicleDef vehicleDef, Color ghostColor)
		{
			int num = 0;
			num = Gen.HashCombine(num, vehicleDef);
			num = Gen.HashCombineStruct(num, ghostColor);
			foreach (GraphicOverlay graphicOverlay in vehicleDef.drawProperties.OverlayGraphics)
			{
				int hash = Gen.HashCombine(num, graphicOverlay.graphic.data.texPath);
				if (!cachedGhostGraphics.TryGetValue(hash, out Graphic graphic))
				{
					graphic = graphicOverlay.graphic;
					GraphicData graphicData = new GraphicData();
					AccessTools.Method(typeof(GraphicData), "Init").Invoke(graphicData, new object[] { });
					graphicData.CopyFrom(graphic.data);
					graphicData.shadowData = null;

					graphic = GraphicDatabase.Get(graphic.GetType(), graphic.path, ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white, graphicData, null);

					cachedGhostGraphics.Add(hash, graphic);
				}
				yield return new GraphicOverlay(graphic, graphicOverlay.rotation);
			}
		}

		public static void DrawGhostTurretTextures(this VehicleDef vehicleDef, Vector3 loc, Rot8 rot, Color ghostCol)
		{
			if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
			{
				foreach (VehicleTurret turret in props.turrets)
				{
					if (turret.NoGraphic)
					{
						continue;
					}

					turret.ResolveCannonGraphics(vehicleDef);

					try
					{
						Graphic graphic = vehicleDef.GhostGraphicFor(turret, ghostCol);
						float locationRotation = turret.defaultAngleRotated + rot.AsAngle;
						if (turret.attachedTo != null)
						{
							locationRotation += turret.attachedTo.defaultAngleRotated + rot.AsAngle;
						}
						Vector3 turretLoc = turret.TurretDrawLocFor(rot, loc);
						Mesh cannonMesh = graphic.MeshAt(rot);
						Graphics.DrawMesh(cannonMesh, turretLoc, locationRotation.ToQuat(), graphic.MatAt(rot), 0);
					}
					catch(Exception ex)
					{
						Log.Error($"Failed to render Cannon=\"{turret.turretDef.defName}\" for VehicleDef=\"{vehicleDef.defName}\", Exception: {ex.Message}");
					}
				}
			}
		}
	}
}
