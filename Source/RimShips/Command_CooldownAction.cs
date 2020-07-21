using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles
{
    public class Command_CooldownAction : Command_Action
    {
        public override void ProcessInput(Event ev)
        {
            if (cannon.ActivateTimer())
                base.ProcessInput(ev);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
        {
            Text.Font = GameFont.Tiny;
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoSize);
            bool flag = false;
            if (Mouse.IsOver(rect))
            {
                flag = true;
                if (!disabled)
                    GUI.color = GenUI.MouseoverColor;
            }
            Texture2D badTex = icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            Material material = (!disabled) ? null : TexUI.GrayscaleGUI;
            Rect ammoRect = new Rect(rect.x + (rect.width), rect.y + (rect.height), rect.width / 10, rect.height / 10);
            GenUI.DrawTextureWithMaterial(rect, BGTex, material, default);
            if (!cannon.cannonDef.ammoAllowed.NullOrEmpty())
            {
                GenUI.DrawTextureWithMaterial(ammoRect, BGTex, material, default);
            }
            MouseoverSounds.DoRegion(rect, SoundDefOf.Mouseover_Command);
            Rect outerRect = rect;
            outerRect.position += new Vector2(iconOffset.x * outerRect.size.x, iconOffset.y * outerRect.size.y);
            GUI.color = IconDrawColor;
            Widgets.DrawTextureFitted(outerRect, badTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords, iconAngle, material);
            GUI.color = Color.white;
            bool flag2 = false;
            KeyCode keyCode = (hotKey != null) ? hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Rect rect2 = new Rect(rect.x, rect.y, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    flag2 = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false))
            {
                flag2 = true;
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
                TooltipHandler.TipRegion(rect, tip);
            }
            if(cannon.reloadTicks > 0)
            {
                float percent = cannon.reloadTicks / (float)cannon.MaxTicks;
                SPExtra.VerticalFillableBar(rect, percent, FillableBar, ClearBar);
            }
            if (!HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, HighlightTag);
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
