using System.Linq;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class Command_CooldownAction : Command
	{
		protected const float AmmoWindowOffset = 5f;

		protected readonly Color DarkGrey = new Color(0.05f, 0.05f, 0.05f, 0.5f);

		public TargetingParameters targetingParams;
		public List<VehicleTurret> turrets = new List<VehicleTurret>();

		protected Color alphaColorTicked = new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, 0.5f);

		protected bool canReload;

		public Command_CooldownAction()
		{
			
		}

		
		public virtual void PostVariablesInit()
		{
			canReload = turrets.All(t => t.turretDef.ammunition != null);
		}

		public override void ProcessInput(Event ev)
		{
			if (turrets.All(t => t.reloadTicks <= 0))
			{
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
				base.ProcessInput(ev);
				foreach (VehicleTurret turret in turrets)
				{
					Vector3 target = turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
					turret.SetTarget(target.ToIntVec3());
					turret.PushTurretToQueue();
					turret.ResetPrefireTimer();
				}
			}
		}

		public override float GetWidth(float maxWidth)
		{
			return turrets.All(t => t.turretDef.cooldown != null) ? 279 : 210f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Text.Font = GameFont.Tiny;
			bool flag = false;
			bool haltFlag = false;
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Rect gizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(6f);
			Rect indicatorIconRect = new Rect(gizmoRect.x + gizmoRect.width / 2f, gizmoRect.y + gizmoRect.height / 2, gizmoRect.width / 2, gizmoRect.height / 2);

			Material material = (!disabled) ? null : TexUI.GrayscaleGUI;
			Material cooldownMat = turrets.Any(t => t.OnCooldown) ? TexUI.GrayscaleGUI : material;
			
			var gizmoColor = GUI.color;
			var ammoColor = GUI.color;
			var reloadColor = GUI.color;
			var fireModeColor = GUI.color;
			var autoTargetColor = GUI.color;

			bool ammoLoaded = true;

			Widgets.DrawWindowBackground(rect);
			Color haltColor = Color.white;
			if (turrets.Any(t => t.loadedAmmo is null || turrets.Any(t => t.shellCount <= 0)) && canReload)
			{
				disabledReason += "NoAmmoLoadedCannon".Translate();
				ammoLoaded = false;
			}
			else if (Mouse.IsOver(gizmoRect))
			{
				if (turrets.FirstOrDefault().cannonTarget.IsValid && Mouse.IsOver(indicatorIconRect))
				{
					haltColor = GenUI.MouseoverColor;
				}
				else
				{
					if (!disabled && !turrets.Any(t => t.OnCooldown))
					{
						GUI.color = GenUI.MouseoverColor;
					}
				}
				flag = true;
				turrets.ForEach(t => t.GizmoHighlighted = true);
			}
			else
			{
				turrets.ForEach(t => t.GizmoHighlighted = false);
			}

			GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, cooldownMat, default);
			MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
			GUI.color = IconDrawColor;
			Widgets.DrawTextureFitted(gizmoRect, icon, iconDrawScale, iconProportions, iconTexCoords, iconAngle, turrets.FirstOrDefault().CannonMaterial);
			if (!ammoLoaded)
			{
				Widgets.DrawBoxSolid(gizmoRect, DarkGrey);
			}
			GUI.color = gizmoColor;

			if (turrets.Any(t => t.OnCooldown))
			{
				GUI.color = haltColor;
				GenUI.DrawTextureWithMaterial(indicatorIconRect, turrets.FirstOrDefault().FireIcon, CompFireOverlay.FireGraphic.MatAt(Rot4.North));
				GUI.color = gizmoColor;
			}
			else if (turrets.FirstOrDefault().cannonTarget.IsValid)
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
			
			float heatPoints = turrets.Average(t => t.currentHeatRate);
			UIElements.VerticalFillableBar(cooldownRect, heatPoints / VehicleTurret.MaxHeatCapacity, 
				turrets.Any(t => t.OnCooldown) ? TexData.RedTex : TexData.HeatColorPercent(heatPoints / VehicleTurret.MaxHeatCapacity), VehicleTex.EmptyBarTex, true);

			if (canReload && Mouse.IsOver(ammoRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			float widthHeight = gizmoRect.height / 2 - 2;
			GenUI.DrawTextureWithMaterial(ammoRect, BGTex, canReload ? material : TexUI.GrayscaleGUI, default);
			if (turrets.Any(t => t.loadedAmmo != null))
			{
				alphaColorTicked.a = turrets.Max(t => t.CannonIconAlphaTicked);
				Graphics.DrawTexture(ammoRect, turrets.FirstOrDefault().loadedAmmo.uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);

				Rect ammoCountRect = new Rect(ammoRect);
				string ammoCount = turrets.FirstOrDefault().vehicle.inventory.innerContainer.Where(td => td.def == turrets.FirstOrDefault().loadedAmmo).Select(t => t.stackCount).Sum().ToStringSafe();
				ammoCountRect.y += ammoCountRect.height / 2;
				ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
				Widgets.Label(ammoCountRect, ammoCount);
			}
			else if (turrets.FirstOrDefault().turretDef.genericAmmo)
			{
				Graphics.DrawTexture(ammoRect, turrets.FirstOrDefault().turretDef.ammunition.AllowedThingDefs.FirstOrDefault().uiIcon, material);

				Rect ammoCountRect = new Rect(ammoRect);
				string ammoCount = turrets.FirstOrDefault().vehicle.inventory.innerContainer.Where(td => td.def == turrets.FirstOrDefault().turretDef.ammunition.AllowedThingDefs.FirstOrDefault()).Select(t => t.stackCount).Sum().ToStringSafe();
				ammoCountRect.y += ammoCountRect.height / 2;
				ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
				Widgets.Label(ammoCountRect, ammoCount);
			}

			GUI.color = ammoColor;
			if (canReload && Mouse.IsOver(reloadRect))
			{
				flag = true;
				if (!disabled)
				{
					GUI.color = GenUI.MouseoverColor;
				}
			}
			Graphics.DrawTexture(reloadRect, VehicleTex.ReloadIcon, canReload ? material : TexUI.GrayscaleGUI);
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
			Graphics.DrawTexture(fireModeRect, turrets.FirstOrDefault().CurrentFireMode.Icon, material);
			TooltipHandler.TipRegion(fireModeRect, turrets.FirstOrDefault().CurrentFireMode.label);

			GUI.color = autoTargetColor;
			if (!turrets.FirstOrDefault().CanAutoTarget)
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
			Graphics.DrawTexture(autoTargetRect, VehicleTex.AutoTargetIcon, null);
			Rect checkboxRect = new Rect(autoTargetRect.x + autoTargetRect.width / 2, autoTargetRect.y + autoTargetRect.height / 2, autoTargetRect.width / 2, autoTargetRect.height / 2);
			GUI.DrawTexture(checkboxRect, turrets.FirstOrDefault().AutoTarget ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex);//cannon.autoTargeting ? material : DisabledCBMat
			TooltipHandler.TipRegion(autoTargetRect, "AutoTargeting".Translate(turrets.FirstOrDefault().AutoTarget.ToString()));

			GUI.color = reloadColor;
			Rect reloadBar = rect.ContractedBy(6f);
			reloadBar.yMin = rect.y + (rect.height / 2) + 1;
			reloadBar.xMin = ammoRect.x;
			Widgets.FillableBar(reloadBar, (float)turrets.FirstOrDefault().shellCount / turrets.FirstOrDefault().turretDef.magazineCapacity, VehicleTex.FullBarTex, VehicleTex.EmptyBarTex, true);
			var font = Text.Font;
			var anchor = Text.Anchor;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			string ammoCountLabel = string.Format("{0} / {1}", turrets.FirstOrDefault().shellCount.ToString("F0"), turrets.FirstOrDefault().turretDef.magazineCapacity.ToString("F0"));
			if (turrets.FirstOrDefault().turretDef.magazineCapacity <= 0)
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
				if (turrets.Any(t => t.cannonTarget.IsValid) && Widgets.ButtonInvisible(indicatorIconRect, true))
				{
					haltFlag = true;
				}
				else if (ammoLoaded && Widgets.ButtonInvisible(gizmoRect, true))
				{
					flag2 = true;
				}
				if (canReload && Widgets.ButtonInvisible(reloadRect, true))
				{
					flag3 = true;
				}
				if (canReload && turrets.FirstOrDefault().shellCount > 0 && Widgets.ButtonInvisible(ammoRect, true))
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
			if (turrets.Any(t => t.reloadTicks > 0))
			{
				float percent = turrets.Max(t => t.reloadTicks) / (float)turrets.FirstOrDefault().MaxTicks;
				UIElements.VerticalFillableBar(gizmoRect, percent, UIData.FillableBarTexture, UIData.ClearBarTexture);
			}
			if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
			{
				UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
			}
			Rect ammoWindowRect = new Rect(rect);
			ammoWindowRect.height = AmmoWindowOffset * 2;
			ammoWindowRect.width = ammoRect.height * 6 + AmmoWindowOffset * 2;
			if (turrets.FirstOrDefault().AmmoWindowOpened)
			{
				List<ThingDef> allVehicleTurretDefs = turrets.FirstOrDefault().vehicle.inventory.innerContainer.Where(d => turrets.FirstOrDefault().ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
				ammoWindowRect.height += ammoRect.height + Mathf.CeilToInt(allVehicleTurretDefs.Count / 6) * ammoRect.height;
				ammoWindowRect.y -= (ammoWindowRect.height + AmmoWindowOffset);
				GenUI.DrawTextureWithMaterial(ammoWindowRect, VehicleTex.AmmoBG, material, default);
				ammoWindowRect.yMin += 5f;
				for(int i = 0; i < allVehicleTurretDefs.Count; i++)
				{
					Rect potentialAmmoRect = new Rect(ammoWindowRect.x + ammoRect.height * (i % 6) + 5f, ammoWindowRect.y + ammoRect.height * Mathf.FloorToInt(i / 6), ammoRect.height, ammoRect.height);
					Graphics.DrawTexture(potentialAmmoRect, allVehicleTurretDefs[i].uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);
					if(Mouse.IsOver(potentialAmmoRect))
					{
						Graphics.DrawTexture(potentialAmmoRect, TexUI.HighlightTex);
					}
					if(Widgets.ButtonInvisible(potentialAmmoRect))
					{
						turrets.FirstOrDefault().AmmoWindowOpened = false;
						turrets.ForEach(t => t.ReloadCannon(allVehicleTurretDefs[i], true));
						break;
					}
					string ammoCount = turrets.FirstOrDefault().vehicle.inventory.innerContainer.Where(td => td.def == allVehicleTurretDefs[i]).Select(t => t.stackCount).Sum().ToStringSafe();
					potentialAmmoRect.y += potentialAmmoRect.height / 2;
					potentialAmmoRect.x += potentialAmmoRect.width - Text.CalcSize(ammoCount).x;
					Widgets.Label(potentialAmmoRect, ammoCount);
				}
			}
			Rect fullRect = new Rect(rect);
			rect.height += ammoWindowRect.height + AmmoWindowOffset;
			rect.y -= (ammoWindowRect.height + AmmoWindowOffset);
			if(!Mouse.IsOver(rect))
			{
				turrets.FirstOrDefault().AmmoWindowOpened = false;
			}

			Text.Font = GameFont.Small;
			if (flag2 && !turrets.Any(t => t.OnCooldown))
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
				turrets.ForEach(t => t.SetTarget(LocalTargetInfo.Invalid));
			}
			if (flag3)
			{
				if (turrets.FirstOrDefault().turretDef.genericAmmo)
				{
					if (!turrets.FirstOrDefault().vehicle.inventory.innerContainer.Contains(turrets.FirstOrDefault().turretDef.ammunition.AllowedThingDefs.FirstOrDefault()))
					{
						Messages.Message("NoAmmoAvailable".Translate(), MessageTypeDefOf.RejectInput);
					}
					else
					{
						turrets.ForEach(t => t.ReloadCannon(turrets.FirstOrDefault().turretDef.ammunition.AllowedThingDefs.FirstOrDefault()));
					}
				}
				else
				{
					turrets.FirstOrDefault().AmmoWindowOpened = !turrets.FirstOrDefault().AmmoWindowOpened;
				}
			}
			if (flag4)
			{
				turrets.ForEach(t => t.TryRemoveShell());
				SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(turrets.FirstOrDefault().vehicle.Position, turrets.FirstOrDefault().vehicle.Map, false));
			}
			if (flag5)
			{
				turrets.ForEach(t => t.CycleFireMode());
			}
			if(flag6)
			{
				turrets.ForEach(t => t.SwitchAutoTarget());
			}
			if (flag)
			{
				return new GizmoResult(GizmoState.Mouseover, null);
			}
			return new GizmoResult(GizmoState.Clear, null);
		}
	}
}
