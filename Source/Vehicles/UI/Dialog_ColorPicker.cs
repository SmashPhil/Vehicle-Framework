using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using static Vehicles.SPExtra;
using UnityEngine;
using RimWorld;
using HarmonyLib;
using System.Text.RegularExpressions;

namespace Vehicles.UI
{
    [StaticConstructorOnStartup]
    public class Dialog_ColorPicker : Window
    {
        public Dialog_ColorPicker() 
        { 
        }

        public Dialog_ColorPicker(VehiclePawn vehicle)
        {
            this.vehicle = vehicle;
            vehicleTex = ContentFinder<Texture2D>.Get(this.vehicle.ageTracker.CurKindLifeStage.bodyGraphicData.texPath + "_north", true);

            SetColors(vehicle.DrawColor, vehicle.DrawColorTwo);
            CurrentSelectedPalette = -1;

			for (int i = 0; i < 255; i++)
			{
				HueChart.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0f, 255f, i), 1f, 1f));
			}
			HueChart.Apply(false);
			for (int j = 0; j < 255; j++)
			{
				for (int k = 0; k < 255; k++)
				{
					Color color = Color.clear;
					Color c = Color.Lerp(color, Color.white, Mathf.InverseLerp(0f, 255f, j));
					color = Color32.Lerp(Color.black, c, Mathf.InverseLerp(0f, 255f, k));
					ColorChart.SetPixel(j, k, color);
				}
			}

            pageNumber = 1;
            maskNames = vehicle.VehicleGraphic.maskMatPatterns.Keys.ToList();
            float ratio = (float)maskNames.Count / (NumberPerRow * 3);
            pageCount = Mathf.CeilToInt(ratio);

			ColorChart.Apply(false);
			doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            selectedVehicleMask = vehicle.selectedMask;
        }

        //must keep as fields for pass-by-ref
        public static ColorInt CurrentColorOne;
        public static ColorInt CurrentColorTwo;

        public static int CurrentSelectedPalette { get; set; }

        public override Vector2 InitialSize => new Vector2(900f, 540f);

        public override void PostClose()
        {
            base.PostClose();
            draggingCP = false;
            draggingHue = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var font = Text.Font;
            Text.Font = GameFont.Small;

            if(Prefs.DevMode)
            {
                if(Widgets.ButtonText(new Rect(0f, 0f, ButtonWidth * 1.5f, ButtonHeight), "Reset Palettes"))
                {
                    Current.Game.GetCachedGameComponent<ColorStorage>().ResetPalettes();
                }
            }

            Rect colorContainerRect = new Rect(inRect.width / 1.5f, 5f, inRect.width / 3, inRect.height / 2 + SwitchSize);
            DrawColorSelection(colorContainerRect);

            Rect paintRect = new Rect(inRect.width / 3f - 5f, 5f, inRect.width / 3, inRect.height / 2 + SwitchSize);
            DrawPaintSelection(paintRect);
            
            //palette height = 157
            Rect paletteRect = new Rect(inRect.width / 3f - 5f, paintRect.height + 10f, inRect.width * 2 / 3 + 5f, inRect.height / 3.21f);
            DrawColorPalette(paletteRect);

            Vector2 display = vehicle.GetCachedComp<CompVehicle>().Props.displayUICoord;
            Rect vehicleRect = new Rect(display.x, display.y, 1f, 1f);
            HelperMethods.DrawVehicleTex(vehicleRect, vehicleTex, vehicle, selectedVehicleMask, true, CurrentColorOne.ToColor, CurrentColorTwo.ToColor);

            Rect buttonRect = new Rect(0f, inRect.height - ButtonHeight, ButtonWidth, ButtonHeight);
            DoBottomButtons(buttonRect);
            Text.Font = font;
        }

        private void DrawPaintSelection(Rect paintRect)
        {
            paintRect = paintRect.ContractedBy(1);
            Widgets.DrawBoxSolid(paintRect, Greyist);

            Rect viewRect = paintRect;
            viewRect.width -= 20f;
            viewRect.x += 10f;

            Vector2 uiSize = vehicle.GetCachedComp<CompVehicle>().Props.displayUISize;
            float downSize = ((viewRect.width) / NumberPerRow) / uiSize.x;
            Rect displayRect = new Rect(0f, 0f, downSize, downSize);

            if (pageCount > 1)
            {
                Rect paginationRect = new Rect(paintRect.x - (ButtonWidth / 3), 0f, ButtonWidth / 3, ButtonWidth / 3);
                if (pageNumber < pageCount)
                {
                    if (Widgets.ButtonText(paginationRect, ">"))
                    {
                        pageNumber += 1;
                    }
                }
                else
                {
                    Widgets.ButtonText(paginationRect, string.Empty);
                }
                paginationRect.x = paintRect.x - (ButtonWidth / 3) * 2;
                if (pageNumber > 1)
                {                
                    if (Widgets.ButtonText(paginationRect, "<"))
                    {
                        pageNumber -= 1;
                    }
                }
                else
                {
                    Widgets.ButtonText(paginationRect, string.Empty);
                }
                paginationRect.y += paginationRect.height;
                Widgets.Label(paginationRect, $"{pageNumber}/{pageCount}");
            }

            float num = 0f;
            int startingIndex = (pageNumber - 1) * (NumberPerRow * 3);
            int maxIndex = SPMultiCell.Clamp((pageNumber * (NumberPerRow * 3)), 0, maskNames.Count);
            int iteration = 0;
            for (int i = startingIndex; i < maxIndex; i++, iteration++)
            {
                displayRect.x = viewRect.x + (iteration % NumberPerRow) * (uiSize.x * downSize); // + ((viewRect.width / 3) * (i % 3))  - ((postSize.x / 2) * (i % 3))
                displayRect.y = viewRect.y + (Mathf.FloorToInt(iteration / NumberPerRow)) * (uiSize.y * downSize);
                
                HelperMethods.DrawVehicleTex(displayRect, vehicleTex, vehicle, maskNames[i], true, CurrentColorOne.ToColor, CurrentColorTwo.ToColor);
                Rect imageRect = new Rect(displayRect.x, displayRect.y, downSize * uiSize.x, downSize * uiSize.y);
                if (iteration % NumberPerRow == 0)
                    num += imageRect.height;
                TooltipHandler.TipRegion(imageRect, maskNames[i]);
                if(Widgets.ButtonInvisible(imageRect))
                {
                    selectedVehicleMask = maskNames[i];
                }
            }
        }

        private void DrawColorSelection(Rect colorContainerRect)
        {
            Rect colorRect = new Rect(colorContainerRect);
            colorRect.x += 5f;
            colorRect.y = SwitchSize / 2 + 5f;
            colorRect.height = colorContainerRect.height - SwitchSize;
            
            Widgets.DrawBoxSolid(colorContainerRect, Greyist);

            //ApplyActionSwitch(delegate (ref ColorInt c, Texture2D tex)
            //{
            //    if (Widgets.ButtonImage(buttonRect, tex))
            //    {
            //        colorSelected = !colorSelected;
            //        SetColor(c.ToColor);
            //        Log.Message($"Setting: {colorSelected} to {c.ToColor}");
            //    }
            //}, new SPTuple2<Texture2D, Texture2D>(VehicleTex.SwitchLeft, VehicleTex.SwitchRight));

            /* Same (Non-Generic) Functionality */

            string c1Text = "ColorOne".Translate().ToString();
            string c2Text = "ColorTwo".Translate().ToString();
            float c1Width = Text.CalcSize(c1Text).x;
            float c2Width = Text.CalcSize(c2Text).x;

            Rect buttonRect = new Rect(colorContainerRect.x, 0f, SwitchSize, SwitchSize);
            float center = HelperMethods.DrawColorPicker(colorRect);
            buttonRect.x = center - c1Width / 2;

            Rect c1Rect = buttonRect;
            c1Rect.x -= (c1Width + 5f);
            c1Rect.width = c1Width;
            Rect c2Rect = buttonRect;
            c2Rect.x += (c2Width + 5f);
            c2Rect.width = c2Width;

            float rightHandTextPoint = c1Rect.x + c1Rect.width;
            float centerBetweenLabels = rightHandTextPoint + ((c2Rect.x - (rightHandTextPoint)) / 2);

            Rect reverseRect = new Rect(colorContainerRect.x + 11f, 15f, SwitchSize / 2.75f, SwitchSize / 2);
            if(Widgets.ButtonImage(reverseRect, VehicleTex.ReverseIcon))
            {
                SetColors(CurrentColorTwo.ToColor, CurrentColorOne.ToColor);
            }
            TooltipHandler.TipRegion(reverseRect, "SwapColors".Translate());

            SPUI.DrawLabel(c1Rect, c1Text, Color.clear, colorSelected ? Color.gray : Color.white, GameFont.Small);
            SPUI.DrawLabel(c2Rect, c2Text, Color.clear, colorSelected ? Color.white : Color.gray, GameFont.Small);

            switch (colorSelected)
            {
                case false:
                    if (Widgets.ButtonImage(buttonRect, VehicleTex.SwitchLeft))
                    {
                        colorSelected = true;
                        SetColor(CurrentColorTwo.ToColor);
                    }
                    break;
                case true:
                    if (Widgets.ButtonImage(buttonRect, VehicleTex.SwitchRight))
                    {
                        colorSelected = false;
                        SetColor(CurrentColorOne.ToColor);
                    }
                    break;
                default:
                    throw new NotImplementedException("New ColorSelection not implemented");
            }

            Rect inputRect = new Rect(colorRect.x, colorRect.y + colorRect.height + 5f, colorRect.width / 2, 20f);
            ApplyActionSwitch(delegate (ref ColorInt c)
            {
                SPUI.StringField("Hex", inputRect, c.ToColor.ToHex(), ValidInputRegex);
            });

            var saveText = string.Concat("VehicleSave".Translate(), " ", "PaletteText".Translate());
            inputRect.width = Text.CalcSize(saveText).x + 20f;
            inputRect.x = colorContainerRect.x + colorContainerRect.width - inputRect.width - 5f;
            
            if(Widgets.ButtonText(inputRect, saveText))
            {
                if (CurrentSelectedPalette >= 0 && CurrentSelectedPalette < ColorStorage.PaletteCount)
                    Current.Game.GetCachedGameComponent<ColorStorage>().AddPalette(CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentSelectedPalette);
                else
                    Messages.Message("MustSelectPalette".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        private void DrawColorPalette(Rect rect)
        {
            var palettes = Current.Game.GetCachedGameComponent<ColorStorage>().colorPalette;

            Widgets.DrawBoxSolid(rect, Greyist);

            rect = rect.ContractedBy(5);

            float rectSize = (rect.width - (ColorStorage.PaletteCount / ColorStorage.PaletteDivisor - 1) * 5f) / (ColorStorage.PaletteCountPerRow * 2);
            Rect displayRect = new Rect(rect.x, rect.y, rectSize, rectSize);
            bool selected = false;

            for (int i = 0; i < ColorStorage.PaletteCount; i++)
            {
                if (selected)
                {
                    displayRect.y -= 1;
                    displayRect.height += 2;
                    selected = false;
                }
                if (i % (ColorStorage.PaletteCount / ColorStorage.PaletteDivisor) == 0 && i != 0)
                {
                    displayRect.y += rectSize + 5f;
                    displayRect.x = rect.x;
                }
                Rect buttonRect = new Rect(displayRect.x, displayRect.y, displayRect.width * 2, displayRect.height);
                if (Widgets.ButtonInvisible(buttonRect))
                {
                    if (CurrentSelectedPalette == i)
                    {
                        CurrentSelectedPalette = -1;
                    }
                    else 
                    {
                        CurrentSelectedPalette = i;
                        SetColors(palettes[i].First, palettes[i].Second);
                    }
                }
                if (CurrentSelectedPalette == i)
                {
                    Widgets.DrawBoxSolid(buttonRect, Color.white);
                    displayRect.x += 1;
                    displayRect.y += 1;
                    displayRect.height -= 2;
                    selected = true;
                }
                Widgets.DrawBoxSolid(displayRect, palettes[i].First);
                displayRect.x += rectSize;
                if (selected)
                    displayRect.x -= 1;
                Widgets.DrawBoxSolid(displayRect, palettes[i].Second);
                displayRect.x += rectSize + 5f;
            }
        }

        private void DoBottomButtons(Rect buttonRect)
        {
            if (Widgets.ButtonText(buttonRect, "Apply".Translate()))
            {
                vehicle.DrawColor = CurrentColorOne.ToColor;
                vehicle.DrawColorTwo = CurrentColorTwo.ToColor;
                vehicle.selectedMask = selectedVehicleMask;
                vehicle.Notify_ColorChanged();
                vehicle.GetCachedComp<CompCannons>()?.Cannons.ForEach(c => c.ResolveCannonGraphics(vehicle, true));
                Close(true);
            }
            buttonRect.x += ButtonWidth;
            if (Widgets.ButtonText(buttonRect, "CancelAssigning".Translate()))
            {
                Close(true);
            }
            buttonRect.x += ButtonWidth;
            if (Widgets.ButtonText(buttonRect, "VehiclesReset".Translate()))
            {
                SoundDefOf.Click.PlayOneShotOnCamera(null);
                selectedVehicleMask = vehicle.selectedMask;
                if (CurrentSelectedPalette >= 0)
                {
                    var palette = Current.Game.GetCachedGameComponent<ColorStorage>().colorPalette[CurrentSelectedPalette];
                    SetColors(palette.First, palette.Second);
                }
                else
                {
                    SetColors(vehicle.DrawColor, vehicle.DrawColorTwo);
                }
                
            }
        }

        public static ColorInt ApplyActionSwitch(ActionRef<ColorInt> action)
        {
            switch(colorSelected)
            {
                case false:
                    action(ref CurrentColorOne);
                    return CurrentColorOne;
                case true:
                    action(ref CurrentColorTwo);
                    return CurrentColorTwo;
                default:
                    throw new NotImplementedException("New ColorSelection not implemented");
            }
        }

        public static ColorInt ApplyActionSwitch<T>(ActionRefP1<ColorInt, T> action, SPTuple2<T,T> paramOptions)
        {
            switch(colorSelected)
            {
                case false:
                    action(ref CurrentColorOne, paramOptions.First);
                    return CurrentColorOne;
                case true:
                    action(ref CurrentColorTwo, paramOptions.Second);
                    return CurrentColorTwo;
                default:
                    throw new NotImplementedException("New ColorSelection not implemented");
            }
        }

        public static void SetColors(Color col1, Color col2)
        {
            CurrentColorOne = new ColorInt(col1);
            CurrentColorTwo = new ColorInt(col2);
            ApplyActionSwitch(delegate (ref ColorInt c) { Color.RGBToHSV(c.ToColor, out hue, out saturation, out value); });
        }

		public static void SetColor(Color col)
		{
            ColorInt curColor = ApplyActionSwitch(delegate(ref ColorInt c) { c = new ColorInt(col); });
			Color.RGBToHSV(curColor.ToColor, out hue, out saturation, out value);
		}

		public static void SetColor(string hex)
		{
			CurrentColorOne = new ColorInt(HexToColor(hex));
			Color.RGBToHSV(CurrentColorOne.ToColor, out hue, out saturation, out value);
		}

		public static void SetColor(float h, float s, float b)
		{
            ColorInt curColor = ApplyActionSwitch(delegate(ref ColorInt c) { c = new ColorInt(Color.HSVToRGB(h, s, b)); });
		}

        public static string ColorToHex(Color col)
		{
			return ColorUtility.ToHtmlStringRGB(col);
		}

		public static Color HexToColor(string hexColor)
		{
			ColorUtility.TryParseHtmlString("#" + hexColor, out Color result);
			return result;
		}

        private const float ButtonWidth = 90f;
        private const float ButtonHeight = 30f;

        private const float SwitchSize = 60f;
        private const int NumberPerRow = 4;

        private readonly VehiclePawn vehicle;
        private readonly Texture2D vehicleTex;

        private int pageNumber;
        private List<string> maskNames;
        private int pageCount;
        private string selectedVehicleMask;

		public static Texture2D ColorChart = new Texture2D(255, 255);
		public static Texture2D HueChart = new Texture2D(1, 255);

		public static float hue;
		public static float saturation;
		public static float value;

        public static bool draggingCP;
        public static bool draggingHue;

        public static Color Blackist = new Color(0.06f, 0.06f, 0.06f);
		public static Color Greyist = new Color(0.2f, 0.2f, 0.2f);

        //0 = color1, 1 = color2
        public static bool colorSelected = false;

        public static readonly Regex ValidInputRegex = new Regex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}
