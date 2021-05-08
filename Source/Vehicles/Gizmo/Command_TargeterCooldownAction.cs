using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	//REDO - Add in capability for turret grouping
	public class Command_TargeterCooldownAction : Command_CooldownAction
	{
		public VehicleTurret turret;

		public Command_TargeterCooldownAction()
		{

		}

		public override void ProcessInput(Event ev)
		{
			if (turrets.All(t => t.reloadTicks <= 0))
			{
				base.ProcessInput(ev);
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
				Targeters.CannonTargeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target)
				{
					turret.SetTarget(target);
					turret.ResetPrefireTimer();
				}, turret, null, null);
			}
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
		{
			Text.Font = GameFont.Tiny;
			bool flag = false;
			bool haltFlag = false;
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect gizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(6f);
			Rect haltIconRect = new Rect(gizmoRect.x + gizmoRect.width / 2f, gizmoRect.y + gizmoRect.height / 2, gizmoRect.width / 2, gizmoRect.height / 2);

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
			if ((turret.loadedAmmo is null || turret.shellCount <= 0) && turret.turretDef.ammunition != null)
			{
				disabledReason += "NoAmmoLoadedCannon".Translate();
				ammoLoaded = false;
			}
			else if (!turret.OnCooldown && Mouse.IsOver(gizmoRect))
			{
				if(turret.cannonTarget.IsValid && Mouse.IsOver(haltIconRect))
				{
					haltColor = GenUI.MouseoverColor;
				}
				else
				{

					if (!disabled)
					{
						GUI.color = GenUI.MouseoverColor;
					}
				}
				flag = true;
				turret.GizmoHighlighted = true;
			}
			else
			{
				turret.GizmoHighlighted = false;
			}

			GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, cooldownMat, default);
			MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
			GUI.color = IconDrawColor;
			Widgets.DrawTextureFitted(gizmoRect, turret.CannonTexture, iconDrawScale, iconProportions, iconTexCoords, iconAngle, turret.CannonMaterial);
			if (!ammoLoaded)
			{
				Widgets.DrawBoxSolid(gizmoRect, DarkGrey);
			}
			GUI.color = gizmoColor;

			if (turret.cannonTarget.IsValid)
			{
				GUI.color = haltColor;
				GenUI.DrawTextureWithMaterial(haltIconRect, VehicleTex.HaltIcon, material, default);
				GUI.color = gizmoColor;
			}

			Rect cooldownRect = new Rect(gizmoRect.x + gizmoRect.width + 7f, gizmoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
			Rect ammoRect = new Rect(gizmoRect.x + gizmoRect.width + 7f, gizmoRect.y + 1, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
			Rect reloadRect = new Rect(ammoRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
			Rect fireModeRect = new Rect(reloadRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
			Rect autoTargetRect = new Rect(fireModeRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);

			UIElements.VerticalFillableBar(cooldownRect, turret.currentHeatRate, TexData.RedTex);

			if (Mouse.IsOver(ammoRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			float widthHeight = gizmoRect.height / 2 - 2;
			GenUI.DrawTextureWithMaterial(ammoRect, BGTex, material, default);
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
			else if (turret.turretDef.genericAmmo && turret.turretDef.ammunition.AllowedDefCount > 0)
			{
				Graphics.DrawTexture(ammoRect, turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault().uiIcon, material);

				Rect ammoCountRect = new Rect(ammoRect);
				string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()).Select(t => t.stackCount).Sum().ToStringSafe();
				ammoCountRect.y += ammoCountRect.height / 2;
				ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
				Widgets.Label(ammoCountRect, ammoCount);
			}

			GUI.color = ammoColor;
			if (Mouse.IsOver(reloadRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			Graphics.DrawTexture(reloadRect, VehicleTex.ReloadIcon, material);
			TooltipHandler.TipRegion(reloadRect, "ReloadVehicleTurret".Translate());

			GUI.color = fireModeColor;
			if (Mouse.IsOver(fireModeRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			Graphics.DrawTexture(fireModeRect, turret.CurrentFireMode.Icon, material);
			TooltipHandler.TipRegion(fireModeRect, turret.CurrentFireMode.label);

			GUI.color = autoTargetColor;
			if (!turret.autoTargeting)
			{
				GUI.color = GenUI.MouseoverColor;
			}
			else if (Mouse.IsOver(autoTargetRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.SubtleMouseoverColor;
				}
			}
			Graphics.DrawTexture(autoTargetRect, VehicleTex.AutoTargetIcon, material);
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
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			bool flag6 = false;
			KeyCode keyCode = (hotKey != null) ? hotKey.MainKey : KeyCode.None;
			if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
			{
				Rect rect2 = new Rect(gizmoRect.x + 5f, rect.y + 5f, gizmoRect.width - 10f, 18f);
				Widgets.Label(rect2, keyCode.ToStringReadable());
				GizmoGridDrawer.drawnHotKeys.Add(keyCode);
				if (hotKey.KeyDownEvent)
				{
					flag2 = true;
					Event.current.Use();
				}
			}
			if(!disabled)
			{
				if (turret.cannonTarget.IsValid && Widgets.ButtonInvisible(haltIconRect, true))
				{
					haltFlag = true;
				}
				else if (ammoLoaded && Widgets.ButtonInvisible(gizmoRect, true))
				{
					flag2 = true;
				}
				if (Widgets.ButtonInvisible(reloadRect, true))
				{
					flag3 = true;
				}
				if ( (turret.shellCount > 0) && Widgets.ButtonInvisible(ammoRect, true))
				{
					flag4 = true;
				}
				if (Widgets.ButtonInvisible(fireModeRect, true))
				{
					flag5 = true;
				}
				if (Widgets.ButtonInvisible(autoTargetRect, true))
				{
					flag6 = true;
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
			Rect ammoWindowRect = new Rect(rect);
			ammoWindowRect.height = AmmoWindowOffset * 2;
			ammoWindowRect.width = ammoRect.height * 6 + AmmoWindowOffset * 2;
			if (turret.ammoWindowOpened)
			{
				List<ThingDef> allVehicleTurretDefs = turret.vehicle.inventory.innerContainer.Where(d => turret.ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
				ammoWindowRect.height += ammoRect.height + Mathf.CeilToInt(allVehicleTurretDefs.Count / 6) * ammoRect.height;
				ammoWindowRect.y -= (ammoWindowRect.height + AmmoWindowOffset);
				GenUI.DrawTextureWithMaterial(ammoWindowRect, VehicleTex.AmmoBG, material, default);
				ammoWindowRect.yMin += 5f;
				for (int i = 0; i < allVehicleTurretDefs.Count; i++)
				{
					Rect potentialAmmoRect = new Rect(ammoWindowRect.x + ammoRect.height * (i % 6) + 5f, ammoWindowRect.y + ammoRect.height * Mathf.FloorToInt(i / 6), ammoRect.height, ammoRect.height);
					Graphics.DrawTexture(potentialAmmoRect, allVehicleTurretDefs[i].uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);
					if (Mouse.IsOver(potentialAmmoRect))
					{
						Graphics.DrawTexture(potentialAmmoRect, TexUI.HighlightTex);
					}
					if (Widgets.ButtonInvisible(potentialAmmoRect))
					{
						turret.ammoWindowOpened = false;
						turret.ReloadCannon(allVehicleTurretDefs[i], true);
						break;
					}
					string ammoCount = turret.vehicle.inventory.innerContainer.Where(td => td.def == allVehicleTurretDefs[i]).Select(t => t.stackCount).Sum().ToStringSafe();
					potentialAmmoRect.y += potentialAmmoRect.height / 2;
					potentialAmmoRect.x += potentialAmmoRect.width - Text.CalcSize(ammoCount).x;
					Widgets.Label(potentialAmmoRect, ammoCount);
				}
			}
			Rect fullRect = new Rect(rect);
			rect.height += ammoWindowRect.height + AmmoWindowOffset;
			rect.y -= (ammoWindowRect.height + AmmoWindowOffset);
			if (!Mouse.IsOver(rect))
			{
				turret.ammoWindowOpened = false;
			}

			Text.Font = GameFont.Small;
			if (flag2 && !turret.OnCooldown)
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
			if(haltFlag)
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
			if (flag3)
			{
				if (turret.turretDef.genericAmmo)
				{
					if(!turret.vehicle.inventory.innerContainer.Contains(turret.turretDef.ammunition.AllowedThingDefs.FirstOrDefault()))
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
					turret.ammoWindowOpened = !turret.ammoWindowOpened;
				}
			}
			if (flag4)
			{
				turret.TryRemoveShell();
				SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(turret.vehicle.Position, turret.vehicle.Map, false));
			}
			if (flag5)
			{
				turret.CycleFireMode();
			}
			if (flag6)
			{
				turret.SwitchAutoTarget();
			}
			if (flag)
			{
				return new GizmoResult(GizmoState.Mouseover, null);
			}
			return new GizmoResult(GizmoState.Clear, null);
		}
	}
}
