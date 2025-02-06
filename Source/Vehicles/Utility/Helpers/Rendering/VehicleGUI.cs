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
		public static void DrawVehicleDefOnGUI(Rect rect, VehicleDef vehicleDef, PatternData patternData = null, Rot8? rot = null, bool withoutTurrets = true)
		{
			//string drawStep = string.Empty;
			try
			{
				/* ----- Reused in VehicleGraphics ----- */
				if (rect.width != rect.height)
				{
					SmashLog.WarningOnce("Drawing VehicleDef with non-uniform rect. VehicleDefs are best drawn in square rects which will then be adjusted to fit.", nameof(DrawVehicleDefOnGUI).GetHashCode());
				}
				//drawStep = "Setting rect and adjusted positioning.";
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

				//drawStep = "Retrieving cached graphic and pattern";
				Graphic_Vehicle graphic = vehicleDef.graphicData.Graphic as Graphic_Vehicle;// VehicleTex.CachedGraphics[vehicleDef];

				//drawStep = "Fetching PatternData";
				PatternData pattern = patternData ?? VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData);
				if (!VehicleMod.settings.main.useCustomShaders)
				{
					pattern.patternDef = PatternDefOf.Default;
				}
				
				Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);
				/* ------------------------------------- */

				bool colorGUI = graphic.Shader.SupportsRGBMaskTex() || graphic.Shader.SupportsMaskTex();

				if (colorGUI) GUI.color = pattern?.color ?? Color.white;

				OverlayRendering.Clear();

				//drawStep = "Attempting to retrieve turret overlays";
				if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets props)
				{
					if (!withoutTurrets || Prefs.UIScale == 1)
					{
						foreach (RenderData turretRenderData in RetrieveAllTurretSettingsGUIProperties(rect, vehicleDef, rotDrawn, props.turrets.OrderBy(x => x.drawLayer), pattern))
						{
							OverlayRendering.Add(turretRenderData);
						}
					}
				}
				//drawStep = "Retrieving graphic overlays";
				foreach (RenderData turretRenderData in RetrieveAllOverlaySettingsGUIProperties(rect, vehicleDef, rotDrawn))
				{
					OverlayRendering.Add(turretRenderData);
				}

				OverlayRendering.FinalizeForRendering();

				//drawStep = "Rendering overlays with layer < 0";
				OverlayRendering.RenderLayer(GUILayer.Lower);

				//drawStep = "Rendering main texture";
				VehicleGraphics.DrawVehicleFitted(adjustedRect, angle, mainTex, material: null); //Null material will reroute to GUI methods

				//drawStep = "Rendering overlays with layer >= 0";
				OverlayRendering.RenderLayer(GUILayer.Upper);
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while trying to draw GUI <type>VehicleDef</type>=\"{vehicleDef?.defName ?? "Null"}\".\nException={ex}");
			}
			finally
			{
				OverlayRendering.Clear();
			}
		}

		/// <summary>
		/// Retrieve GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="rot"></param>
		public static IEnumerable<RenderData> RetrieveAllOverlaySettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, List<GraphicOverlay> graphicOverlays = null)
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

		public static RenderData RetrieveOverlaySettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, GraphicOverlay graphicOverlay)
		{
			Rect overlayRect = VehicleGraphics.OverlayRect(rect, vehicleDef, graphicOverlay, rot);
			Graphic graphic = graphicOverlay.Graphic;
			Texture2D texture;
			if (graphic is Graphic_RGB graphicRGB)
			{
				texture = graphicRGB.TexAt(rot);
			}
			else
			{
				texture = graphic.MatAt(rot).mainTexture as Texture2D;
			}
			bool canMask = graphicOverlay.Graphic.Shader.SupportsMaskTex() || graphicOverlay.Graphic.Shader.SupportsRGBMaskTex();
			Color color = canMask ? graphicOverlay.data.graphicData.color : Color.white;
			return new RenderData(overlayRect, texture, color, graphicOverlay.data.graphicData.DrawOffsetFull(rot).y, graphicOverlay.data.rotation);
		}

		/// <summary>
		/// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="displayRect"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="cannons"></param>
		/// <param name="patternData"></param>
		/// <param name="rot"></param>
		public static IEnumerable<RenderData> RetrieveAllTurretSettingsGUIProperties(Rect rect, VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData)
		{
			foreach (VehicleTurret turret in turrets)
			{
				if (!turret.parentKey.NullOrEmpty())
				{
					continue; //Attached turrets temporarily disabled from rendering
				}
				if (!turret.NoGraphic)
				{
					yield return RetrieveTurretSettingsGUIProperties(rect, vehicleDef, turret, rot, patternData);
				}
				if (!turret.TurretGraphics.NullOrEmpty())
				{
					foreach (VehicleTurret.TurretDrawData turretDrawData in turret.TurretGraphics)
					{
						Rect turretRect = VehicleGraphics.TurretRect(rect, vehicleDef, turret, rot);
						bool canMask = turretDrawData.graphic.Shader.SupportsMaskTex() || turretDrawData.graphic.Shader.SupportsRGBMaskTex();
						Color color = canMask ? turretDrawData.graphicDataRGB.color : Color.white;
						if (canMask && turret.turretDef.matchParentColor)
						{
							color = patternData.color;
						}
						yield return new RenderData(turretRect, turretDrawData.graphic.TexAt(Rot8.North), color, turretDrawData.graphicDataRGB.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
					}
				}
			}
		}

		public static RenderData RetrieveTurretSettingsGUIProperties(Rect rect, VehicleDef vehicleDef, VehicleTurret turret, Rot8 rot, PatternData patternData, float iconScale = 1)
		{
			if (turret.NoGraphic)
			{
				Log.Warning($"Attempting to fetch GUI properties for VehicleTurret with no graphic.");
				return RenderData.Invalid;
			}
			Rect turretRect = VehicleGraphics.TurretRect(rect, vehicleDef, turret, rot, iconScale: iconScale);
			bool canMask = turret.CannonGraphic.Shader.SupportsMaskTex() || turret.CannonGraphic.Shader.SupportsRGBMaskTex();
			Color color = canMask ? turret.turretDef.graphicData.color : Color.white;
			if (canMask && turret.turretDef.matchParentColor)
			{
				color = patternData.color;
			}
			return new RenderData(turretRect, turret.CannonTexture, color, turret.CannonGraphicData.drawOffset.y, turret.defaultAngleRotated + rot.AsAngle);
		}

		/// <summary>
		/// Draw <paramref name="buildDef"/> with proper vehicle material
		/// </summary>
		/// <param name="command"></param>
		/// <param name="rect"></param>
		/// <param name="buildDef"></param>
		public static GizmoResult GizmoOnGUIWithMaterial(Command command, Rect rect, GizmoRenderParms parms, VehicleBuildDef buildDef)
		{
			bool mouseOver = false;
			bool clicked = false;

			VehicleDef vehicleDef = buildDef.thingToSpawn;
			using TextBlock textFont = new(GameFont.Tiny, Color.white);

			if (Mouse.IsOver(rect))
			{
				mouseOver = true;
				if (!command.Disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}

			MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
			if (parms.highLight)
			{
				Widgets.DrawStrongHighlight(rect.ExpandedBy(12f), null);
			}

			if (parms.lowLight)
			{
				GUI.color = Command.LowLightBgColor;
			}
			Material material = command.Disabled ? TexUI.GrayscaleGUI : null;
			GenUI.DrawTextureWithMaterial(rect, command.BGTexture, material);
			GUI.color = Color.white;

			// Begin GUI Group
			Rect iconRect = rect.ContractedBy(1);
			GUI.BeginGroup(iconRect);
			iconRect = iconRect.AtZero();
			
			Rect buttonRect = iconRect;
			PatternData defaultPatternData = new PatternData(VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData));
			if (command.Disabled)
			{
				defaultPatternData.color = vehicleDef.graphicData.color.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
				defaultPatternData.colorTwo = vehicleDef.graphicData.colorTwo.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
				defaultPatternData.colorThree = vehicleDef.graphicData.colorThree.SubtractNoAlpha(0.1f, 0.1f, 0.1f);
			}

			if (!command.Disabled || parms.lowLight)
			{
				GUI.color = command.IconDrawColor;
			}
			else
			{
				GUI.color = command.IconDrawColor.SaturationChanged(0f);
				defaultPatternData.color = vehicleDef.graphicData.color.SaturationChanged(0);
				defaultPatternData.colorTwo = vehicleDef.graphicData.colorTwo.SaturationChanged(0);
				defaultPatternData.colorThree = vehicleDef.graphicData.colorThree.SaturationChanged(0);
			}
			if (parms.lowLight)
			{
				GUI.color = GUI.color.ToTransparent(0.6f);
				defaultPatternData.color = defaultPatternData.color.ToTransparent(0.6f);
				defaultPatternData.colorTwo = defaultPatternData.colorTwo.ToTransparent(0.6f);
				defaultPatternData.colorThree = defaultPatternData.colorThree.ToTransparent(0.6f);
			}
			DrawVehicleDefOnGUI(buttonRect, vehicleDef, defaultPatternData);
			GUI.color = Color.white;

			if (command.hotKey != null)
			{
				KeyCode keyCode = command.hotKey.MainKey;
				if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
				{
					Vector2 vector = new Vector2(5f, 3f);
					Widgets.Label(new Rect(iconRect.x + vector.x, iconRect.y + vector.y, iconRect.width - 10f, 18f), keyCode.ToStringReadable());
					GizmoGridDrawer.drawnHotKeys.Add(keyCode);
					if (command.hotKey.KeyDownEvent)
					{
						clicked = true;
						Event.current.Use();
					}
				}
			}
			if (Widgets.ButtonInvisible(iconRect, true))
			{
				clicked = true;
			}
			GUI.EndGroup();
			// End GUI Group

			string topRightLabel = command.TopRightLabel;
			if (!topRightLabel.NullOrEmpty())
			{
				Vector2 vector2 = Text.CalcSize(topRightLabel);
				Rect position = new Rect(rect.xMax - vector2.x - 2f, rect.y + 3f, vector2.x, vector2.y);
				Rect rectBase = position;
				position.x -= 2f;
				position.width += 3f;

				using TextBlock labelBlock = new(TextAnchor.UpperRight, Color.white);
				GUI.DrawTexture(position, TexUI.GrayTextBG);
				Widgets.Label(rectBase, topRightLabel);
			}
			string labelCap = buildDef.LabelCap;
			if (!labelCap.NullOrEmpty())
			{
				float num = Text.CalcHeight(labelCap, rect.width);
				Rect rect2 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
				GUI.DrawTexture(rect2, TexUI.GrayTextBG);
				using TextBlock labelBlock = new(TextAnchor.UpperCenter, Color.white);
				Widgets.Label(rect2, labelCap);
			}
			GUI.color = Color.white;
			if (Mouse.IsOver(rect))
			{
				TipSignal tip = command.Desc;
				if (command.Disabled && !command.disabledReason.NullOrEmpty())
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
			if (clicked)
			{
				if (command.Disabled)
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
			return new GizmoResult(mouseOver ? GizmoState.Mouseover : GizmoState.Clear, null);
		}

		private enum GUILayer
		{
			Lower,
			Upper
		}

		public readonly struct RenderData : IComparable<RenderData>
		{
			public readonly Rect rect;
			public readonly Texture mainTex;
			public readonly Color color;
			public readonly float layer;
			public readonly float angle;

			public RenderData(Rect rect, Texture mainTex, Color color, float layer, float angle)
			{
				this.rect = rect;
				this.mainTex = mainTex;
				this.color = color;
				this.layer = layer;
				this.angle = angle;
			}

			public static RenderData Invalid => new RenderData(Rect.zero, null, Color.white, -1, 0);

			readonly int IComparable<RenderData>.CompareTo(RenderData other)
			{
				if (this.layer < other.layer)
				{
					return -1;
				}
				else if (this.layer == other.layer)
				{
					return 0;
				}
				return 1;
			}
		}

		private static class OverlayRendering
		{
			private static readonly List<RenderData> renderDataLower = [];
			private static readonly List<RenderData> renderDataUpper = [];

			public static void Add(RenderData renderData)
			{
				if (renderData.layer < 0)
				{
					renderDataLower.Add(renderData);
				}
				else
				{
					renderDataUpper.Add(renderData);
				}
			}

			public static void Clear()
			{
				renderDataLower.Clear();
				renderDataUpper.Clear();
			}

			public static void FinalizeForRendering()
			{
				renderDataLower.Sort();
				renderDataUpper.Sort();
			}

			public static void RenderLayer(GUILayer layer)
			{
				switch (layer)
				{
					case GUILayer.Lower:
						{
							foreach (RenderData renderData in renderDataLower)
							{
								using TextBlock guiColor = new(renderData.color);
								UIElements.DrawTextureWithMaterialOnGUI(renderData.rect, renderData.mainTex, null, renderData.angle);
							}
						}
						break;
					case GUILayer.Upper:
						{
							foreach (RenderData renderData in renderDataUpper)
							{
								using TextBlock guiColor = new(renderData.color);
								UIElements.DrawTextureWithMaterialOnGUI(renderData.rect, renderData.mainTex, null, renderData.angle);
							}
						}
						break;
					default:
						throw new NotImplementedException(nameof(GUILayer));
				}
			}
		}
	}
}
