using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using SPExtendedLibrary;

namespace RimShips
{
    public class Command_CooldownAction : Command_Action
    {
        public override void ProcessInput(Event ev)
        {
            if (this.cannon.ActivateTimer())
                base.ProcessInput(ev);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Text.Font = GameFont.Tiny;
            Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), GizmoSize);
            bool flag = false;
            if (Mouse.IsOver(rect))
            {
                flag = true;
                if (!this.disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            Texture2D badTex = this.icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            Material material = (!this.disabled) ? null : TexUI.GrayscaleGUI;
            GenUI.DrawTextureWithMaterial(rect, Command.BGTex, material, default(Rect));
            MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
            Rect outerRect = rect;
            outerRect.position += new Vector2(this.iconOffset.x * outerRect.size.x, this.iconOffset.y * outerRect.size.y);
            GUI.color = this.IconDrawColor;
            Widgets.DrawTextureFitted(outerRect, badTex, this.iconDrawScale * 0.85f, this.iconProportions, this.iconTexCoords, this.iconAngle, material);
            GUI.color = Color.white;
            bool flag2 = false;
            KeyCode keyCode = (this.hotKey != null) ? this.hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (this.hotKey.KeyDownEvent)
                {
                    flag2 = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false))
            {
                flag2 = true;
            }
            string labelCap = this.LabelCap;
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
            if (this.DoTooltip)
            {
                TipSignal tip = this.Desc;
                if (this.disabled && !this.disabledReason.NullOrEmpty())
                {
                    string text = tip.text;
                    tip.text = string.Concat(new string[]
                    {
                text,
                "\n\n",
                "DisabledCommand".Translate(),
                ": ",
                this.disabledReason
                    });
                }
                TooltipHandler.TipRegion(rect, tip);
            }
            if(this.cannon.cooldownTicks > 0)
            {
                float percent = (float)this.cannon.cooldownTicks / (float)this.cannon.MaxTicks;
                SPExtended.VerticalFillableBar(rect, percent, FillableBar, ClearBar);
            }
            if (!this.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, this.HighlightTag);
            }
            Text.Font = GameFont.Small;
            if (flag2)
            {
                if (this.disabled)
                {
                    if (!this.disabledReason.NullOrEmpty())
                        Messages.Message(this.disabledReason, MessageTypeDefOf.RejectInput, false);
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                if (!TutorSystem.AllowAction(this.TutorTagSelect))
                {
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                var result = new GizmoResult(GizmoState.Interacted, Event.current);
                TutorSystem.Notify_Event(this.TutorTagSelect);
                return result;
            }
            if (flag)
                return new GizmoResult(GizmoState.Mouseover, null);
            return new GizmoResult(GizmoState.Clear, null);
        }

        public CannonHandler cannon;
        public CompCannons comp;
        private const float GizmoSize = 75f;
        private readonly Texture2D FillableBar = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.25f);
        private readonly Texture2D ClearBar = SolidColorMaterials.NewSolidColorTexture(Color.clear);
    }
}
