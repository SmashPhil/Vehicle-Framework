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
			catch(Exception ex)
			{
				Log.Error(string.Format("Error occurred during rendering of attached thing on {0}. Exception: {1}", turret.vehicle.Label, ex.Message));
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
		public static IEnumerable<ValueTuple<Rect, Texture, Material, float, float>> RetrieveTurretSettingsDrawProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (turret.NoGraphic)
				{
					continue;
				}
				turret.ResolveCannonGraphics(vehicleDef);
				Vector2 rectSize = vehicleDef.ScaleDrawRatio(turret.turretDef.graphicData, rect.size);
				Vector2 adjustedPosition = rect.position + (rect.size - rectSize) / 2f;
				Rect turretRect = new Rect(turret.ScaleUIRect(adjustedPosition, rectSize, rot), rectSize);
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
				yield return new ValueTuple<Rect, Texture, Material, float, float>(turretRect, turret.CannonTexture, cannonMat, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
			}
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
			//Doesn't guarantee preventing dynamic drawing if def doesn't have expanding icon, second check required in DynamicDrawnWorldObject.Draw
			if (VehicleMod.settings.main.dynamicWorldDrawing && worldObject is DynamicDrawnWorldObject dynamicObject)
			{ 
				//dynamicObject.Draw();
				//return true;
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
