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
		public override void ProcessInput(Event ev)
		{
			if (turret.reloadTicks <= 0)
			{
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
				base.ProcessInput(ev);
				Vector3 target = turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
				turret.SetTarget(target.ToIntVec3());
				turret.PushTurretToQueue();
				turret.ResetPrefireTimer();
			}
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Text.Font = GameFont.Tiny;
			bool mouseOver = false;
			bool haltFlag = false;
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect gizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(6f);
			Rect indicatorIconRect = new Rect(gizmoRect.x + gizmoRect.width / 2f, gizmoRect.y + gizmoRect.height / 2, gizmoRect.width / 2, gizmoRect.height / 2);

			Material material = (!disabled) ? null : TexUI.GrayscaleGUI;
			Material cooldownMat = turret.OnCooldown ? TexUI.GrayscaleGUI : material;
			
			var gizmoColor = GUI.color;
			var ammoColor = GUI.color;
			var reloadColor = GUI.color;
			var fireModeColor = GUI.color;
			var autoTargetColor = GUI.color;

			bool ammoLoaded = true;

			Widgets.DrawWindowBackground(rect);
			Color haltColor = Color.white;
			if ((turret.loadedAmmo is null || turret.shellCount <= 0) && canReload)
			{
				disabledReason += "NoAmmoLoadedCannon".Translate();
				ammoLoaded = false;
			}
			else if (Mouse.IsOver(gizmoRect))
			{
				if (turret.cannonTarget.IsValid && Mouse.IsOver(indicatorIconRect))
				{
					haltColor = GenUI.MouseoverColor;
				}
				else
				{
					if (!disabled && !turret.OnCooldown)
					{
						GUI.color = GenUI.MouseoverColor;
					}
				}
				mouseOver = true;
				turret.GizmoHighlighted = true;
			}
			else
			{
				turret.GizmoHighlighted = false;
			}

			GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, cooldownMat, default);
			MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
			GUI.color = IconDrawColor;
			Widgets.DrawTextureFitted(gizmoRect, icon, iconDrawScale, iconProportions, iconTexCoords, iconAngle, turret.CannonMaterial);
			if (!ammoLoaded)
			{
				Widgets.DrawBoxSolid(gizmoRect, DarkGrey);
			}
			GUI.color = gizmoColor;

			if (turret.OnCooldown)
			{
				GUI.color = haltColor;
				GenUI.DrawTextureWithMaterial(indicatorIconRect, turret.FireIcon, CompFireOverlay.FireGraphic.MatAt(Rot4.North));
				GUI.color = gizmoColor;
			}
			else if (turret.cannonTarget.IsValid)
			{
				GUI.color = haltColor;
				GenUI.DrawTextureWithMaterial(indicatorIconRect, VehicleTex.HaltIcon, material, default);
				GUI.color = gizmoColor;
			}

			float gizmoWidth = gizmoRect.width / 2;
			float gizmoHeight = gizmoRect.height / 2 - 2;
			Rect cooldownRect = new Rect(gizmoRect.x + gizmoRect.width + 7f, gizmoRect.y, gizmoWidth, gizmoRect.height);
			Rect ammoRect = new Rect(cooldownRect.x + cooldownRect.width + 7f, cooldownRect.y + 1, gizmoRect.width / 2, gizmoHeight);
			Rect reloadRect = new Rect(ammoRect.x + ammoRect.width, ammoRect.y, gizmoWidth, gizmoHeight);
			Rect fireModeRect = new Rect(reloadRect.x + ammoRect.width, ammoRect.y, gizmoWidth, gizmoHeight);
			Rect autoTargetRect = new Rect(fireModeRect.x + ammoRect.width, ammoRect.y, gizmoWidth, gizmoHeight);
			
			float heatPoints = turret.currentHeatRate;
			UIElements.VerticalFillableBar(cooldownRect, heatPoints / VehicleTurret.MaxHeatCapacity,
				turret.OnCooldown ? TexData.RedTex : TexData.HeatColorPercent(heatPoints / VehicleTurret.MaxHeatCapacity), VehicleTex.EmptyBarTex, true);

			if (canReload && Mouse.IsOver(ammoRect))
			{
				mouseOver = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			float widthHeight = gizmoRect.height / 2 - 2;
			GenUI.DrawTextureWithMaterial(ammoRect, BGTex, canReload ? material : TexUI.GrayscaleGUI, default);
			if (turret.loadedAmmo != null)
			{
				alphaColorTicked.a = turret.CannonIconAlphaTicked;
				Graphics.DrawTexture(ammoRect, turret.loadedAmmo.uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);

				Rect ammoCountRect = new Rect(ammoRect);
				string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == turret.loadedAmmo).Select(t => t.stackCount).Sum().ToStringSafe();
				ammoCountRect.y += ammoCountRect.height / 2;
				ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
				Widgets.Label(ammoCountRect, ammoCount);
			}
			else if (turret.turretDef.genericAmmo)
			{
				Graphics.DrawTexture(ammoRect, turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault().uiIcon, material);

				Rect ammoCountRect = new Rect(ammoRect);
				string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()).Select(t => t.stackCount).Sum().ToStringSafe();
				ammoCountRect.y += ammoCountRect.height / 2;
				ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
				Widgets.Label(ammoCountRect, ammoCount);
			}

			GUI.color = ammoColor;
			if (canReload && Mouse.IsOver(reloadRect))
			{
				mouseOver = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			Graphics.DrawTexture(reloadRect, VehicleTex.ReloadIcon, canReload ? material : TexUI.GrayscaleGUI);
			TooltipHandler.TipRegion(reloadRect, "VehicleReloadVehicleTurret".Translate());

			GUI.color = fireModeColor;
			if (Mouse.IsOver(fireModeRect))
			{
				mouseOver = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			Graphics.DrawTexture(fireModeRect, turret.CurrentFireMode.Icon, material);
			TooltipHandler.TipRegion(fireModeRect, turret.CurrentFireMode.label);

			GUI.color = autoTargetColor;
			if (!turret.CanAutoTarget)
			{
				GUI.color = GenUI.MouseoverColor;
			}
			else if (Mouse.IsOver(autoTargetRect))
			{
				mouseOver = true;
				if (!disabled)
				{
					GUI.color = GenUI.SubtleMouseoverColor;
				}
			}
			Graphics.DrawTexture(autoTargetRect, VehicleTex.AutoTargetIcon, null);
			Rect checkboxRect = new Rect(autoTargetRect.x + autoTargetRect.width / 2, autoTargetRect.y + autoTargetRect.height / 2, autoTargetRect.width / 2, autoTargetRect.height / 2);
			GUI.DrawTexture(checkboxRect, turret.AutoTarget ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);//cannon.autoTargeting ? material : DisabledCBMat
			TooltipHandler.TipRegion(autoTargetRect, "AutoTargeting".Translate(turret.AutoTarget.ToString()));

			GUI.color = reloadColor;
			Rect reloadBar = rect.ContractedBy(6f);
			reloadBar.yMin = rect.y + (rect.height / 2) + 1;
			reloadBar.xMin = ammoRect.x;
			Widgets.FillableBar(reloadBar, (float)turret.shellCount / turret.turretDef.magazineCapacity, VehicleTex.FullBarTex, VehicleTex.EmptyBarTex, true);
			var font = Text.Font;
			var anchor = Text.Anchor;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			string ammoCountLabel = string.Format("{0} / {1}", turret.shellCount.ToString("F0"), turret.turretDef.magazineCapacity.ToString("F0"));
			if (turret.turretDef.magazineCapacity <= 0)
			{
				ammoCountLabel = "\u221E";
				Text.Font = GameFont.Medium;
			}
			Widgets.Label(reloadBar, ammoCountLabel);
			Text.Font = font;
			Text.Anchor = anchor;

			GUI.color = Color.white;
			bool fireTurret = false;
			bool reloadTurret = false;
			bool removeAmmo = false;
			bool cycleFireMode = false;
			bool switchTarget = false;
			KeyCode keyCode = (hotKey != null) ? hotKey.MainKey : KeyCode.None;
			if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
			{
				Rect rect2 = new Rect(gizmoRect.x + 5f, rect.y + 5f, gizmoRect.width - 10f, 18f);
				Widgets.Label(rect2, keyCode.ToStringReadable());
				GizmoGridDrawer.drawnHotKeys.Add(keyCode);
				if (hotKey.KeyDownEvent)
				{
					fireTurret = true;
					Event.current.Use();
				}
			}
			if(!disabled)
			{
				if (turret.cannonTarget.IsValid && Widgets.ButtonInvisible(indicatorIconRect, true))
				{
					haltFlag = true;
				}
				else if (ammoLoaded && Widgets.ButtonInvisible(gizmoRect, true))
				{
					fireTurret = true;
				}
				if (canReload && Widgets.ButtonInvisible(reloadRect, true))
				{
					reloadTurret = true;
				}
				if (canReload && turret.shellCount > 0 && Widgets.ButtonInvisible(ammoRect, true))
				{
					removeAmmo = true;
				}
				if (Widgets.ButtonInvisible(fireModeRect, true))
				{
					cycleFireMode = true;
				}
				if (Widgets.ButtonInvisible(autoTargetRect, true))
				{
					switchTarget = true;
				}
			}

			string labelCap = LabelCap;
			if (!labelCap.NullOrEmpty())
			{
				float num = Text.CalcHeight(labelCap, rect.width);
				Rect rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
				GUI.DrawTexture(rect3, TexUI.GrayTextBG);
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(rect3, labelCap);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
			GUI.color = Color.white;
			if (DoTooltip)
			{
				TipSignal tip = Desc;
				if (!disabledReason.NullOrEmpty())
				{
					string text = tip.text;
					tip.text = string.Concat(new string[]
					{
						text,
						"\n\n",
						"DisabledCommand".Translate(),
						": ",
						disabledReason
					});
				}
				TooltipHandler.TipRegion(gizmoRect, tip);
			}
			if (turret.reloadTicks > 0)
			{
				float percent = turret.reloadTicks / (float)turret.MaxTicks;
				UIElements.VerticalFillableBar(gizmoRect, percent, UIData.FillableBarTexture, UIData.ClearBarTexture);
			}
			if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
			{
				UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
			}

			Text.Font = GameFont.Small;
			if (fireTurret && !turret.OnCooldown)
			{
				if (disabled)
				{
					if (!disabledReason.NullOrEmpty())
					{
						Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
					}
					return new GizmoResult(GizmoState.Mouseover, null);
				}
				if (!TutorSystem.AllowAction(TutorTagSelect))
				{
					return new GizmoResult(GizmoState.Mouseover, null);
				}
				var result = new GizmoResult(GizmoState.Interacted, Event.current);
				TutorSystem.Notify_Event(TutorTagSelect);
				return result;
			}
			if (haltFlag)
			{
				if (disabled)
				{
					if (!disabledReason.NullOrEmpty())
					{
						Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
					}
					return new GizmoResult(GizmoState.Mouseover, null);
				}
				turret.SetTarget(LocalTargetInfo.Invalid);
			}
			if (reloadTurret)
			{
				if (turret.turretDef.genericAmmo)
				{
					if (!turret.vehicle.inventory.innerContainer.Contains(turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()))
					{
						Messages.Message("NoAmmoAvailable".Translate(), MessageTypeDefOf.RejectInput);
					}
					else
					{
						turret.ReloadCannon(turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault());
					}
				}
				else
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					var ammoAvailable = turret.vehicle.inventory.innerContainer.Where(d => turret.ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
					for (int i = ammoAvailable.Count - 1; i >= 0; i--)
					{
						ThingDef ammo = ammoAvailable[i];
						options.Add(new FloatMenuOption(ammoAvailable[i].LabelCap, delegate ()
						{
							turret.ReloadCannon(ammo, true);
						}));
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}
			if (removeAmmo)
			{
				turret.TryRemoveShell();
				SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(turret.vehicle.Position, turret.vehicle.Map, false));
			}
			if (cycleFireMode)
			{
				turret.CycleFireMode();
			}
			if (switchTarget)
			{
				turret.SwitchAutoTarget();
			}
			if (mouseOver)
			{
				return new GizmoResult(GizmoState.Mouseover, null);
			}
			return new GizmoResult(GizmoState.Clear, null);
		}
	}
}
