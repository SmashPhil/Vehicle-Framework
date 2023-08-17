using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public static class VehicleGUI
	{
		public static void DrawVehicleDefOnGUI(Rect rect, VehicleDef vehicleDef, PatternData patternData = null, Rot8? rot = null)
		{
			string drawStep = string.Empty;
			GUIState.Push();
			try
			{
				/* ----- Reused in VehicleGraphics ----- */
				if (rect.width != rect.height)
				{
					SmashLog.WarningOnce("Drawing VehicleDef with non-uniform rect. VehicleDefs are best drawn in square rects which will then be adjusted to fit.", nameof(DrawVehicleDefOnGUI).GetHashCode());
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
				Graphic_Vehicle graphic = VehicleTex.CachedGraphics[vehicleDef];

				PatternDef pattern = patternData?.patternDef;
				pattern ??= VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData)?.patternDef ?? PatternDefOf.Default;

				drawStep = "Fetching PatternData";
				Color color1 = patternData?.color ?? vehicleDef.graphicData.color;
				Color color2 = patternData?.colorTwo ?? vehicleDef.graphicData.color;
				Color color3 = patternData?.colorThree ?? vehicleDef.graphicData.color;

				float tiling = patternData?.tiles ?? vehicleDef.graphicData.tiles;
				Vector2 displacement = patternData?.displacement ?? vehicleDef.graphicData.displacement;

				Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);
				/* ------------------------------------- */

				bool colorGUI = graphic.Shader.SupportsRGBMaskTex() || graphic.Shader.SupportsMaskTex();

				if (colorGUI) GUI.color = color1;

				drawStep = "Attempting to retrieve turret overlays";
				List<(Rect rect, Texture mainTex, Color color, float layer, float angle)> overlays = new List<(Rect, Texture, Color, float, float)>();
				if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
				{
					overlays.AddRange(RetrieveAllTurretSettingsGUIProperties(rect, vehicleDef, rotDrawn, props.turrets.OrderBy(x => x.drawLayer),
						new PatternData(color1, color2, color3, pattern, displacement, tiling)));
				}
				drawStep = "Retrieving graphic overlays";
				overlays.AddRange(RetrieveAllOverlaySettingsGUIProperties(rect, vehicleDef, rotDrawn));

				drawStep = "Rendering overlays with layer < 0";
				//(Rect, Texture, Material, Layer, Angle)
				foreach (var overlay in overlays.Where(overlay => overlay.layer < 0).OrderBy(overlay => overlay.layer))
				{
					GUI.color = overlay.color;
					{
						UIElements.DrawTextureWithMaterialOnGUI(overlay.rect, overlay.mainTex, null, overlay.angle);
					}
					GUIState.Reset();
				}

				drawStep = "Rendering main texture";
				VehicleGraphics.DrawVehicleFitted(adjustedRect, angle, mainTex, material: null); //Null material will reroute to GUI methods

				GUIState.Reset();

				drawStep = "Rendering overlays with layer >= 0";
				foreach (var overlay in overlays.Where(overlay => overlay.layer >= 0).OrderBy(overlay => overlay.layer))
				{
					GUI.color = overlay.color;
					{
						UIElements.DrawTextureWithMaterialOnGUI(overlay.rect, overlay.mainTex, null, overlay.angle);
					}
					GUIState.Reset();
				}
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to draw GUI <type>VehicleDef</type>=\"{vehicleDef?.defName ?? "Null"}\" during step {drawStep}.\nException={ex.Message}");
			}
			finally
			{
				GUIState.Pop();
			}
		}

		/// <summary>
		/// Retrieve GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="rot"></param>
		public static IEnumerable<(Rect rect, Texture mainTex, Color color, float layer, float angle)> RetrieveAllOverlaySettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, List<GraphicOverlay> graphicOverlays = null)
		{
			List<GraphicOverlay> overlays = graphicOverlays ?? vehicleDef.drawProperties.overlays;
			foreach (GraphicOverlay graphicOverlay in overlays)
			{
				if (graphicOverlay.data.renderUI)
				{
					yield return RetrieveOverlaySettingsGUIProperties(rect, vehicleDef, rot, graphicOverlay);
				}
			}
		}

		public static (Rect rect, Texture mainTex, Color color, float layer, float angle) RetrieveOverlaySettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, GraphicOverlay graphicOverlay)
		{
			Rect overlayRect = VehicleGraphics.OverlayRect(rect, vehicleDef, graphicOverlay, rot);
			Texture2D texture = ContentFinder<Texture2D>.Get(graphicOverlay.data.graphicData.texPath);
			bool canMask = graphicOverlay.data.graphicData.Graphic.Shader.SupportsMaskTex() || graphicOverlay.data.graphicData.Graphic.Shader.SupportsRGBMaskTex();
			Color color = canMask ? graphicOverlay.data.graphicData.color : Color.white;
			return (overlayRect, texture, color, graphicOverlay.data.graphicData.DrawOffsetFull(rot).y, graphicOverlay.data.rotation);
		}

		/// <summary>
		/// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="displayRect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="cannons"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		public static IEnumerable<(Rect rect, Texture mainTex, Color color, float layer, float angle)> RetrieveAllTurretSettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (!turret.NoGraphic)
				{
					yield return RetrieveTurretSettingsGUIProperties(rect, vehicleDef, turret, rot, patternData);
				}
			}
		}

		public static (Rect rect, Texture mainTex, Color color, float layer, float angle) RetrieveTurretSettingsGUIProperties(Rect rect, VehicleDef vehicleDef, VehicleTurret turret, Rot8 rot, PatternData patternData, float iconScale = 1)
		{
			if (turret.NoGraphic)
			{
				Log.Warning($"Attempting to fetch GUI properties for VehicleTurret with no graphic.");
				return (Rect.zero, null, Color.white, -1, 0);
			}
			Rect turretRect = VehicleGraphics.TurretRect(rect, vehicleDef, turret, rot, iconScale: iconScale);
			bool canMask = turret.CannonGraphic.Shader.SupportsMaskTex() || turret.CannonGraphic.Shader.SupportsRGBMaskTex();
			Color color = canMask ? turret.turretDef.graphicData.color : Color.white;
			if (canMask && turret.turretDef.matchParentColor)
			{
				color = patternData.color;
			}
			return (turretRect, turret.CannonTexture, color, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
		}

		/// <summary>
		/// Draw <paramref name="buildDef"/> with proper vehicle material
		/// </summary>
		/// <param name="command"></param>
		/// <param name="rect"></param>
		/// <param name="buildDef"></param>
		public static GizmoResult GizmoOnGUIWithMaterial(Command command, Rect rect, VehicleBuildDef buildDef)
		{
			bool mouseOver = false;
			bool clicked = false;

			GUIState.Push();
			{
				VehicleDef vehicleDef = buildDef.thingToSpawn;
				Text.Font = GameFont.Tiny;
				if (Mouse.IsOver(rect))
				{
					mouseOver = true;
					if (!command.disabled)
					{
						GUI.color = GenUI.MouseoverColor;
					}
				}
				GUI.BeginGroup(rect.ContractedBy(1));
				{
					rect = rect.AtZero();
					MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
					Material material = command.disabled ? TexUI.GrayscaleGUI : null;
					GenUI.DrawTextureWithMaterial(rect, command.BGTexture, material);
					Rect buttonRect = rect;
					PatternData defaultPatternData = new PatternData(VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData));
					if (command.disabled)
					{
						defaultPatternData.color = vehicleDef.graphicData.color.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
						defaultPatternData.colorTwo = vehicleDef.graphicData.colorTwo.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
						defaultPatternData.colorThree = vehicleDef.graphicData.colorThree.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
					}
					DrawVehicleDefOnGUI(buttonRect, vehicleDef, defaultPatternData);

					KeyCode keyCode = (command.hotKey == null) ? KeyCode.None : command.hotKey.MainKey;
					if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
					{
						Vector2 vector = new Vector2(5f, 3f);
						Widgets.Label(new Rect(rect.x + vector.x, rect.y + vector.y, rect.width - 10f, 18f), keyCode.ToStringReadable());
						GizmoGridDrawer.drawnHotKeys.Add(keyCode);
						if (command.hotKey.KeyDownEvent)
						{
							clicked = true;
							Event.current.Use();
						}
					}
					if (Widgets.ButtonInvisible(rect, true))
					{
						clicked = true;
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
				}
				GUI.EndGroup();
			}
			GUIState.Pop();

			try
			{
				GUIState.Push();
				Text.Font = GameFont.Small;
				if (clicked)
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
					if (mouseOver)
					{
						return new GizmoResult(GizmoState.Mouseover, null);
					}
					return new GizmoResult(GizmoState.Clear, null);
				}
			}
			finally
			{
				GUIState.Pop();
			}
		}
	}
}
