using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SPExtended;

namespace Vehicles
{
    public class Command_TargeterCooldownAction : Command
    {
        public override void ProcessInput(Event ev)
        {
            if (cannon.cooldownTicks <= 0)
            {
                if( (cannon.loadedAmmo is null || cannon.shellCount == 0) && !cannon.cannonDef.ammoAllowed.NullOrEmpty())
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
            return cannon.cannonDef.ammoAllowed.NullOrEmpty() ? 75 : 140;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Text.Font = GameFont.Tiny;
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoSize);
            bool flag = false;

            Rect gizmoRect = new Rect(rect.x + 3.5f, rect.y, rect.width / 2, rect.height);

            Material material = (!disabled) ? null : TexUI.GrayscaleGUI;
            if(cannon.cannonDef.ammoAllowed.NullOrEmpty())
            {
                gizmoRect = rect;
                gizmoRect.width = 75f;
            }
            else
            {
                gizmoRect = gizmoRect.ContractedBy(7);
                GenUI.DrawTextureWithMaterial(rect, TexCommandVehicles.AmmoBG, material, default);
            }

            Texture2D badTex = icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            var gizmoColor = GUI.color;
            var ammoColor = GUI.color;
            var reloadColor = GUI.color;
            
            Rect ammoRect = new Rect(gizmoRect.x + gizmoRect.width + 7, gizmoRect.y, gizmoRect.width, (gizmoRect.height / 2) - 3.5f);
            Rect reloadRect = new Rect(gizmoRect.x + gizmoRect.width + 7, ammoRect.y + ammoRect.height + 7, gizmoRect.width, (gizmoRect.height / 2) - 3.5f);

            if (Mouse.IsOver(gizmoRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, material, default);
            GUI.color = gizmoColor;
            if (!cannon.cannonDef.ammoAllowed.NullOrEmpty())
            {
                MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
                MouseoverSounds.DoRegion(ammoRect, SoundDefOf.Mouseover_Command);
                MouseoverSounds.DoRegion(reloadRect, SoundDefOf.Mouseover_Command);

                if (Mouse.IsOver(ammoRect))
                {
                    flag = true;
                    if (!disabled)
                        GUI.color = GenUI.MouseoverColor;
                }
                GenUI.DrawTextureWithMaterial(ammoRect, BGTex, material, default);
                float widthHeight = ammoRect.height;
                Rect ammoIconRect = new Rect(ammoRect.x + (ammoRect.width / 2) - (widthHeight / 2), ammoRect.y + (ammoRect.height / 2) - (widthHeight / 2), widthHeight, widthHeight);
                if(cannon.loadedAmmo != null)
                {
                    alphaColorTicked.a = cannon.CannonIconAlphaTicked;
                    Graphics.DrawTexture(ammoIconRect, cannon.loadedAmmo.uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);
                }
                else
                {
                    Graphics.DrawTexture(ammoIconRect, TexCommandVehicles.MissingAmmoIcon);
                }
                GUI.color = ammoColor;
                if (Mouse.IsOver(reloadRect))
                {
                    flag = true;
                    if (!disabled)
                        GUI.color = GenUI.MouseoverColor;
                }
                GenUI.DrawTextureWithMaterial(reloadRect, BGTex, material, default);
                GUI.color = reloadColor;
                Rect reloadLabel = new Rect(reloadRect.x + 10, reloadRect.y + reloadRect.height / 4, reloadRect.width - 10, reloadRect.height / 1.5f);
                string buttonTextLoad = (cannon.loadedAmmo is null || cannon.cooldownTicks > 0) ? string.Empty : "ExtractCannon".Translate().ToString();
                Widgets.Label(reloadLabel, buttonTextLoad);
            }
            GUI.color = IconDrawColor;
            Widgets.DrawTextureFitted(gizmoRect, badTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords, iconAngle, material);
            GUI.color = Color.white;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
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
            if (Widgets.ButtonImageWithBG(gizmoRect, TexCommandVehicles.AmmoBG))
            {
                flag2 = true;
            }
            if(Widgets.ButtonInvisible(ammoRect, false))
            {
                flag3 = true;
            }
            if(Widgets.ButtonInvisible(reloadRect, false))
            {
                flag4 = true;
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
            if(cannon.cooldownTicks > 0)
            {
                float percent = cannon.cooldownTicks / (float)cannon.MaxTicks;
                SPExtra.VerticalFillableBar(gizmoRect, percent, FillableBar, ClearBar);
            }
            if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(gizmoRect)))
            {
                UIHighlighter.HighlightOpportunity(gizmoRect, HighlightTag);
            }
            Rect ammoWindowRect = new Rect(rect);
            ammoWindowRect.height = 0f;
            if(cannon.ammoWindowOpened)
            {
                List<ThingDef> allCannonDefs = cannon.pawn.inventory.innerContainer.Where(x => cannon.cannonDef.ammoAllowed.Contains(x.def)).Select(y => y.def).Distinct().ToList();
                if ((allCannonDefs?.Count ?? 0) > 0)
                {
                    ammoWindowRect.height += ammoRect.height + Mathf.CeilToInt(allCannonDefs.Count / 7) * ammoRect.height;
                    ammoWindowRect.y -= ammoWindowRect.height;
                    GenUI.DrawTextureWithMaterial(ammoWindowRect, TexCommandVehicles.AmmoBG, material, default);
                    for(int i = 0; i < allCannonDefs.Count; i++)
                    {
                        Rect potentialAmmoRect = new Rect(ammoWindowRect.x + 5f + ammoRect.height * i, ammoWindowRect.y + 1f, ammoRect.height, ammoRect.height);
                        Graphics.DrawTexture(potentialAmmoRect, allCannonDefs[i].uiIcon, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, alphaColorTicked, material);
                        if(Mouse.IsOver(potentialAmmoRect))
                        {
                            Graphics.DrawTexture(potentialAmmoRect, TexUI.HighlightTex);
                        }
                        if(Widgets.ButtonInvisible(potentialAmmoRect))
                        {
                            cannon.ammoWindowOpened = false;
                            cannon.ReloadCannon(allCannonDefs[i]);
                            break;
                        }
                    }
                }
            }
            Rect fullRect = new Rect(rect);
            fullRect.y -= ammoWindowRect.height;
            fullRect.height += ammoWindowRect.height;
            if(!Mouse.IsOver(fullRect))
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
            if(flag3 && !cannon.cannonDef.ammoAllowed.NullOrEmpty())
            {
                cannon.ammoWindowOpened = !cannon.ammoWindowOpened;
            }
            if(flag4)
            {
                cannon.TryRemoveShell();
            }
            if (flag)
                return new GizmoResult(GizmoState.Mouseover, null);
            return new GizmoResult(GizmoState.Clear, null);
        }

        public TargetingParameters targetingParams;
        public CannonHandler cannon;
        public CompCannons comp;
        private const float GizmoSize = 75f;
        private readonly Texture2D FillableBar = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.25f);
        private readonly Texture2D ClearBar = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        private Color alphaColorTicked = new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, 0.5f);
    }
}
