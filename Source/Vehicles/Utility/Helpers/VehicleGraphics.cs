using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public static class VehicleGraphics
	{
		/// <summary>
		/// DrawOffset for full rotation <paramref name="rot"/>
		/// </summary>
		/// <param name="graphic"></param>
		/// <param name="rot"></param>
		public static Vector3 DrawOffsetFull(this Graphic graphic, Rot8 rot)
		{
			return graphic.data.DrawOffsetFull(rot);
		}

		/// <summary>
		/// DrawOffset for full rotation <paramref name="rot"/>
		/// </summary>
		/// <param name="graphicData"></param>
		/// <param name="rot"></param>
		public static Vector3 DrawOffsetFull(this GraphicData graphicData, Rot8 rot)
		{
			Vector2 offset = VehicleDrawOffset(rot, graphicData.drawOffset.x, graphicData.drawOffset.y);
			return new Vector3(offset.x, graphicData.drawOffset.y, offset.y);
		}

		/// <summary>
		/// Calculate VehicleTurret draw offset
		/// </summary>
		/// <param name="rot"></param>
		/// <param name="renderProps"></param>
		/// <param name="extraRotation"></param>
		/// <param name="attachedTo"></param>
		public static Vector2 TurretDrawOffset(Rot8 rot, VehicleTurretRender renderProps, float extraRotation = 0, VehicleTurret attachedTo = null)
		{
			Vector2 turretOffset = renderProps.OffsetFor(rot);
			if (attachedTo != null)
			{
				Vector2 parentOffset = attachedTo.renderProperties.OffsetFor(rot);
				turretOffset = TEMP_ConvertRelativeOffset(rot, turretOffset);
				Vector2 rootLoc = Ext_Math.RotatePointClockwise(turretOffset.x, turretOffset.y, extraRotation);
				return new Vector2(rootLoc.x + parentOffset.x, rootLoc.y + parentOffset.y);
			}
			return turretOffset;
		}

		private static Vector2 TEMP_ConvertRelativeOffset(Rot8 rot, Vector2 offset)
		{
			return rot.AsInt switch
			{
				0 => offset,
				1 => offset * new Vector2(-1, -1),
				2 => offset,
				3 => offset * new Vector2(-1, 1),
				4 => new Vector2(-1 * offset.y, offset.x),
				5 => new Vector2(offset.y, -1 * offset.x),
				6 => new Vector2(-1 * offset.y, offset.x),
				7 => new Vector2(offset.y, -1 * offset.x),
				_ => offset,
			};
		}

		/// <summary>
		/// Calculate draw offset given offsets from center rotated alongside <paramref name="rot"/>
		/// </summary>
		/// <param name="rot"></param>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		/// <param name="turretRotation"></param>
		/// <param name="attachedTo"></param>
		public static Vector2 VehicleDrawOffset(Rot8 rot, float offsetX, float offsetY, float additionalRotation = 0)
		{
			return Ext_Math.RotatePointClockwise(offsetX, offsetY, rot.AsAngle + additionalRotation);
		}

		/// <summary>
		/// Draw VehicleTurret on vehicle
		/// </summary>
		/// <param name="turret"></param>
		public static void DrawTurret(VehicleTurret turret, Rot8 rot)
		{
			DrawTurret(turret, turret.vehicle.DrawPos, rot);
		}

		public static void DrawTurret(VehicleTurret turret, Vector3 drawPos, Rot8 rot)
		{
			try
			{
				Vector3 turretDrawLoc = turret.TurretDrawLocFor(rot);
				Vector3 rootPos = drawPos + turretDrawLoc;
				Vector3 recoilOffset = Vector3.zero;
				Vector3 parentRecoilOffset = Vector3.zero;
				if (turret.recoilTracker != null && turret.recoilTracker.Recoil > 0f)
				{
					recoilOffset = Ext_Math.PointFromAngle(Vector3.zero, turret.recoilTracker.Recoil, turret.recoilTracker.Angle);
				}
				if (turret.attachedTo?.recoilTracker != null && turret.attachedTo.recoilTracker.Recoil > 0f)
				{
					parentRecoilOffset = Ext_Math.PointFromAngle(Vector3.zero, turret.attachedTo.recoilTracker.Recoil, turret.attachedTo.recoilTracker.Angle);
				}
				Mesh cannonMesh = turret.CannonGraphic.MeshAt(rot);
				Graphics.DrawMesh(cannonMesh, rootPos + recoilOffset + parentRecoilOffset, turret.TurretRotation.ToQuat(), turret.CannonMaterial, 0);

				DrawTurretOverlays(turret, rootPos + parentRecoilOffset, rot);
			}
			catch (Exception ex)
			{
				Log.Error($"Error occurred during rendering of attached thing on {turret.vehicle.Label}. Exception: {ex}");
			}
		}

		public static void DrawTurretOverlays(VehicleTurret turret, Vector3 drawPos, Rot8 rot)
		{
			try
			{
				if (!turret.TurretGraphics.NullOrEmpty())
				{
					for (int i = 0; i < turret.TurretGraphics.Count; i++)
					{
						VehicleTurret.TurretDrawData turretDrawData = turret.TurretGraphics[i];
						Turret_RecoilTracker recoilTracker = turret.recoilTrackers[i];

						Vector3 rootPos = turretDrawData.DrawOffset(drawPos, rot);
						Vector3 recoilOffset = Vector3.zero;
						Vector3 parentRecoilOffset = Vector3.zero;
						if (recoilTracker != null && recoilTracker.Recoil > 0f)
						{
							recoilOffset = Ext_Math.PointFromAngle(Vector3.zero, recoilTracker.Recoil, recoilTracker.Angle);
						}
						if (turret.attachedTo != null && turret.attachedTo.recoilTracker != null && turret.attachedTo.recoilTracker.Recoil > 0f)
						{
							parentRecoilOffset = Ext_Math.PointFromAngle(Vector3.zero, turret.attachedTo.recoilTracker.Recoil, turret.attachedTo.recoilTracker.Angle);
						}
						Mesh cannonMesh = turretDrawData.graphic.MeshAt(rot);
						Graphics.DrawMesh(cannonMesh, rootPos + recoilOffset + parentRecoilOffset, turret.TurretRotation.ToQuat(), turretDrawData.graphic.MatAt(Rot4.North), 0);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Error occurred during rendering of layered turret graphics on {turret.vehicle.Label}. Exception: {ex}");
			}
		}

		public static string DrawVehicle(Rect rect, VehiclePawn vehicle, Rot8? rot = null, List<GraphicOverlay> extraOverlays = null)
		{
			return DrawVehicleDef(rect, vehicle.VehicleDef, patternData: vehicle.patternData, rot: rot, extraOverlays: extraOverlays);
		}

		/// <summary>
		/// Draw <paramref name="vehicleDef"/>
		/// </summary>
		/// <remarks><paramref name="material"/> may overwrite material used for vehicle</remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="material"></param>
		public static string DrawVehicleDef(Rect rect, VehicleDef vehicleDef, PatternData patternData = null, Rot8? rot = null, bool withoutTurrets = false, List<GraphicOverlay> extraOverlays = null)
		{
			string drawStep = string.Empty;
			try
			{
				if (rect.width != rect.height)
				{
					SmashLog.WarningOnce("Drawing VehicleDef with non-uniform rect. VehicleDefs are best drawn in square rects which will then be adjusted to fit.", nameof(DrawVehicleDef).GetHashCode());
				}
				drawStep = "Setting rect and adjusted positioning.";
				Vector2 rectSize = vehicleDef.ScaleDrawRatio(rect.size);
				Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;

				bool elongated = rotDrawn.IsHorizontal || rotDrawn.IsDiagonal;

				Vector2 displayOffset = vehicleDef.drawProperties.DisplayOffsetForRot(rotDrawn);
				float scaledWidth = rectSize.x;
				float scaledHeight = rectSize.y;
				if (elongated)
				{
					scaledWidth = rectSize.y;
					scaledHeight = rectSize.x;
				}
				float offsetX = (rect.width - scaledWidth) / 2 + (displayOffset.x * rect.width);
				float offsetY = (rect.height - scaledHeight) / 2 + (displayOffset.y * rect.height);

				Rect adjustedRect = new Rect(rect.x + offsetX, rect.y + offsetY, scaledWidth, scaledHeight);

				drawStep = "Retrieving cached graphic and pattern";
				Graphic_Vehicle graphic = vehicleDef.graphicData.Graphic as Graphic_Vehicle;// VehicleTex.CachedGraphics[vehicleDef];

				PatternData pattern = patternData;
				if (pattern is null)
				{
					drawStep = "Setting default color";
					pattern = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData);
				}

				Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);

				Material material = null;
				if (graphic.Shader.SupportsRGBMaskTex())
				{
					drawStep = $"Fetching material for {vehicleDef}";

					material = RGBMaterialPool.Get(vehicleDef, rotDrawn);
					RGBMaterialPool.SetProperties(vehicleDef, pattern, graphic.TexAt, graphic.MaskAt);
				}
				else
				{
					//material = vehicleDef.graphicData.Graphic.MatAt(rotDrawn);
				}
				
				drawStep = "Attempting to retrieve turret overlays";
				List<(Rect rect, Texture mainTex, Material material, float layer, float angle)> overlays = new List<(Rect, Texture, Material, float, float)>();
				if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
				{
					if (!withoutTurrets || Prefs.UIScale == 1) //NOTE: Temporary fix until Ludeon fixes vanilla bug with matrix rotations inside GUI groups
					{
						overlays.AddRange(RetrieveAllTurretSettingsGraphicsProperties(rect, vehicleDef, rotDrawn, props.turrets.OrderBy(x => x.drawLayer), pattern));
					}
				}
				drawStep = "Retrieving graphic overlays";
				overlays.AddRange(RetrieveAllOverlaySettingsGraphicsProperties(rect, vehicleDef, rotDrawn, pattern: pattern, extraOverlays: extraOverlays));

				drawStep = "Rendering overlays with layer < 0";
				foreach (var overlay in overlays.Where(overlay => overlay.layer < 0).OrderBy(overlay => overlay.layer))
				{
					UIElements.DrawTextureWithMaterialOnGUI(overlay.rect, overlay.mainTex, overlay.material, overlay.angle);
				}

				drawStep = "Rendering main texture";
				DrawVehicleFitted(adjustedRect, angle, mainTex, material);

				drawStep = "Rendering overlays with layer >= 0";
				foreach (var overlay in overlays.Where(overlay => overlay.layer >= 0).OrderBy(overlay => overlay.layer))
				{
					UIElements.DrawTextureWithMaterialOnGUI(overlay.rect, overlay.mainTex, overlay.material, overlay.angle);
				}
				return string.Empty;
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to draw Graphics <type>VehicleDef</type>=\"{vehicleDef?.defName ?? "Null"}\" Exception={ex}");
			}
			return drawStep;
		}

		public static IEnumerable<(Rect rect, Texture mainTex, Material material, float layer, float angle)> RetrieveAllOverlaySettingsGraphicsProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, PatternData pattern = null, List<GraphicOverlay> extraOverlays = null)
		{
			List<GraphicOverlay> overlays = vehicleDef.drawProperties.overlays;
			foreach (GraphicOverlay graphicOverlay in overlays)
			{
				if (graphicOverlay.data.renderUI)
				{
					yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicleDef, rot, graphicOverlay, pattern: pattern);
				}
			}
			if (!extraOverlays.NullOrEmpty())
			{
				foreach (GraphicOverlay graphicOverlay in extraOverlays)
				{
					if (graphicOverlay.data.renderUI)
					{
						yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicleDef, rot, graphicOverlay, pattern: pattern);
					}
				}
			}
		}

		public static (Rect rect, Texture mainTex, Material material, float layer, float angle) RetrieveOverlaySettingsGraphicsProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, GraphicOverlay graphicOverlay, PatternData pattern)
		{
			Rect overlayRect = OverlayRect(rect, vehicleDef, graphicOverlay, rot);
			Graphic graphic = graphicOverlay.Graphic;
			Texture2D texture;
			Material material = null;

			Graphic_RGB graphicRGB = graphic as Graphic_RGB;
			if (graphicRGB != null)
			{
				texture = graphicRGB.TexAt(rot);
			}
			else
			{
				texture = graphic.MatAt(rot).mainTexture as Texture2D;
			}
			
			if (graphic.Shader.SupportsRGBMaskTex())
			{
				material = RGBMaterialPool.Get(graphicOverlay, rot);
				RGBMaterialPool.SetProperties(graphicOverlay, pattern, graphicRGB.TexAt, graphicRGB.MaskAt);
			}
			else if (graphic.Shader.SupportsMaskTex())
			{
				material = graphic.MatAt(rot);
			}
			return (overlayRect, texture, material, graphicOverlay.data.graphicData.DrawOffsetFull(rot).y, graphicOverlay.data.rotation);
		}

		/// <summary>
		/// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="turrets"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		public static IEnumerable<(Rect rect, Texture mainTex, Material material, float layer, float angle)> RetrieveAllTurretSettingsGraphicsProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (!turret.parentKey.NullOrEmpty())
				{
					continue; //Attached turrets temporarily disabled from rendering
				}
				if (!turret.NoGraphic)
				{
					yield return RetrieveTurretSettingsGraphicsProperties(rect, vehicleDef, rot, turret, patternData);
				}
				if (!turret.TurretGraphics.NullOrEmpty())
				{
					foreach (VehicleTurret.TurretDrawData turretDrawData in turret.TurretGraphics)
					{
						Rect turretRect = TurretRect(rect, vehicleDef, turret, rot);
						Material material = null;
						if (turretDrawData.graphic.Shader.SupportsMaskTex())
						{
							//material = turretDrawData.graphic.MatAt(Rot8.North);
						}
						else if (patternData != null && turretDrawData.graphic.Shader.SupportsRGBMaskTex())
						{
							material = RGBMaterialPool.Get(turretDrawData, Rot8.North);
							RGBMaterialPool.SetProperties(turretDrawData, patternData, turretDrawData.graphic.TexAt, turretDrawData.graphic.MaskAt);
						}
						yield return (turretRect, turretDrawData.graphic.TexAt(Rot8.North), material, turretDrawData.graphicDataRGB.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
					}
				}
			}
		}

		public static (Rect rect, Texture mainTex, Material material, float layer, float angle) RetrieveTurretSettingsGraphicsProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, VehicleTurret turret, PatternData patternData)
		{
			Rect turretRect = TurretRect(rect, vehicleDef, turret, rot);
			Material material = null;
			if (turret.CannonGraphic.Shader.SupportsMaskTex())
			{
				//material = turret.CannonGraphic.MatAt(Rot8.North);
			}
			else if (patternData != null && turret.CannonGraphic.Shader.SupportsRGBMaskTex())
			{
				material = RGBMaterialPool.Get(turret, Rot8.North);
				RGBMaterialPool.SetProperties(turret, patternData, turret.CannonGraphic.TexAt, turret.CannonGraphic.MaskAt);
			}
			return (turretRect, turret.CannonTexture, material, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
		}

		/// <summary>
		/// Retrieves VehicleTurret Rect adjusted to <paramref name="rect"/> of where it's being rendered.
		/// </summary>
		/// <remarks>Scales up / down relative to drawSize of <paramref name="vehicleDef"/>. Best used inside GUI Group</remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="turret"></param>
		/// <param name="rot"></param>
		internal static Rect TurretRect(Rect rect, VehicleDef vehicleDef, VehicleTurret turret, Rot8 rot, float iconScale = 1)
		{
			//Ensure CannonGraphics are up to date (only required upon changes to default pattern from mod settings)
			turret.ResolveCannonGraphics(vehicleDef);
			return turret.ScaleUIRectRecursive(vehicleDef, rect, rot, iconScale: iconScale);
		}

		/// <summary>
		/// Retrieve GraphicOverlay adjusted to <paramref name="rect"/> of where it's being rendered.
		/// </summary>
		/// <remarks>Best used inside GUI Group</remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="graphicOverlay"></param>
		/// <param name="rot"></param>
		internal static Rect OverlayRect(Rect rect, VehicleDef vehicleDef, GraphicOverlay graphicOverlay, Rot8 rot)
		{
			//Scale to VehicleDef drawSize
			Vector2 size = vehicleDef.ScaleDrawRatio(graphicOverlay.data.graphicData, rot, rect.size);
			//Adjust position from new rect size
			Vector2 adjustedPosition = rect.position + (rect.size - size) / 2f;
			// Size / V_max = scalar
			float scalar = rect.size.x / Mathf.Max(vehicleDef.graphicData.drawSize.x, vehicleDef.graphicData.drawSize.y);
			
			Vector3 graphicOffset = graphicOverlay.data.graphicData.DrawOffsetForRot(rot);

			//Invert y axis post-calculations, UI y-axis is top to bottom
			Vector2 position = adjustedPosition + (scalar * new Vector2(graphicOffset.x, -graphicOffset.z));
			return new Rect(position, size);
		}

		public static void DrawVehicleFitted(Rect rect, VehicleDef vehicleDef, Rot4 rot, Material material)
		{
			Texture2D vehicleIcon = VehicleTex.VehicleTexture(vehicleDef, rot, out float angle);
			Rect texCoords = new Rect(0, 0, 1, 1);
			Vector2 texProportions = vehicleDef.graphicData.drawSize;
			if (rot.IsHorizontal)
			{
				float x = texProportions.x;
				texProportions.x = texProportions.y;
				texProportions.y = x;
			}
			Widgets.DrawTextureFitted(rect, vehicleIcon, GenUI.IconDrawScale(vehicleDef), texProportions, texCoords, angle, material);
		}

		public static void DrawVehicleFitted(Rect rect, float angle, Texture2D texture, Material material)
		{
			Widgets.DrawTextureFitted(rect, texture, 1, new Vector2(texture.width, texture.height), new Rect(0f, 0f, 1f, 1f), angle, material);
		}

		/// <summary>
		/// Render lines from <paramref name="cannonPos"/> given angle and ranges
		/// </summary>
		/// <param name="cannonPos"></param>
		/// <param name="restrictedAngle"></param>
		/// <param name="minRange"></param>
		/// <param name="maxRange"></param>
		/// <param name="theta"></param>
		/// <param name="additionalAngle"></param>
		public static void DrawAngleLines(Vector3 cannonPos, Vector2 restrictedAngle, float minRange, float maxRange, float theta, float additionalAngle = 0f)
		{
			Vector3 minTargetPos1 = cannonPos.PointFromAngle(minRange, restrictedAngle.x + additionalAngle);
			Vector3 minTargetPos2 = cannonPos.PointFromAngle(minRange, restrictedAngle.y + additionalAngle);

			Vector3 maxTargetPos1 = cannonPos.PointFromAngle(maxRange, restrictedAngle.x + additionalAngle);
			Vector3 maxTargetPos2 = cannonPos.PointFromAngle(maxRange, restrictedAngle.y + additionalAngle);

			GenDraw.DrawLineBetween(minTargetPos1, maxTargetPos1);
			GenDraw.DrawLineBetween(minTargetPos2, maxTargetPos2);
			if (minRange > 0)
			{
				GenDraw.DrawLineBetween(cannonPos, minTargetPos1, SimpleColor.Red);
				GenDraw.DrawLineBetween(cannonPos, minTargetPos2, SimpleColor.Red);
			}

			float angleStart = restrictedAngle.x;

			Vector3 lastPointMin = minTargetPos1;
			Vector3 lastPointMax = maxTargetPos1;

			for (int angle = 0; angle < theta + 1; angle++)
			{
				Vector3 targetPointMax = cannonPos.PointFromAngle(maxRange, angleStart + angle + additionalAngle);
				GenDraw.DrawLineBetween(lastPointMax, targetPointMax);
				lastPointMax = targetPointMax;

				if (minRange > 0)
				{
					Vector3 targetPointMin = cannonPos.PointFromAngle(minRange, angleStart + angle + additionalAngle);
					GenDraw.DrawLineBetween(lastPointMin, targetPointMin, SimpleColor.Red);
					lastPointMin = targetPointMin;
				}
			}
		}
	}
}
