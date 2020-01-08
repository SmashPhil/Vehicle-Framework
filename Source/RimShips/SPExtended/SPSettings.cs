using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SRTS
{
    [StaticConstructorOnStartup]
    public static class DialogSettings
    {
        public static Color textColor = Color.white;
        public static Color highlightColor = new Color(0f, 0f, 0f, 0.25f);
        public static void Settings_IntegerBox(this Listing_Standard lister, string text, ref int value, float labelLength, float padding, int min = int.MinValue, int max = int.MaxValue)
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(Text.LineHeight);

            Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
            Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);

            Color color = GUI.color;
            Widgets.Label(rectLeft, text);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
            string buffer = value.ToString();
            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
        }

        public static void Settings_Numericbox(this Listing_Standard lister, string text, ref float value, float labelLength, float padding, float min = -1E+09f, float max = 1E+09f)
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(Text.LineHeight);

            Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
            Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);

            Color color = GUI.color;
            Widgets.Label(rectLeft, text);

            var align = Text.CurTextFieldStyle.alignment;
            Text.CurTextFieldStyle.alignment = TextAnchor.MiddleLeft;
            string buffer = value.ToString();
            Widgets.TextFieldNumeric<float>(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
        }

        public static void Settings_SliderLabeled(this Listing_Standard lister, string label, string endSymbol, ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2, float endValue = -1f, string endValueDisplay = "")
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(24f);
            string format = string.Format("{0}" + endSymbol, Math.Round(value * multiplier, decimalPlaces));
            if (!endValueDisplay.NullOrEmpty() && endValue > 0)
                if(value >= endValue)
                    format = endValueDisplay;
            value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            if(endValue > 0 && value >= max)
                value = endValue;
        }

        public static void Settings_SliderLabeled(this Listing_Standard lister, string label, string endSymbol, ref int value, int min, int max, int endValue = -1, string endValueDisplay = "")
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(24f);
            string format = string.Format("{0}" + endSymbol, value);
            if(!endValueDisplay.NullOrEmpty() && endValue > 0)
                if(value == endValue)
                    format = endValueDisplay;
            value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            if(endValue > 0 && value == max)
                value = endValue;
        }

        public static void Settings_Header(this Listing_Standard lister, string header, Color highlight, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var textSize = Text.Font;
            Text.Font = fontSize;

            Rect rect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = textColor;

            var anchorTmp = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, header);
            Text.Font = textSize;   
            Text.Anchor = anchorTmp;
            lister.Gap(12f);
        }

        public static bool Settings_Button(this Listing_Standard lister, string label, Rect rect, Color customColor, bool background = true, bool active = true)
        {
            var anchor = Text.Anchor;
            Color color = GUI.color;
            
            if(background)
            {
                Texture2D atlas = ButtonBGAtlas;
                if(Mouse.IsOver(rect))
                {
                    atlas = ButtonBGAtlasMouseover;
                    if(Input.GetMouseButton(0))
                    {
                        atlas = ButtonBGAtlasClick;
                    }
                }
                Widgets.DrawAtlas(rect, atlas);
            }
            else
            {
                GUI.color = customColor;
                if(Mouse.IsOver(rect))
                    GUI.color = Color.cyan;
            }
            if(background)
                Text.Anchor = TextAnchor.MiddleCenter;
            else
                Text.Anchor = TextAnchor.MiddleLeft;
            bool wordWrap = Text.WordWrap;
            if (rect.height < Text.LineHeight * 2f)
                Text.WordWrap = false;
            Widgets.Label(rect, label);
            Text.Anchor = anchor;
            GUI.color = color;
            Text.WordWrap = wordWrap;
            lister.Gap(2f);
            return Widgets.ButtonInvisible(rect, false);
        }

        public static bool Settings_ButtonLabeled(this Listing_Standard lister, string header, string buttonLabel, Color highlightColor, float buttonWidth = 30f, bool background = true, bool active = true)
        {
            var anchor = Text.Anchor;
            Color color = GUI.color;
            Rect rect = lister.GetRect(20f);
            Rect buttonRect = new Rect(rect.width - buttonWidth, rect.y, buttonWidth, rect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, header);

            if(background)
            {
                Texture2D atlas = ButtonBGAtlas;
                if (Mouse.IsOver(buttonRect))
                {
                    atlas = ButtonBGAtlasMouseover;
                    if (Input.GetMouseButton(0))
                    {
                        atlas = ButtonBGAtlasClick;
                    }
                }
                Widgets.DrawAtlas(buttonRect, atlas);
            }
            else
            {
                GUI.color = Color.white;
                if(Mouse.IsOver(buttonRect))
                    GUI.color = highlightColor;
            }
            if (background)
                Text.Anchor = TextAnchor.MiddleCenter;
            else
                Text.Anchor = TextAnchor.MiddleRight;
            bool wordWrap = Text.WordWrap;
            if(buttonRect.height < Text.LineHeight * 2f)
                Text.WordWrap = false;

            Widgets.Label(buttonRect, buttonLabel);
            Text.Anchor = anchor;
            GUI.color = color;
            Text.WordWrap = wordWrap;
            lister.Gap(2f);
            return Widgets.ButtonInvisible(buttonRect, false);
        }

        public static void Draw_Label(Rect rect, string label, Color highlight, Color textColor, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var textSize = Text.Font;
            Text.Font = fontSize;
            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = textColor;

            var anchorTmp = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, label);
            Text.Font = textSize;
            Text.Anchor = anchorTmp;
        }

        public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG", true);

        public static readonly Texture2D ButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover", true);

        public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick", true);

        public static readonly Texture2D LightHighlight = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.04f));
    }

    public class Dialog_ResearchChange : Window
    {
        public Dialog_ResearchChange()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.projectsSearched = new List<ResearchProjectDef>();
            this.addedResearch = false;
            charCount = 0;
        }

        public override Vector2 InitialSize => new Vector2(200f, 350f);

        public override void DoWindowContents(Rect inRect)
        {
            Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Rect searchBarRect = new Rect(inRect.x, labelRect.y + 24f, inRect.width, 24f);
            Widgets.Label(labelRect, "SearchResearch".Translate());
            charCount = researchString?.Count() ?? 0;
            researchString = Widgets.TextArea(searchBarRect, researchString);
            
            if(researchString.Count() != charCount || researchChanged)
            {
                projectsSearched = DefDatabase<ResearchProjectDef>.AllDefs.Where(x => !SRTSMod.mod.props.ResearchPrerequisites.Contains(x) && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, researchString, CompareOptions.IgnoreCase) >= 0).ToList();
                researchChanged = false;
            }
            
            for(int i = 0; i < projectsSearched.Count; i++)
            {
                ResearchProjectDef proj = projectsSearched[i];
                Rect projectRect = new Rect(inRect.x, searchBarRect.y + 24 + (24*i), inRect.width, 30f);
                if(Widgets.ButtonText(projectRect, proj.defName, false, false, true) && !SRTSMod.mod.props.ResearchPrerequisites.Contains(proj))
                {
                    addedResearch = true;
                    researchChanged = true;
                    SRTSMod.mod.props.AddCustomResearch(proj);
                }
            }
        }

        public override void PreClose()
        {
            /*if (addedResearch)
                Messages.Message("RestartGameResearch".Translate(), MessageTypeDefOf.CautionInput, false);*/ //Uncomment to send message to restart game if research has been changed
        }

        bool addedResearch;

        bool researchChanged;

        private string researchString;

        int charCount;

        private List<ResearchProjectDef> projectsSearched;
    }
}
