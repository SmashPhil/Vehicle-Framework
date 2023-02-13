using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class Command_CooldownAction : Command_Turret
	{
		protected const float TurretGizmoPadding = 5;

		protected const float GizmoHeight = 75;
		protected const float SubIconSize = GizmoHeight / 2.25f;

		protected const float CooldownBarWidth = 32;
		protected const float PaddingBetweenElements = 2;

		protected override float RecalculateWidth()
		{
			float minWidth = GizmoHeight;
			if (turret.CanOverheat)
			{
				minWidth += CooldownBarWidth + TurretGizmoPadding;
			}
			minWidth += turret.SubGizmos.Count() * SubIconSize;
			return minWidth;
		}

		public override void FireTurret(VehicleTurret turret)
		{
			if (turret.ReloadTicks <= 0)
			{
				Vector3 target = turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
				turret.SetTarget(target.ToIntVec3());
				turret.PushTurretToQueue();
				turret.ResetPrefireTimer();
			}
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			GUIState.Push();
			try
			{
				Text.Font = GameFont.Tiny;
				Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);

				Material material = !disabled ? null : TexUI.GrayscaleGUI;
				Material cooldownMaterial = turret.OnCooldown ? TexUI.GrayscaleGUI : material;

				Widgets.DrawWindowBackground(rect);
				rect = rect.ContractedBy(TurretGizmoPadding); //padding between all contents inside gizmo background

				Rect gizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height);
				float gizmoWidth = DrawGizmoButton(gizmoRect, cooldownMaterial, out bool mouseOver, out bool ammoLoaded, out bool fireTurret, out bool haltTurret);

				GUIState.Reset();

				Rect cooldownRect = new Rect(gizmoRect.x + gizmoWidth + TurretGizmoPadding, gizmoRect.y, CooldownBarWidth, gizmoRect.height);
				float cooldownWidth = DrawCooldownBar(cooldownRect);

				float topIconSize = gizmoRect.height / 2;
				float barWidth = rect.width - (gizmoWidth + cooldownWidth + TurretGizmoPadding);
				Rect topBarRect = new Rect(cooldownRect.x + cooldownWidth, gizmoRect.y, barWidth, topIconSize);
				VehicleTurret.SubGizmo subGizmo = DrawTopBar(topBarRect, ref mouseOver);
				Rect bottomBarRect = new Rect(topBarRect.x, topBarRect.y + topIconSize, topBarRect.width, topIconSize);
				DrawBottomBar(bottomBarRect);

				GUIState.Reset();

				if (!LabelCap.NullOrEmpty())
				{
					GUI.color = Color.white;
					Text.Font = GameFont.Tiny;

					float textWidth = gizmoRect.width + TurretGizmoPadding * 2;
					float textHeight = Text.CalcHeight(LabelCap, textWidth);
					Rect labelRect = new Rect(gizmoRect.x, rect.yMax - textHeight + 12f, textWidth, textHeight);
					GUI.DrawTexture(labelRect, TexUI.GrayTextBG);

					Text.Anchor = TextAnchor.UpperCenter;
					Widgets.Label(labelRect, LabelCap);
				}

				GUIState.Reset();

				if (DoTooltip)
				{
					string tooltip = Desc;
					if (!disabledReason.NullOrEmpty())
					{
						tooltip = $"{Desc}\n\n{"DisabledCommand".Translate()}: {disabledReason}";
					}
					TooltipHandler.TipRegion(gizmoRect, tooltip);
				}

				GUIState.Reset();

				if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
				{
					UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
				}

				GUIState.Reset();

				if (hotKey is KeyBindingDef keyBind && keyBind.MainKey != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyBind.MainKey))
				{
					Rect hotkeyRect = new Rect(gizmoRect.x + 5f, rect.y + 5f, gizmoRect.width - 10f, 18f);
					Widgets.Label(hotkeyRect, keyBind.MainKey.ToStringReadable());
					GizmoGridDrawer.drawnHotKeys.Add(keyBind.MainKey);
					if (hotKey.KeyDownEvent)
					{
						fireTurret = true;
						Event.current.Use();
					}
				}

				if (subGizmo.IsValid) subGizmo.onClick();

				GUIState.Reset();
				Text.Font = GameFont.Small;

				if (haltTurret)
				{
					if (disabled)
					{
						if (!disabledReason.NullOrEmpty())
						{
							Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
						}
						return new GizmoResult(GizmoState.Mouseover);
					}
					turret.SetTarget(LocalTargetInfo.Invalid);
					return new GizmoResult(GizmoState.Clear);
				}

				if (fireTurret && !turret.OnCooldown)
				{
					if (disabled)
					{
						if (!disabledReason.NullOrEmpty())
						{
							Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
						}
						return new GizmoResult(GizmoState.Mouseover);
					}
					if (!TutorSystem.AllowAction(TutorTagSelect))
					{
						return new GizmoResult(GizmoState.Mouseover);
					}
					GizmoResult result = new GizmoResult(GizmoState.Interacted, Event.current);
					TutorSystem.Notify_Event(TutorTagSelect);
					return result;
				}

				if (mouseOver)
				{
					return new GizmoResult(GizmoState.Mouseover);
				}
				return new GizmoResult(GizmoState.Clear);
			}
			finally
			{
				GUIState.Pop();
			}
		}

		protected virtual float DrawGizmoButton(Rect rect, Material cooldownMaterial, out bool mouseOver, out bool ammoLoaded, out bool fireTurret, out bool haltTurret)
		{
			rect = rect.ContractedBy(2);
			
			ammoLoaded = true;
			mouseOver = false;
			fireTurret = false;
			
			Widgets.BeginGroup(rect);
			{
				Rect gizmoRect = rect.AtZero();
				Rect subIconRect = new Rect(gizmoRect.xMax - SubIconSize, gizmoRect.y, SubIconSize, SubIconSize); //top right
				if ((turret.loadedAmmo is null || turret.shellCount <= 0) && turret.turretDef.ammunition != null)
				{
					disabledReason += "NoAmmoLoadedCannon".Translate();
					ammoLoaded = false;
				}
				else if (!turret.OnCooldown && Mouse.IsOver(gizmoRect) && (!Mouse.IsOver(subIconRect) || !turret.cannonTarget.IsValid))
				{
					if (!disabled)
					{
						GUI.color = GenUI.MouseoverColor;
					}
					mouseOver = true;
					turret.GizmoHighlighted = true;
				}
				else
				{
					turret.GizmoHighlighted = false;
				}

				GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, cooldownMaterial, default);
				MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
				GUI.color = IconDrawColor;

				GUIState.Push();
				{
					Rect turretRect = gizmoRect.ContractedBy(2);
					(Rect rect, Texture mainTex, Color color, float layer, float angle) turretProps = VehicleGUI.RetrieveTurretSettingsGUIProperties(turretRect, vehicle.VehicleDef, turret, Rot8.North, vehicle.patternData, iconScale: iconDrawScale);
					Widgets.BeginGroup(turretRect);
					{
						GUI.color = turretProps.color;
						//Draw turret facing North for gizmos
						UIElements.DrawTextureWithMaterialOnGUI(turretRect.AtZero().ExpandedBy(turretRect.width * (iconDrawScale - 1)), turretProps.mainTex, null, 0); 
					}
					Widgets.EndGroup();
				}
				GUIState.Pop();
				if (!ammoLoaded)
				{
					Widgets.DrawBoxSolid(gizmoRect, DarkGrey);
				}

				if (turret.ReloadTicks > 0)
				{
					float percent = turret.ReloadTicks / (float)turret.MaxTicks;
					UIElements.VerticalFillableBar(gizmoRect, percent, UIData.FillableBarTexture, UIData.ClearBarTexture);
				}

				GUIState.Reset();

				if (DrawSubIcons(subIconRect, cooldownMaterial, out haltTurret))
				{
					mouseOver = false;
					fireTurret = false;
				}
				else if (ammoLoaded && Widgets.ButtonInvisible(gizmoRect, true))
				{
					fireTurret = true;
				}
			}
			Widgets.EndGroup();

			return rect.width;
		}

		/// <summary>
		/// Sub icons rendered in bottom right of Gizmo button
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="material"></param>
		/// <param name="haltTurret"></param>
		/// <returns>Mouse is over SubIcon rect</returns>
		protected virtual bool DrawSubIcons(Rect rect, Material material, out bool haltTurret)
		{
			bool mouseOverSubIcon = false;
			haltTurret = false;

			if (turret.OnCooldown)
			{
				GenUI.DrawTextureWithMaterial(rect, turret.FireIcon, CompFireOverlay.FireGraphic.MatAt(Rot4.North));
			}
			else if (turret.cannonTarget.IsValid)
			{
				if (!disabled && Widgets.ButtonInvisible(rect, true))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					haltTurret = true;
				}

				if (!disabled && Mouse.IsOver(rect))
				{
					mouseOverSubIcon = true;
					GUI.color = GenUI.MouseoverColor;
				}
				GenUI.DrawTextureWithMaterial(rect, VehicleTex.HaltIcon, material, default);
			}
			return mouseOverSubIcon; //No MouseOver for Gizmo rect if mouse is over active sub icon
		}

		protected virtual float DrawCooldownBar(Rect rect)
		{
			if (turret.CanOverheat)
			{
				float heatPercent = turret.currentHeatRate / VehicleTurret.MaxHeatCapacity;
				UIElements.VerticalFillableBar(rect, heatPercent, TexData.HeatColorPercent(heatPercent), BaseContent.BlackTex, doBorder: true);
				return rect.width + TurretGizmoPadding;
			}
			return 0;
		}

		protected virtual VehicleTurret.SubGizmo DrawTopBar(Rect rect, ref bool mouseOver)
		{
			VehicleTurret.SubGizmo clickedGizmo = VehicleTurret.SubGizmo.None;
			GUIState.Push();
			{
				Rect subGizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height);
				foreach (VehicleTurret.SubGizmo subGizmo in turret.SubGizmos)
				{
					if (!disabled)
					{
						TooltipHandler.TipRegion(subGizmoRect, subGizmo.tooltip);
						if (subGizmo.canClick() && Mouse.IsOver(subGizmoRect))
						{
							mouseOver = true;
							GUI.color = GenUI.SubtleMouseoverColor; //MouseoverColor
							if (Widgets.ButtonInvisible(subGizmoRect))
							{
								clickedGizmo = subGizmo;
							}
						}
					}
					subGizmo.drawGizmo(subGizmoRect);
					GUIState.Reset();
					subGizmoRect.x += SubIconSize;
				}
			}
			GUIState.Pop();

			return clickedGizmo;
		}

		protected virtual void DrawBottomBar(Rect rect)
		{
			Widgets.FillableBar(rect, (float)turret.shellCount / turret.turretDef.magazineCapacity, VehicleTex.FullBarTex, VehicleTex.EmptyBarTex, true);

			GUIState.Push();
			{
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				string ammoCountLabel = string.Format("{0} / {1}", turret.shellCount.ToString("F0"), turret.turretDef.magazineCapacity.ToString("F0"));
				if (turret.turretDef.magazineCapacity <= 0)
				{
					ammoCountLabel = "\u221E";
					Text.Font = GameFont.Medium;
				}
				Widgets.Label(rect, ammoCountLabel);
			}
			GUIState.Pop();
		}
	}
}
