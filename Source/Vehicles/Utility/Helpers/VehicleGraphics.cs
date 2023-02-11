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
			Pair<float, float> offset = VehicleDrawOffset(rot, graphicData.drawOffset.x, graphicData.drawOffset.y);
			return new Vector3(offset.First, graphicData.drawOffset.y, offset.Second);
		}

		/// <summary>
		/// Calculate VehicleTurret draw offset
		/// </summary>
		/// <param name="rot"></param>
		/// <param name="renderProps"></param>
		/// <param name="turretRotation"></param>
		/// <param name="attachedTo"></param>
		public static Vector2 TurretDrawOffset(Rot8 rot, VehicleTurretRender renderProps, float extraRotation = 0, VehicleTurret attachedTo = null)
		{
			VehicleTurretRender.RotationalOffset turretOffset = renderProps.OffsetFor(rot);
			if (attachedTo != null)
			{
				VehicleTurretRender.RotationalOffset parentOffset = attachedTo.renderProperties.OffsetFor(rot);
				Pair<float, float> rootLoc = Ext_Math.RotatePointClockwise(turretOffset.Offset.x, turretOffset.Offset.y, extraRotation);
				return new Vector2(rootLoc.First + parentOffset.Offset.x, rootLoc.Second + parentOffset.Offset.y);
			}
			return turretOffset.Offset;
		}

		/// <summary>
		/// Calculate draw offset given offsets from center rotated alongside <paramref name="rot"/>
		/// </summary>
		/// <param name="rot"></param>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		/// <param name="turretRotation"></param>
		/// <param name="attachedTo"></param>
		public static Pair<float, float> VehicleDrawOffset(Rot8 rot, float offsetX, float offsetY, float additionalRotation = 0)
		{
			return Ext_Math.RotatePointClockwise(offsetX, offsetY, rot.AsAngle + additionalRotation);
		}

		/// <summary>
		/// Draw VehicleTurret on vehicle
		/// </summary>
		/// <param name="turret"></param>
		public static void DrawTurret(VehicleTurret turret, Rot8 rot)
		{
			try
			{
				Vector3 topVectorLocation = turret.TurretLocation;
				if (turret.rTracker.Recoil > 0f)
				{
					topVectorLocation = Ext_Math.PointFromAngle(topVectorLocation, turret.rTracker.Recoil, turret.rTracker.Angle);
				}
				if (turret.attachedTo != null && turret.attachedTo.rTracker.Recoil > 0f)
				{
					topVectorLocation = Ext_Math.PointFromAngle(topVectorLocation, turret.attachedTo.rTracker.Recoil, turret.attachedTo.rTracker.Angle);
				}
				Mesh cannonMesh = turret.CannonGraphic.MeshAt(rot);
				Graphics.DrawMesh(cannonMesh, topVectorLocation, turret.TurretRotation.ToQuat(), turret.CannonMaterial, 0);
			}
			catch (Exception ex)
			{
				Log.Error(string.Format("Error occurred during rendering of attached thing on {0}. Exception: {1}", turret.vehicle.Label, ex.Message));
			}
		}

		public static string DrawVehicle(Rect rect, VehiclePawn vehicle, Rot8? rot = null)
		{
			return DrawVehicleDef(rect, vehicle.VehicleDef, patternData: vehicle.patternData, rot: rot);
		}

		/// <summary>
		/// Draw <paramref name="vehicleDef"/>
		/// </summary>
		/// <remarks><paramref name="material"/> may overwrite material used for vehicle</remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="material"></param>
		public static string DrawVehicleDef(Rect rect, VehicleDef vehicleDef, Material material = null, PatternData patternData = null, Rot8? rot = null)
		{
			string drawStep = string.Empty;
			try
			{
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
				Graphic_Vehicle graphic = VehicleTex.CachedGraphics[vehicleDef];

				PatternDef pattern = patternData?.patternDef;
				pattern ??= VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData)?.patternDef ?? PatternDefOf.Default;

				drawStep = "Setting default color";
				Color color1 = patternData?.color ?? vehicleDef.graphicData.color;
				Color color2 = patternData?.colorTwo ?? vehicleDef.graphicData.color;
				Color color3 = patternData?.colorThree ?? vehicleDef.graphicData.color;

				float tiling = patternData?.tiles ?? vehicleDef.graphicData.tiles;
				Vector2 displacement = patternData?.displacement ?? vehicleDef.graphicData.displacement;

				Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);
				if (material is null && pattern != null && (graphic.Shader.SupportsRGBMaskTex() || graphic.Shader.SupportsMaskTex()))
				{
					drawStep = $"Regenerating material for pattern={pattern.defName}";

					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = mainTex,
						shader = pattern is SkinDef ? RGBShaderTypeDefOf.CutoutComplexSkin.Shader : vehicleDef.graphic.Shader,
						color = color1,
						colorTwo = color2,
						colorThree = color3,
						tiles = tiling,
						displacement = displacement,
						properties = pattern.properties,
						maskTex = (vehicleDef.graphic as Graphic_Vehicle).masks[rotDrawn.AsInt],
						patternTex = pattern?[rotDrawn]
					};
					material = MaterialPoolExpanded.MatFrom(matReq);
				}
				drawStep = "Attempting to retrieve turret overlays";
				List<ValueTuple<Rect, Texture, Material, float, float>> overlays = new List<(Rect, Texture, Material, float, float)>();
				if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
				{
					overlays.AddRange(RetrieveTurretSettingsDrawProperties(rect, vehicleDef, rotDrawn, props.turrets.OrderBy(x => x.drawLayer),
						new PatternData(color1, color2, color3, pattern, displacement, tiling)));
				}
				drawStep = "Retrieving graphic overlays";
				//overlays.AddRange(RetrieveOverlaySettingsDrawProperties(rect, vehicleDef, rotDrawn));

				drawStep = "Rendering overlays with layer < 0";
				foreach (var overlay in overlays.Where(o => o.Item4 < 0).OrderBy(o => o.Item4))
				{
					UIElements.DrawTextureWithMaterialOnGUI(overlay.Item1, overlay.Item2, overlay.Item3, overlay.Item5);
				}

				drawStep = "Rendering main texture";
				DrawVehicleFitted(adjustedRect, angle, mainTex, material);

				drawStep = "Rendering overlays with layer >= 0";
				foreach (var overlay in overlays.Where(o => o.Item4 >= 0).OrderBy(o => o.Item4))
				{
					UIElements.DrawTextureWithMaterialOnGUI(overlay.Item1, overlay.Item2, overlay.Item3, overlay.Item5);
				}
				return string.Empty;
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to draw <type>VehicleDef</type>=\"{vehicleDef?.defName ?? "Null"}\" Exception={ex.Message}");
			}
			return drawStep;
		}

		/// <summary>
		/// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="displayRect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="cannons"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		public static IEnumerable<(Rect, Texture, Material, float, float)> RetrieveTurretSettingsDrawProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (turret.NoGraphic)
				{
					continue;
				}
				Rect turretRect = TurretRect(rect, vehicleDef, turret, rot);
				Material cannonMat = turret.CannonGraphic.Shader.SupportsRGBMaskTex() ? new Material(turret.CannonGraphic.MatAt(patternData.patternDef)) : null;
				if ((turret.CannonGraphic.Shader.SupportsRGBMaskTex() || turret.CannonGraphic.Shader.SupportsMaskTex()) && patternData != VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData))
				{
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = turret.CannonTexture,
						shader = patternData?.patternDef is SkinDef ? RGBShaderTypeDefOf.CutoutComplexSkin.Shader : turret.CannonGraphic.Shader,
						color = patternData.color,
						colorTwo = patternData.colorTwo,
						colorThree = patternData.colorThree,
						tiles = patternData.tiles,
						displacement = patternData.displacement,
						properties = patternData.patternDef.properties,
						maskTex = turret.CannonGraphic.masks[Rot8.North.AsInt],
						patternTex = patternData.patternDef?[Rot8.North]
					};
					cannonMat = MaterialPoolExpanded.MatFrom(matReq);
				}
				yield return (turretRect, turret.CannonTexture, cannonMat, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
			}
		}

		/// <summary>
		/// Retrieves VehicleTurret Rect adjusted to <paramref name="rect"/> of where it's being rendered.
		/// </summary>
		/// <remarks>Scales up / down relative to drawSize of <paramref name="vehicleDef"/></remarks>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="turret"></param>
		/// <param name="rot"></param>
		internal static Rect TurretRect(Rect rect, VehicleDef vehicleDef, VehicleTurret turret, Rot8 rot)
		{
			//Ensure CannonGraphics are up to date (only required upon changes to default pattern from mod settings)
			turret.ResolveCannonGraphics(vehicleDef);
			//Scale to VehicleDef drawSize
			Vector2 rectSize = vehicleDef.ScaleDrawRatio(turret.turretDef.graphicData, rect.size);
			//Adjust position from new rect size
			Vector2 adjustedPosition = rect.position + (rect.size - rectSize) / 2f;
			Vector2 turretUIPos = turret.ScaleUIRect(vehicleDef, adjustedPosition, rectSize, rot);
			return new Rect(turretUIPos, rectSize);
		}

		/// <summary>
		/// Retrieve GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="rot"></param>
		public static IEnumerable<ValueTuple<Rect, Texture, Material, float, float>> RetrieveOverlaySettingsDrawProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, List<GraphicOverlay> graphicOverlays = null)
		{
			var overlays = graphicOverlays ?? vehicleDef.drawProperties.OverlayGraphics;
			foreach (GraphicOverlay graphicOverlay in overlays)
			{
				Vector2 rectSize = vehicleDef.ScaleDrawRatio(graphicOverlay.graphic.data, rect.size);
				Vector2 adjustedPosition = rect.position + (rect.size - rectSize) / 2f;
				Vector3 offsets = graphicOverlay.graphic.DrawOffset(rot);
				Vector2 finalPosition = new Vector2(offsets.x, offsets.z) + adjustedPosition;
				Rect overlayRect = new Rect(finalPosition, rectSize);
				Texture2D texture = ContentFinder<Texture2D>.Get(graphicOverlay.graphic.data.texPath);
				Material material = graphicOverlay.graphic.Shader.SupportsMaskTex() ? graphicOverlay.graphic.MatAt(rot) : null;
				yield return new ValueTuple<Rect, Texture, Material, float, float>(overlayRect, texture, material, graphicOverlay.graphic.data.DrawOffsetFull(rot).y, graphicOverlay.rotation);
			}
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
