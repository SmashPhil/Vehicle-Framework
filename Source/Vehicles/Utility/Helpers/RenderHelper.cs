using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class RenderHelper
	{
		private static readonly List<int> cachedEdgeTiles = new List<int>();
		private static int cachedEdgeTilesForCenter = -1;
		private static int cachedEdgeTilesForRadius = -1;
		private static int cachedEdgeTilesForWorldSeed = -1;

		public static bool draggingCP;
		public static bool draggingHue;
		public static bool draggingDisplacement;

		public static Texture2D ColorChart = new Texture2D(255, 255);
		public static Texture2D HueChart = new Texture2D(1, 255);

		public static Color Blackist = new Color(0.06f, 0.06f, 0.06f);
		public static Color Greyist = new Color(0.2f, 0.2f, 0.2f);

		static RenderHelper()
		{
			for (int i = 0; i < 255; i++)
			{
				HueChart.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0f, 255f, i), 1f, 1f));
			}
			HueChart.Apply(false);
			for (int j = 0; j < 255; j++)
			{
				for (int k = 0; k < 255; k++)
				{
					Color color = Color.clear;
					Color c = Color.Lerp(color, Color.white, Mathf.InverseLerp(0f, 255f, j));
					color = Color32.Lerp(Color.black, c, Mathf.InverseLerp(0f, 255f, k));
					ColorChart.SetPixel(j, k, color);
				}
			}
			ColorChart.Apply(false);
		}

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
		public static Pair<float,float> TurretDrawOffset(Rot8 rot, VehicleTurretRender renderProps, float extraRotation = 0, VehicleTurret attachedTo = null)
		{
			var turretOffset = renderProps.OffsetFor(rot);
			if (attachedTo != null)
			{
				var parentOffset = attachedTo.renderProperties.OffsetFor(rot);
				Pair<float, float> rootLoc = Ext_Math.RotatePointClockwise(turretOffset.Offset.x, turretOffset.Offset.y, extraRotation);
				return new Pair<float, float>(rootLoc.First + parentOffset.Offset.x, rootLoc.Second + parentOffset.Offset.y);
			}
			return new Pair<float, float>(turretOffset.Offset.x, turretOffset.Offset.y);
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
		public static void DrawAttachedThing(VehicleTurret turret)
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
				Mesh cannonMesh = turret.CannonGraphic.MeshAt(Rot4.North);
				Graphics.DrawMesh(cannonMesh, topVectorLocation, turret.TurretRotation.ToQuat(), turret.CannonMaterial, 0);
			}
			catch(Exception ex)
			{
				Log.Error(string.Format("Error occurred during rendering of attached thing on {0}. Exception: {1}", turret.vehicle.Label, ex.Message));
			}
		}

		/// <summary>
		/// Draw cannon textures on GUI given collection of cannons and vehicle GUI is being drawn for
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="displayRect"></param>
		/// <param name="cannons"></param>
		/// <param name="vehicleMaskName"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		/// <remarks>Might want to research into optimization practices for rendering</remarks>
		public static void DrawCannonTextures(this VehiclePawn vehicle, Rect displayRect, IEnumerable<VehicleTurret> cannons, PatternDef pattern, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null, Rot8? rot = null)
		{
			Rot8 rotDrawn = rot ?? vehicle.VehicleDef.drawProperties.displayRotation;
			foreach (VehicleTurret turret in cannons)
			{
				if (turret.NoGraphic)
				{
					continue;
				}
				GraphicDataRGB graphicData = vehicle.VehicleDef.graphicData;
				if (resolveGraphics)
				{
					turret.ResolveCannonGraphics(vehicle);
				}

				float cannonWidth = (displayRect.width / graphicData.drawSize.x) * turret.CannonGraphicData.drawSize.x;
				float cannonHeight = (displayRect.height / graphicData.drawSize.y) * turret.CannonGraphicData.drawSize.y;

				Vector3 offset = turret.DefaultOffsetLocFor(rotDrawn);
				Vector2 rectSize = vehicle.VehicleDef.ScaleDrawRatio(new Vector2(displayRect.width, displayRect.height));
				float newX = (displayRect.width / 2) - (rectSize.x / 2) + (vehicle.VehicleDef.drawProperties.displayOffset.x * rectSize.x / displayRect.width);
				float newY = (displayRect.height / 2) - (rectSize.y / 2) + (vehicle.VehicleDef.drawProperties.displayOffset.y * rectSize.y / displayRect.height);
				Rect adjustedRect = new Rect(displayRect.x + newX, displayRect.y + newY, rectSize.x, rectSize.y);
				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = (adjustedRect.x + (adjustedRect.width / 2) - (cannonWidth / 2)) + (rectSize.x / graphicData.drawSize.x * offset.x);
				float yCannon = (adjustedRect.y + (adjustedRect.height / 2) - (cannonHeight / 2)) - (rectSize.y / graphicData.drawSize.y * offset.z);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
				Material cannonMat = null;
				
				if (turret.CannonGraphic.Shader.SupportsRGBMaskTex())
				{
					cannonMat = new Material(turret.CannonGraphic.MatAt(Rot4.North, vehicle));
					if ((manualColorOne != null || manualColorTwo != null || manualColorThree != null) && turret.CannonGraphic.GetType().IsAssignableFrom(typeof(Graphic_Turret)))
					{
						MaterialRequestRGB matReq = new MaterialRequestRGB()
						{
							mainTex = turret.CannonTexture,
							shader = turret.CannonGraphic.Shader,
							color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
							colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
							colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
							tiles = vehicle.Tiles,
							properties = pattern.properties,
							isSkin = pattern is SkinDef,
							maskTex = turret.CannonGraphic.masks[0],
							patternTex = pattern[rotDrawn]
						};
						cannonMat = MaterialPoolExpanded.MatFrom(matReq);
					}
				}
				GenUI.DrawTextureWithMaterial(cannonDrawnRect, turret.CannonTexture, cannonMat);

				if (VehicleMod.settings.debug.debugDrawCannonGrid)
				{
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
					Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
					Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
				}
			}
		}

		/// <summary>
		/// Draw Vehicle texture with option to manually apply colors to Material
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleTex"></param>
		/// <param name="vehicle"></param>
		/// <param name="vehicleMaskName"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="manualColorOne"></param>
		/// <param name="manualColorTwo"></param>
		public static void DrawVehicle(Rect rect, VehiclePawn vehicle, PatternDef pattern = null, bool resolveGraphics = false, Color? manualColorOne = null, Color? manualColorTwo = null, Color? manualColorThree = null, Rot8? rot = null)
		{
			Rot8 rotDrawn = rot ?? vehicle.VehicleDef.drawProperties.displayRotation;
			Texture2D mainTex = vehicle.VehicleGraphic.TexAt(rotDrawn);
			Material mat = vehicle.VehicleGraphic.MatAt(rotDrawn, pattern, vehicle);
			if (vehicle.VehicleGraphic.Shader.SupportsRGBMaskTex())
			{
				mat = new Material(vehicle.VehicleGraphic.MatAt(rotDrawn, vehicle));
				if (manualColorOne != null || manualColorTwo != null || manualColorThree != null)
				{
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = mainTex,
						shader = vehicle.VehicleGraphic.Shader,
						color = manualColorOne != null ? manualColorOne.Value : vehicle.DrawColor,
						colorTwo = manualColorTwo != null ? manualColorTwo.Value : vehicle.DrawColorTwo,
						colorThree = manualColorThree != null ? manualColorThree.Value : vehicle.DrawColorThree,
						tiles = vehicle.Tiles,
						properties = pattern.properties,
						isSkin = pattern is SkinDef,
						maskTex = vehicle.VehicleGraphic.masks[rotDrawn.AsInt],
						patternTex = pattern?[rotDrawn]
					};
					mat = MaterialPoolExpanded.MatFrom(matReq);
				}
			}

			GenUI.DrawTextureWithMaterial(rect, mainTex, mat);

			if (vehicle.CompCannons != null)
			{
				vehicle.DrawCannonTextures(rect, vehicle.CompCannons.Cannons.Where(t => !t.isUpgrade).OrderBy(x => x.drawLayer), pattern, resolveGraphics, manualColorOne, manualColorTwo, manualColorThree, rotDrawn);
			}
		}

		/// <summary>
		/// Draw Vehicle texture dynamically allowing for tiling and rescaling of patterns
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="rect"></param>
		/// <param name="vehicleTex"></param>
		/// <param name="vehicleGraphic"></param>
		/// <param name="patternData"></param>
		/// <param name="resolveGraphics"></param>
		/// <param name="turrets"></param>
		public static void DrawVehicleTexTiled(this VehicleDef vehicleDef, Rect rect, PatternData patternData, Rot8? rot = null, List<VehicleTurret> turrets = null, List<GraphicOverlay> graphicOverlays = null)
		{
			Vector2 rectSize = vehicleDef.ScaleDrawRatio(new Vector2(rect.width * 0.95f, rect.height * 0.95f));
			float newX = (rect.width / 2) - (rectSize.x / 2) + (vehicleDef.drawProperties.displayOffset.x * rectSize.x / rect.width);
			float newY = (rect.height / 2) - (rectSize.y / 2) + (vehicleDef.drawProperties.displayOffset.y * rectSize.y / rect.height);
			Rect adjustedRect = new Rect(rect.x + newX, rect.y + newY, rectSize.x, rectSize.y);

			Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;

			Graphic_Vehicle graphic = VehicleTex.CachedGraphics[vehicleDef];

			PatternDef pattern = patternData?.pattern;
			pattern ??= VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData)?.pattern ?? PatternDefOf.Default;

			Color color1 = patternData?.color ?? vehicleDef.graphicData.color;
			Color color2 = patternData?.colorTwo ?? vehicleDef.graphicData.color;
			Color color3 = patternData?.colorThree ?? vehicleDef.graphicData.color;

			float tiling = patternData?.tiles ?? vehicleDef.graphicData.tiles;
			Vector2 displacement = patternData?.displacement ?? vehicleDef.graphicData.displacement;

			Material material = null;
			if (pattern != null && graphic.Shader.SupportsRGBMaskTex())
			{
				MaterialRequestRGB matReq = new MaterialRequestRGB()
				{
					mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn),
					shader = vehicleDef.graphic.Shader,
					color = color1,
					colorTwo = color2,
					colorThree = color3,
					tiles = tiling,
					displacement = displacement,
					properties = pattern.properties,
					isSkin = pattern is SkinDef,
					maskTex = (vehicleDef.graphic as Graphic_Vehicle).masks[rotDrawn.AsInt],
					patternTex = pattern?[rotDrawn]
				};
				material = MaterialPoolExpanded.MatFrom(matReq);
			}

			var drawOverlays = new List<ValueTuple<Rect, Texture, Material, float, float>>();
			drawOverlays.AddRange(RetrieveOverlaySettingsDrawProperties(adjustedRect, vehicleDef, rotDrawn, graphicOverlays));
			drawOverlays.AddRange(RetrieveTurretSettingsDrawProperties(adjustedRect, vehicleDef, turrets, patternData, rotDrawn));

			foreach (var overlay in drawOverlays.Where(o => o.Item4 < 0).OrderBy(o => o.Item4))
			{
				UIElements.DrawTextureWithMaterialOnGUI(overlay.Item1, overlay.Item2, overlay.Item3, overlay.Item5);
			}

			GenUI.DrawTextureWithMaterial(adjustedRect, VehicleTex.VehicleTexture(vehicleDef, rotDrawn), material);

			foreach (var overlay in drawOverlays.Where(o => o.Item4 >= 0).OrderBy(o => o.Item4))
			{
				UIElements.DrawTextureWithMaterialOnGUI(overlay.Item1, overlay.Item2, overlay.Item3, overlay.Item5);
			}
		}

		/// <summary>
		/// Draw cannon textures on GUI given collection of cannons and vehicle GUI is being drawn for with additional tiling
		/// </summary>
		/// <remarks>Might possibly want to throw into separate threads</remarks>
		/// <param name="vehicleDef"></param>
		/// <param name="displayRect"></param>
		/// <param name="cannons"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		/// <param name="resolveGraphics"></param>
		public static void DrawTurretsTexturesTiled(this VehicleDef vehicleDef, Rect displayRect, IEnumerable<VehicleTurret> cannons, PatternData patternData, Rot8? rot = null, bool resolveGraphics = false)
		{
			foreach (VehicleTurret turret in cannons)
			{
				if (turret.NoGraphic)
				{
					continue;
				}
				GraphicDataRGB graphicData = vehicleDef.graphicData;
				if (resolveGraphics)
				{
					turret.ResolveCannonGraphics(patternData);
				}
				Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;
				float cannonWidth = (displayRect.width / graphicData.drawSize.x) * turret.CannonGraphicData.drawSize.x;
				float cannonHeight = (displayRect.height / graphicData.drawSize.y) * turret.CannonGraphicData.drawSize.y;

				Vector3 offset = turret.DefaultOffsetLocFor(rotDrawn);
				Vector2 rectSize = vehicleDef.ScaleDrawRatio(new Vector2(displayRect.width, displayRect.height));
				float newX = (displayRect.width / 2) - (rectSize.x / 2) + (vehicleDef.drawProperties.displayOffset.x * rectSize.x / displayRect.width);
				float newY = (displayRect.height / 2) - (rectSize.y / 2) + (vehicleDef.drawProperties.displayOffset.y * rectSize.y / displayRect.height);
				Rect adjustedRect = new Rect(displayRect.x + newX, displayRect.y + newY, rectSize.x, rectSize.y);
				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = (adjustedRect.x + (adjustedRect.width / 2) - (cannonWidth / 2)) + (rectSize.x / graphicData.drawSize.x * offset.x);
				float yCannon = (adjustedRect.y + (adjustedRect.height / 2) - (cannonHeight / 2)) - (rectSize.y / graphicData.drawSize.y * offset.z);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
				Material cannonMat = null;
				if (turret.CannonGraphic.Shader.SupportsRGBMaskTex())
				{
					cannonMat = new Material(turret.CannonGraphic.MatAt(rotDrawn, patternData.pattern));
					if (turret.CannonGraphic.GetType().IsAssignableFrom(typeof(Graphic_Turret)))
					{
						MaterialRequestRGB matReq = new MaterialRequestRGB()
						{
							mainTex = turret.CannonTexture,
							shader = turret.CannonGraphic.Shader,
							color = patternData.color,
							colorTwo = patternData.colorTwo,
							colorThree = patternData.colorThree,
							tiles = patternData.tiles,
							displacement = patternData.displacement,
							properties = patternData.pattern.properties,
							isSkin = patternData.pattern is SkinDef,
							maskTex = turret.CannonGraphic.masks[0],
							patternTex = patternData.pattern[rotDrawn]
						};
						cannonMat = MaterialPoolExpanded.MatFrom(matReq);
					}
				}

				GenUI.DrawTextureWithMaterial(cannonDrawnRect, turret.CannonTexture, cannonMat);

				if (VehicleMod.settings.debug.debugDrawCannonGrid)
				{
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
					Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
					Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
					Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
				}
			}
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
				Vector2 overlayDrawSize = graphicOverlay.graphic.data.drawSize;
				GraphicDataRGB vehicleGraphicData = vehicleDef.graphicData;

				float overlayWidth = rect.width / vehicleGraphicData.drawSize.x * overlayDrawSize.x;
				float overlayHeight = rect.height / vehicleGraphicData.drawSize.y * overlayDrawSize.y;

				float xOverlay = rect.x + (rect.width / 2) - (overlayWidth / 2) + (vehicleGraphicData.drawSize.x * graphicOverlay.graphic.DrawOffsetFull(rot).x);
				float yOverlay = rect.y + (rect.height / 2) - (overlayHeight / 2) - (vehicleGraphicData.drawSize.x * graphicOverlay.graphic.DrawOffsetFull(rot).y);

				Rect overlayRect = new Rect(xOverlay, yOverlay, overlayWidth, overlayHeight);
				Texture2D texture = TextureCache.Get(graphicOverlay.graphic.data.texPath);
				Material material = graphicOverlay.graphic.Shader.SupportsMaskTex() ? graphicOverlay.graphic.MatAt(rot) : null;
				yield return new ValueTuple<Rect, Texture, Material, float, float>(overlayRect, texture, material, graphicOverlay.graphic.data.DrawOffsetFull(rot).y, graphicOverlay.rotation);
			}
		}

		/// <summary>
		/// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="displayRect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="cannons"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		public static IEnumerable<ValueTuple<Rect, Texture, Material, float, float>> RetrieveTurretSettingsDrawProperties(Rect displayRect, VehicleDef vehicleDef, IEnumerable<VehicleTurret> cannons, PatternData patternData, Rot8? rot = null)
		{
			foreach (VehicleTurret turret in cannons)
			{
				if (turret.NoGraphic)
				{
					continue;
				}

				GraphicDataRGB vehicleGraphicData = vehicleDef.graphicData;
				Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;
				turret.ResolveCannonGraphics(vehicleDef);

				float cannonWidth = displayRect.width / vehicleGraphicData.drawSize.x * turret.CannonGraphicData.drawSize.x * vehicleDef.drawProperties.displaySizeMultiplier;
				float cannonHeight = displayRect.height / vehicleGraphicData.drawSize.y * turret.CannonGraphicData.drawSize.y * vehicleDef.drawProperties.displaySizeMultiplier;

				var offset = turret.renderProperties.OffsetFor(rotDrawn);
				/// ( center point of vehicle) + (UI size / drawSize) * cannonPos
				/// y axis inverted as UI goes top to bottom, but DrawPos goes bottom to top
				float xCannon = displayRect.x + (vehicleDef.drawProperties.displaySizeMultiplier / displayRect.width * vehicleDef.drawProperties.displayOffset.x) + (displayRect.width / 2) - (cannonWidth / 2) + (vehicleDef.drawProperties.displaySizeMultiplier / vehicleGraphicData.drawSize.x * offset.Offset.x);
				float yCannon = displayRect.y + (vehicleDef.drawProperties.displaySizeMultiplier / displayRect.height * vehicleDef.drawProperties.displayOffset.y) + (displayRect.height / 2) - (cannonHeight / 2) - (vehicleDef.drawProperties.displaySizeMultiplier / vehicleGraphicData.drawSize.y * offset.Offset.y);

				Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);

				Material cannonMat = turret.CannonGraphic.Shader.SupportsRGBMaskTex() ? new Material(turret.CannonGraphic.MatAt(patternData.pattern)) : null;
				if (patternData != VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData))
				{
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = turret.CannonTexture,
						shader = turret.CannonGraphic.Shader,
						color = patternData.color,
						colorTwo = patternData.colorTwo,
						colorThree = patternData.colorThree,
						tiles = patternData.tiles,
						displacement = patternData.displacement,
						properties = patternData.pattern.properties,
						isSkin = patternData.pattern is SkinDef,
						maskTex = turret.CannonGraphic.masks[0],
						patternTex = patternData.pattern?[rotDrawn]
					};
					cannonMat = MaterialPoolExpanded.MatFrom(matReq);
				}
				yield return new ValueTuple<Rect, Texture, Material, float, float>(cannonDrawnRect, turret.CannonTexture, cannonMat, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated);
			}
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
				Vector2 rectSize = vehicleDef.ScaleDrawRatio(new Vector2(rect.width * 0.95f, rect.height * 0.95f));
				float newX = (rect.width / 2) - (rectSize.x / 2) + (vehicleDef.drawProperties.displayOffset.x * rect.width);
				float newY = (rect.height / 2) - (rectSize.y / 2) + (vehicleDef.drawProperties.displayOffset.y * rect.height);
				Rect adjustedRect = new Rect(rect.x + newX, rect.y + newY, rectSize.x, rectSize.y);
				Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;

				drawStep = "Retrieving cached graphic and pattern";
				Graphic_Vehicle graphic = VehicleTex.CachedGraphics[vehicleDef];

				PatternDef pattern = patternData?.pattern;
				pattern ??= VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData)?.pattern ?? PatternDefOf.Default;

				drawStep = "Setting default color";
				Color color1 = patternData?.color ?? vehicleDef.graphicData.color;
				Color color2 = patternData?.colorTwo ?? vehicleDef.graphicData.color;
				Color color3 = patternData?.colorThree ?? vehicleDef.graphicData.color;

				float tiling = patternData?.tiles ?? vehicleDef.graphicData.tiles;
				Vector2 displacement = patternData?.displacement ?? vehicleDef.graphicData.displacement;

				if (material is null && pattern != null && graphic.Shader.SupportsRGBMaskTex())
				{
					drawStep = $"Regenerating material for pattern={pattern.defName}";
					MaterialRequestRGB matReq = new MaterialRequestRGB()
					{
						mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn),
						shader = vehicleDef.graphic.Shader,
						color = color1,
						colorTwo = color2,
						colorThree = color3,
						tiles = tiling,
						displacement = displacement,
						properties = pattern.properties,
						isSkin = pattern is SkinDef,
						maskTex = (vehicleDef.graphic as Graphic_Vehicle).masks[rotDrawn.AsInt],
						patternTex = pattern?[rotDrawn]
					};
					material = MaterialPoolExpanded.MatFrom(matReq);
				}
				drawStep = "Attempting to retrieve turret overlays";
				List<ValueTuple<Rect, Texture, Material, float, float>> overlays = new List<(Rect, Texture, Material, float, float)>();
				if (vehicleDef.GetSortedCompProperties<CompProperties_Cannons>() is CompProperties_Cannons props)
				{
					overlays.AddRange(RetrieveTurretSettingsDrawProperties(adjustedRect, vehicleDef, props.turrets.OrderBy(x => x.drawLayer),
						new PatternData(color1, color2, color3, pattern, displacement, tiling), rotDrawn));
				}
				drawStep = "Retrieving graphic overlays";
				overlays.AddRange(RetrieveOverlaySettingsDrawProperties(adjustedRect, vehicleDef, rotDrawn));

				drawStep = "Rendering overlays with layer < 0";
				foreach (var overlay in overlays.Where(o => o.Item4 < 0).OrderBy(o => o.Item4))
				{
					UIElements.DrawTextureWithMaterialOnGUI(overlay.Item1, overlay.Item2, overlay.Item3, overlay.Item5);
				}
				drawStep = "Rendering main texture";
				GenUI.DrawTextureWithMaterial(adjustedRect, VehicleTex.VehicleTexture(vehicleDef, rotDrawn), material);
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

		public static void DrawLinesBetweenTargets(VehiclePawn pawn, Job curJob, JobQueue jobQueue)
		{
			Vector3 a = pawn.Position.ToVector3Shifted();
			if (pawn.vPather.curPath != null)
			{
				a = pawn.vPather.Destination.CenterVector3;
			}
			else if (curJob != null && curJob.targetA.IsValid && (!curJob.targetA.HasThing || (curJob.targetA.Thing.Spawned && curJob.targetA.Thing.Map == pawn.Map)))
			{
				GenDraw.DrawLineBetween(a, curJob.targetA.CenterVector3, AltitudeLayer.Item.AltitudeFor());
				a = curJob.targetA.CenterVector3;
			}
			for (int i = 0; i < jobQueue.Count; i++)
			{
				if (jobQueue[i].job.targetA.IsValid)
				{
					if (!jobQueue[i].job.targetA.HasThing || (jobQueue[i].job.targetA.Thing.Spawned && jobQueue[i].job.targetA.Thing.Map == pawn.Map))
					{
						Vector3 centerVector = jobQueue[i].job.targetA.CenterVector3;
						GenDraw.DrawLineBetween(a, centerVector, AltitudeLayer.Item.AltitudeFor());
						a = centerVector;
					}
				}
				else
				{
					List<LocalTargetInfo> targetQueueA = jobQueue[i].job.targetQueueA;
					if (targetQueueA != null)
					{
						for (int j = 0; j < targetQueueA.Count; j++)
						{
							if (!targetQueueA[j].HasThing || (targetQueueA[j].Thing.Spawned && targetQueueA[j].Thing.Map == pawn.Map))
							{
								Vector3 centerVector2 = targetQueueA[j].CenterVector3;
								GenDraw.DrawLineBetween(a, centerVector2, AltitudeLayer.Item.AltitudeFor());
								a = centerVector2;
							}
						}
					}
				}
			}
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
		
		/// <summary>
		/// Allow for optional overriding of mote saturation on map while being able to throw any MoteThrown <paramref name="mote"/>
		/// </summary>
		/// <seealso cref="MoteThrown"/>
		/// <param name="loc"></param>
		/// <param name="map"></param>
		/// <param name="mote"></param>
		/// <param name="overrideSaturation"></param>
		public static Mote ThrowMoteEnhanced(Vector3 loc, Map map, MoteThrown mote, bool overrideSaturation = false)
		{
			if(!loc.ShouldSpawnMotesAt(map) || (overrideSaturation && map.moteCounter.Saturated))
			{
				return null;
			}

			GenSpawn.Spawn(mote, loc.ToIntVec3(), map, WipeMode.Vanish);
			return mote;
		}

		/// <summary>
		/// Draw ColorPicker and HuePicker
		/// </summary>
		/// <param name="fullRect"></param>
		public static Rect DrawColorPicker(Rect fullRect, ref float hue, ref float saturation, ref float value, Action<float, float, float> colorSetter)
		{
			Rect rect = fullRect.ContractedBy(10f);
			rect.width = 15f;
			if (fullRect.width != fullRect.height + 25)
			{
				SmashLog.WarningOnce($"ColorPicker fullRect dimensions are not correct. Width should be exactly 25 larger than the height.", "VehiclesColorPicker".GetHashCode());
			}
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingHue)
			{
				draggingHue = true;
			}
			if (draggingHue && Event.current.isMouse)
			{
				float num = hue;
				hue = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y);
				if (hue != num)
				{
					colorSetter(hue, saturation, value);
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				draggingHue = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawTexturePart(rect, new Rect(0f, 0f, 1f, 1f), HueChart);
			Rect rect2 = new Rect(0f, 0f, 16f, 16f)
			{
				center = new Vector2(rect.center.x, rect.height * (1f - hue) + rect.y).Rounded()
			};

			Widgets.DrawTextureRotated(rect2, VehicleTex.ColorHue, 0f);
			rect = fullRect.ContractedBy(10f);
			rect.x = rect.xMax - rect.height;
			rect.width = rect.height;
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingCP)
			{
				draggingCP = true;
			}
			if (draggingCP)
			{
				saturation = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x);
				value = Mathf.InverseLerp(rect.width, 0f, Event.current.mousePosition.y - rect.y);
				colorSetter(hue, saturation, value);
			}
			if (Input.GetMouseButtonUp(0))
			{
				draggingCP = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawBoxSolid(rect, Color.white);
			GUI.color = Color.HSVToRGB(hue, 1f, 1f);
			Widgets.DrawTextureFitted(rect, ColorChart, 1f);
			GUI.color = Color.white;
			GUI.BeginClip(rect);
			rect2.center = new Vector2(rect.width * saturation, rect.width * (1f - value));
			if (value >= 0.4f && (hue <= 0.5f || saturation <= 0.5f))
			{
				GUI.color = Blackist;
			}
			Widgets.DrawTextureFitted(rect2, VehicleTex.ColorPicker, 1f);
			GUI.color = Color.white;
			GUI.EndClip();
			return rect;
		}

		/// <summary>
		/// Draw <paramref name="buildDef"/> with proper vehicle material
		/// </summary>
		/// <param name="command"></param>
		/// <param name="rect"></param>
		/// <param name="buildDef"></param>
		public static GizmoResult GizmoOnGUIWithMaterial(Command command, Rect rect, VehicleBuildDef buildDef)
		{
			VehicleDef vehicleDef = buildDef.thingToSpawn;
			var font = Text.Font;
			Text.Font = GameFont.Tiny;
			bool flag = false;
			if (Mouse.IsOver(rect))
			{
				flag = true;
				if (!command.disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
			Material material = command.disabled ? TexUI.GrayscaleGUI : null;
			GenUI.DrawTextureWithMaterial(rect, command.BGTexture, material);

			PatternData defaultPatternData = new PatternData(VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData));
			if (command.disabled)
			{
				defaultPatternData.color = vehicleDef.graphicData.color.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
				defaultPatternData.colorTwo = vehicleDef.graphicData.colorTwo.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
				defaultPatternData.colorThree = vehicleDef.graphicData.colorThree.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
			}
			DrawVehicleDef(rect, vehicleDef, null, defaultPatternData);

			bool flag2 = false;
			KeyCode keyCode = (command.hotKey == null) ? KeyCode.None : command.hotKey.MainKey;
			if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
			{
				Vector2 vector = new Vector2(5f, 3f);
				Widgets.Label(new Rect(rect.x + vector.x, rect.y + vector.y, rect.width - 10f, 18f), keyCode.ToStringReadable());
				GizmoGridDrawer.drawnHotKeys.Add(keyCode);
				if (command.hotKey.KeyDownEvent)
				{
					flag2 = true;
					Event.current.Use();
				}
			}
			if (Widgets.ButtonInvisible(rect, true))
			{
				flag2 = true;
			}
			string topRightLabel = command.TopRightLabel;
			if (!topRightLabel.NullOrEmpty())
			{
				Vector2 vector2 = Text.CalcSize(topRightLabel);
				Rect position;
				Rect rectBase = position = new Rect(rect.xMax - vector2.x - 2f, rect.y + 3f, vector2.x, vector2.y);
				position.x -= 2f;
				position.width += 3f;
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperRight;
				GUI.DrawTexture(position, TexUI.GrayTextBG);
				Widgets.Label(rectBase, topRightLabel);
				Text.Anchor = TextAnchor.UpperLeft;
			}
			string labelCap = command.LabelCap;
			if (!labelCap.NullOrEmpty())
			{
				float num = Text.CalcHeight(labelCap, rect.width);
				Rect rect2 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
				GUI.DrawTexture(rect2, TexUI.GrayTextBG);
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(rect2, labelCap);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
			GUI.color = Color.white;
			if (Mouse.IsOver(rect) /*&& command.DoTooltip*/)
			{
				TipSignal tip = command.Desc;
				if (command.disabled && !command.disabledReason.NullOrEmpty())
				{
					tip.text += "\n\n" + "DisabledCommand".Translate() + ": " + command.disabledReason;
				}
				TooltipHandler.TipRegion(rect, tip);
			}
			if (!command.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
			{
				UIHighlighter.HighlightOpportunity(rect, command.HighlightTag);
			}
			Text.Font = GameFont.Small;
			try
			{
				if (flag2)
				{
					if (command.disabled)
					{
						if (!command.disabledReason.NullOrEmpty())
						{
							Messages.Message(command.disabledReason, MessageTypeDefOf.RejectInput, false);
						}
						return new GizmoResult(GizmoState.Mouseover, null);
					}
					GizmoResult result;
					if (Event.current.button == 1)
					{
						result = new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);
					}
					else
					{
						if (!TutorSystem.AllowAction(command.TutorTagSelect))
						{
							return new GizmoResult(GizmoState.Mouseover, null);
						}
						result = new GizmoResult(GizmoState.Interacted, Event.current);
						TutorSystem.Notify_Event(command.TutorTagSelect);
					}
					return result;
				}
				else
				{
					if (flag)
					{
						return new GizmoResult(GizmoState.Mouseover, null);
					}
					return new GizmoResult(GizmoState.Clear, null);
				}
			}
			finally
			{
				Text.Font = font;
			}
		}

		/// <summary>
		/// Create rotated Mesh where <paramref name="rot"/> [1:3] indicates number of 90 degree rotations
		/// </summary>
		/// <param name="size"></param>
		/// <param name="rot"></param>
		public static Mesh NewPlaneMesh(Vector2 size, int rot)
		{
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] triangles = new int[6];
			vertices[0] = new Vector3(-0.5f * size.x, 0f, -0.5f * size.y);
			vertices[1] = new Vector3(-0.5f * size.x, 0f, 0.5f * size.y);
			vertices[2] = new Vector3(0.5f * size.x, 0f, 0.5f * size.y);
			vertices[3] = new Vector3(0.5f * size.x, 0f, -0.5f * size.y);
			switch (rot)
			{
				case 1:
					uv[0] = new Vector2(1f, 0f);
					uv[1] = new Vector2(0f, 0f);
					uv[2] = new Vector2(0f, 1f);
					uv[3] = new Vector2(1f, 1f);
					break;
				case 2:
					uv[0] = new Vector2(1f, 1f);
					uv[1] = new Vector2(1f, 0f);
					uv[2] = new Vector2(0f, 0f);
					uv[3] = new Vector2(0f, 1f);
					break;
				case 3:
					uv[0] = new Vector2(0f, 1f);
					uv[1] = new Vector2(1f, 1f);
					uv[2] = new Vector2(1f, 0f);
					uv[3] = new Vector2(0f, 0f);
					break;
				default:
					uv[0] = new Vector2(0f, 0f);
					uv[1] = new Vector2(0f, 1f);
					uv[2] = new Vector2(1f, 1f);
					uv[3] = new Vector2(1f, 0f);
					break;
			}
			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;
			triangles[3] = 0;
			triangles[4] = 2;
			triangles[5] = 3;
			Mesh mesh = new Mesh();
			mesh.name = "NewPlaneMesh()";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>
		/// Create mesh with varying length of vertices rather than being restricted to 4
		/// </summary>
		/// <param name="size"></param>
		public static Mesh NewTriangleMesh(Vector2 size)
		{
			Vector3[] vertices = new Vector3[3];
			Vector2[] uv = new Vector2[3];
			int[] triangles = new int[3];

			vertices[0] = new Vector3(-0.5f * size.x, 0, 1 * size.y);
			vertices[1] = new Vector3(0.5f * size.x, 0, 1 * size.y);
			vertices[2] = new Vector3(0, 0, 0);

			uv[0] = vertices[0];
			uv[1] = vertices[1];
			uv[2] = vertices[2];

			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;

			Mesh mesh = new Mesh();
			mesh.name = "TriangleMesh";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
		
		/// <summary>
		/// Create triangle mesh with a cone like arc for an FOV effect
		/// </summary>
		/// <remarks><paramref name="arc"/> should be within [0:360]</remarks>
		/// <param name="size"></param>
		/// <param name="arc"></param>
		public static Mesh NewConeMesh(float distance, int arc)
		{
			float currentAngle = arc / -2f;
			Vector3[] vertices = new Vector3[arc + 2];
			Vector2[] uv = new Vector2[vertices.Length];
			int[] triangles = new int[arc * 3];

			vertices[0] = Vector3.zero;
			uv[0] = Vector3.zero;
			int t = 0;
			for (int i = 1; i <= arc; i++)
			{
				vertices[i] = vertices[0].PointFromAngle(distance, currentAngle);
				uv[i] = vertices[i];
				currentAngle += 1;

				triangles[t] = 0;
				triangles[t + 1] = i;
				triangles[t + 2] = i + 1;
				t += 3;
			}

			Mesh mesh = new Mesh();
			mesh.name = "ConeMesh";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>
		/// Reroute Draw method call to dynamic object's Draw method
		/// </summary>
		/// <param name="worldObject"></param>
		public static bool RenderDynamicWorldObjects(WorldObject worldObject)
		{
			if (VehicleMod.settings.main.dynamicWorldDrawing && worldObject is DynamicDrawnWorldObject dynamicObject)
			{
				dynamicObject.Draw();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Draw ring around edge tile cells given <paramref name="center"/> and <paramref name="radius"/>
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="material"></param>
		public static void DrawWorldRadiusRing(int center, int radius, Material material)
		{
			if (radius < 0)
			{
				return;
			}
			if (cachedEdgeTilesForCenter != center || cachedEdgeTilesForRadius != radius || cachedEdgeTilesForWorldSeed != Find.World.info.Seed)
			{
				cachedEdgeTilesForCenter = center;
				cachedEdgeTilesForRadius = radius;
				cachedEdgeTilesForWorldSeed = Find.World.info.Seed;
				cachedEdgeTiles.Clear();
				Find.WorldFloodFiller.FloodFill(center, (int tile) => true, delegate (int tile, int dist)
				{
					if (dist > radius + 1)
					{
						return true;
					}
					if (dist == radius + 1)
					{
						cachedEdgeTiles.Add(tile);
					}
					return false;
				}, int.MaxValue, null);
				WorldGrid worldGrid = Find.WorldGrid;
				Vector3 c = worldGrid.GetTileCenter(center);
				Vector3 n = c.normalized;
				cachedEdgeTiles.Sort(delegate (int a, int b)
				{
					float num = Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
					if (Mathf.Abs(num) < 0.0001f)
					{
						return 0;
					}
					if (num < 0f)
					{
						return -1;
					}
					return 1;
				});
			}
			GenDraw.DrawWorldLineStrip(cachedEdgeTiles, material, 5f);
		}
	}
}
