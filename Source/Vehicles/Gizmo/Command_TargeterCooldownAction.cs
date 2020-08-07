using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;


namespace Vehicles
{
    public class Command_TargeterCooldownAction : Command
    {
        public override void ProcessInput(Event ev)
        {
            if (cannon.reloadTicks <= 0)
            {
                if( (cannon.loadedAmmo is null || cannon.shellCount <= 0) && !cannon.cannonDef.ammoAllowed.NullOrEmpty())
                {
                    Messages.Message("NoAmmoLoadedCannon".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                base.ProcessInput(ev);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                HelperMethods.CannonTargeter.BeginTargeting(targetingParams, delegate(LocalTargetInfo target)
			    {
                    cannon.SetTarget(target);
                    cannon.ResetPrefireTimer();
			    }, cannon, null, null);
            }
        }

        public override float GetWidth(float maxWidth)
        {
            return 210f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Text.Font = GameFont.Tiny;
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoSize);
            bool flag = false;
            bool haltFlag = false;
            Rect gizmoRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(6f);
            Rect haltIconRect = new Rect(gizmoRect.x + gizmoRect.width / 2f, gizmoRect.y + gizmoRect.height / 2, gizmoRect.width / 2, gizmoRect.height / 2);

            Material material = (!disabled) ? null : TexUI.GrayscaleGUI;

            Texture2D badTex = icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            var gizmoColor = GUI.color;
            var ammoColor = GUI.color;
            var reloadColor = GUI.color;
            var fireModeColor = GUI.color;
            var autoTargetColor = GUI.color;

            Widgets.DrawWindowBackground(rect);
            Color haltColor = Color.white;
            if (Mouse.IsOver(gizmoRect))
            {
                if(cannon.cannonTarget.IsValid && Mouse.IsOver(haltIconRect))
                {
                    haltColor = GenUI.MouseoverColor;
                }
                else
                {
                    
                    if (!disabled)
                        GUI.color = GenUI.MouseoverColor;
                }
                flag = true;
                cannon.GizmoHighlighted = true;
            }
            else
            {
                cannon.GizmoHighlighted = false;
            }

            GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, material, default);
            MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
            GUI.color = IconDrawColor;
            Widgets.DrawTextureFitted(gizmoRect, badTex, iconDrawScale, iconProportions, iconTexCoords, iconAngle, material);
            GUI.color = gizmoColor;

            if(cannon.cannonTarget.IsValid)
            {
                GUI.color = haltColor;
                GenUI.DrawTextureWithMaterial(haltIconRect, TexCommandVehicles.HaltIcon, material, default);
                GUI.color = gizmoColor;
            }

            Rect ammoRect = new Rect(gizmoRect.x + gizmoRect.width + 7f, gizmoRect.y + 1, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
            Rect reloadRect = new Rect(ammoRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
            Rect fireModeRect = new Rect(reloadRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);
            Rect autoTargetRect = new Rect(fireModeRect.x + ammoRect.width, ammoRect.y, gizmoRect.width / 2, gizmoRect.height / 2 - 2);

            if (Mouse.IsOver(ammoRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            float widthHeight = gizmoRect.height / 2 - 2;
            GenUI.DrawTextureWithMaterial(ammoRect, BGTex, material, default);
            if(cannon.loadedAmmo != null)
            {
                alphaColorTicked.a = cannon.CannonIconAlphaTicked;
                Graphics.DrawTexture(ammoRect, cannon.loadedAmmo.uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);

                Rect ammoCountRect = new Rect(ammoRect);
                string ammoCount = cannon.pawn.inventory.innerContainer.Where(td => td.def == cannon.loadedAmmo).Select(t => t.stackCount).Sum().ToStringSafe();
                ammoCountRect.y += ammoCountRect.height / 2;
                ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
                Widgets.Label(ammoCountRect, ammoCount);
            }
            else if(cannon.cannonDef.genericAmmo && cannon.cannonDef.ammoAllowed.AnyNullified())
            {
                Graphics.DrawTexture(ammoRect, cannon.cannonDef.ammoAllowed.FirstOrDefault().uiIcon, material);

                Rect ammoCountRect = new Rect(ammoRect);
                string ammoCount = cannon.pawn.inventory.innerContainer.Where(td => td.def == cannon.cannonDef.ammoAllowed.FirstOrDefault()).Select(t => t.stackCount).Sum().ToStringSafe();
                ammoCountRect.y += ammoCountRect.height / 2;
                ammoCountRect.x += ammoCountRect.width - Text.CalcSize(ammoCount).x;
                Widgets.Label(ammoCountRect, ammoCount);
            }

            GUI.color = ammoColor;
            if (Mouse.IsOver(reloadRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            Graphics.DrawTexture(reloadRect, TexCommandVehicles.ReloadIcon, material);
            TooltipHandler.TipRegion(reloadRect, "ReloadCannonHandler".Translate());

            GUI.color = fireModeColor;
            if(Mouse.IsOver(fireModeRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            Graphics.DrawTexture(fireModeRect, cannon.CurrentFireMode.Icon, material);
            TooltipHandler.TipRegion(fireModeRect, cannon.CurrentFireMode.label);

            GUI.color = autoTargetColor;
            if(Mouse.IsOver(autoTargetRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            Graphics.DrawTexture(autoTargetRect, TexCommandVehicles.AutoTargetIcon, material);
            if(cannon.autoTargeting)
            {
                Rect checkboxRect = new Rect(autoTargetRect.x + autoTargetRect.width / 2, autoTargetRect.y + autoTargetRect.height / 2, autoTargetRect.width / 2, autoTargetRect.height / 2);
                Graphics.DrawTexture(checkboxRect, cannon.AutoTarget ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, material);
                TooltipHandler.TipRegion(autoTargetRect, "AutoTargeting".Translate(cannon.AutoTarget.ToString()));
            }
            

            GUI.color = reloadColor;
            Rect reloadBar = rect.ContractedBy(6f);
            reloadBar.yMin = rect.y + (rect.height / 2) + 1;
            reloadBar.xMin = ammoRect.x;
            Widgets.FillableBar(reloadBar, (float)cannon.shellCount / cannon.cannonDef.magazineCapacity, TexCommandVehicles.FullBarTex, TexCommandVehicles.EmptyBarTex, true);
            var font = Text.Font;
            var anchor = Text.Anchor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reloadBar, cannon.shellCount.ToString("F0") + " / " + cannon.cannonDef.magazineCapacity.ToString("F0"));
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
                if(cannon.cannonTarget.IsValid && Widgets.ButtonInvisible(haltIconRect, true))
                {
                    haltFlag = true;
                }
                else if (Widgets.ButtonInvisible(gizmoRect, true))
                {
                    flag2 = true;
                }
                if(Widgets.ButtonInvisible(reloadRect, true))
                {
                    flag3 = true;
                }
                if( (cannon.shellCount > 0) && Widgets.ButtonInvisible(ammoRect, true))
                {
                    flag4 = true;
                }
                if(Widgets.ButtonInvisible(fireModeRect, true))
                {
                    flag5 = true;
                }
                if(Widgets.ButtonInvisible(autoTargetRect, true))
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
                if (disabled && !disabledReason.NullOrEmpty())
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
            if(cannon.reloadTicks > 0)
            {
                float percent = cannon.reloadTicks / (float)cannon.MaxTicks;
                SPExtra.VerticalFillableBar(gizmoRect, percent, FillableBar, ClearBar);
            }
            if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
            {
                UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
            }
            Rect ammoWindowRect = new Rect(rect);
            ammoWindowRect.height = AmmoWindowOffset * 2;
            ammoWindowRect.width = ammoRect.height * 6 + AmmoWindowOffset * 2;
            if(cannon.ammoWindowOpened)
            {
                List<ThingDef> allCannonDefs = cannon.pawn.inventory.innerContainer.Where(d => cannon.ContainsAmmoDefOrShell(d.def)).Select(t => t.def).Distinct().ToList();
                ammoWindowRect.height += ammoRect.height + Mathf.CeilToInt(allCannonDefs.Count / 6) * ammoRect.height;
                ammoWindowRect.y -= (ammoWindowRect.height + AmmoWindowOffset);
                GenUI.DrawTextureWithMaterial(ammoWindowRect, TexCommandVehicles.AmmoBG, material, default);
                ammoWindowRect.yMin += 5f;
                for(int i = 0; i < allCannonDefs.Count; i++)
                {
                    Rect potentialAmmoRect = new Rect(ammoWindowRect.x + ammoRect.height * (i % 6) + 5f, ammoWindowRect.y + ammoRect.height * Mathf.FloorToInt(i / 6), ammoRect.height, ammoRect.height);
                    Graphics.DrawTexture(potentialAmmoRect, allCannonDefs[i].uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);
                    if(Mouse.IsOver(potentialAmmoRect))
                    {
                        Graphics.DrawTexture(potentialAmmoRect, TexUI.HighlightTex);
                    }
                    if(Widgets.ButtonInvisible(potentialAmmoRect))
                    {
                        cannon.ammoWindowOpened = false;
                        cannon.ReloadCannon(allCannonDefs[i], true);
                        break;
                    }
                    string ammoCount = cannon.pawn.inventory.innerContainer.Where(td => td.def == allCannonDefs[i]).Select(t => t.stackCount).Sum().ToStringSafe();
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
                cannon.ammoWindowOpened = false;
            }

            Text.Font = GameFont.Small;
            if (flag2)
            {
                if (disabled)
                {
                    if (!disabledReason.NullOrEmpty())
                        Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
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
                        Messages.Message(disabledReason, MessageTypeDefOf.RejectInput, false);
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                cannon.SetTarget(LocalTargetInfo.Invalid);
            }
            if(flag3)
            {
                if(cannon.cannonDef.genericAmmo)
                {
                    if(!cannon.pawn.inventory.innerContainer.Contains(cannon.cannonDef.ammoAllowed.FirstOrDefault()))
                    {
                        Messages.Message("NoAmmoAvailable".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        cannon.ReloadCannon(cannon.cannonDef.ammoAllowed.FirstOrDefault());
                    }
                }
                else
                {
                    cannon.ammoWindowOpened = !cannon.ammoWindowOpened;
                }
            }
            if(flag4)
            {
                cannon.TryRemoveShell();
                SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(cannon.pawn.Position, cannon.pawn.Map, false));
            }
            if(flag5)
            {
                cannon.CycleFireMode();
            }
            if(flag6)
            {
                cannon.SwitchAutoTarget();
            }
            if (flag)
                return new GizmoResult(GizmoState.Mouseover, null);
            return new GizmoResult(GizmoState.Clear, null);
        }

        public TargetingParameters targetingParams;
        public CannonHandler cannon;
        private const float GizmoSize = 75f;
        private const float AmmoWindowOffset = 5f;
        private const float UnloadIconScale = 1.25f;
        private readonly Texture2D FillableBar = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.25f);
        private readonly Texture2D ClearBar = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        private Color alphaColorTicked = new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, 0.5f);
    }
}
