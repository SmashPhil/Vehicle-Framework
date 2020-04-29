using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SPExtended;

namespace RimShips
{
    public class Command_TargeterCooldownAction : Command
    {
        public override void ProcessInput(Event ev)
        {
            if (cannon.cooldownTicks <= 0/* && cannon.loadedAmmo != null*/)
            {
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
            return 140;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Text.Font = GameFont.Tiny;
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoSize);
            bool flag = false;

            Rect gizmoRect = new Rect(rect.x + 3.5f, rect.y, rect.width / 2, rect.height).ContractedBy(7);
            Texture2D badTex = icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            var gizmoColor = GUI.color;
            var ammoColor = GUI.color;
            var reloadColor = GUI.color;
            
            
            Material material = (!disabled) ? null : TexUI.GrayscaleGUI;
            GenUI.DrawTextureWithMaterial(rect, AmmoBG, material, default);
            Rect ammoRect = new Rect(gizmoRect.x + gizmoRect.width + 7, gizmoRect.y, gizmoRect.width, (gizmoRect.height / 2) - 3.5f);
            Rect reloadRect = new Rect(gizmoRect.x + gizmoRect.width + 7, ammoRect.y + ammoRect.height + 7, gizmoRect.width, (gizmoRect.height / 2) - 3.5f);

            MouseoverSounds.DoRegion(gizmoRect, SoundDefOf.Mouseover_Command);
            MouseoverSounds.DoRegion(ammoRect, SoundDefOf.Mouseover_Command);
            MouseoverSounds.DoRegion(reloadRect, SoundDefOf.Mouseover_Command);

            if (Mouse.IsOver(gizmoRect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            GenUI.DrawTextureWithMaterial(gizmoRect, BGTex, material, default);
            GUI.color = gizmoColor;
            if (cannon.cannonDef.ammoAllowed?.Any() ?? false)
            {
                if (Mouse.IsOver(ammoRect))
                {
                    flag = true;
                    if (!disabled)
                        GUI.color = GenUI.MouseoverColor;
                }
                GenUI.DrawTextureWithMaterial(ammoRect, BGTex, material, default);
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
                Widgets.Label(reloadLabel, "Extract".Translate());
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
                Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    flag2 = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(gizmoRect, false))
            {
                flag2 = true;
            }
            if(Widgets.ButtonInvisible(ammoRect, false))
            {
                flag3 = true;
            }
            if(Widgets.ButtonInvisible(reloadRect, false))
            {
                flag4 = false;
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
            if(flag3)
            {
                //Change later
                cannon.ReloadCannon(cannon.pawn.inventory.innerContainer.FirstOrDefault(x => cannon.cannonDef.ammoAllowed.Contains(x.def)).def);
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
        private static readonly Texture2D AmmoBG = ContentFinder<Texture2D>.Get("UI/GizmoGrid/AmmoBoxBG", true);
    }
}
