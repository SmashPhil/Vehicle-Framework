using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using static SmashTools.Utilities;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class Dialog_ColorPicker : Window
	{
		private const float ButtonWidth = 90f;
		private const float ButtonHeight = 30f;

		private const float SwitchSize = 60f;
		private const int GridDimensionSqr = 3;

		private readonly VehiclePawn vehicle;
		private readonly Texture2D vehicleTex;

		private int pageNumber;
		private static List<PatternDef> availableMasks = new List<PatternDef>();
		private static List<Material> maskMaterials = new List<Material>();
		private int pageCount;
		private PatternDef selectedPattern;

		public static Texture2D ColorChart = new Texture2D(255, 255);
		public static Texture2D HueChart = new Texture2D(1, 255);

		public static float hue;
		public static float saturation;
		public static float value;

		public static bool draggingCP;
		public static bool draggingHue;

		public static Color Blackist = new Color(0.06f, 0.06f, 0.06f);
		public static Color Greyist = new Color(0.2f, 0.2f, 0.2f);

		//must keep as fields for pass-by-ref
		public static ColorInt CurrentColorOne;
		public static ColorInt CurrentColorTwo;
		public static ColorInt CurrentColorThree;

		public static string colorOneHex;
		public static string colorTwoHex;
		public static string colorThreeHex;

		public static int colorSelected = 1;
		public static float additionalTiling = 1;

		public Dialog_ColorPicker() 
		{ 
		}

		public Dialog_ColorPicker(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			vehicleTex = vehicle.VehicleGraphic.TexAt(Rot8.North);

			SetColors(vehicle.DrawColor, vehicle.DrawColorTwo, vehicle.DrawColorThree);
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
			availableMasks = DefDatabase<PatternDef>.AllDefs.Where(p => p.ValidFor(vehicle.VehicleDef)).ToList();
			RecacheMaterials();
			float ratio = (float)availableMasks.Count / (GridDimensionSqr * GridDimensionSqr);
			pageCount = Mathf.CeilToInt(ratio);

			ColorChart.Apply(false);
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			selectedPattern = vehicle.pattern;
		}

		public static int CurrentSelectedPalette { get; set; }

		public override Vector2 InitialSize => new Vector2(900f, 540f);

		public static string ColorToHex(Color col) => ColorUtility.ToHtmlStringRGB(col);

		public static bool HexToColor(string hexColor, out Color color) => ColorUtility.TryParseHtmlString("#" + hexColor, out color);

		public override void PostClose()
		{
			base.PostClose();
			draggingCP = false;
			draggingHue = false;
		}

		public override void PostOpen()
		{
			base.PostOpen();
			colorOneHex = ColorToHex(CurrentColorOne.ToColor);
			colorTwoHex = ColorToHex(CurrentColorTwo.ToColor);
			colorThreeHex = ColorToHex(CurrentColorThree.ToColor);
		}

		public override void DoWindowContents(Rect inRect)
		{
			var font = Text.Font;
			Text.Font = GameFont.Small;

			if(Prefs.DevMode)
			{
				if(Widgets.ButtonText(new Rect(0f, 0f, ButtonWidth * 1.5f, ButtonHeight), "Reset Palettes"))
				{
					VehicleMod.settings.colorStorage.ResetPalettes();
				}
			}

			Rect colorContainerRect = new Rect(inRect.width / 1.5f, 5f, inRect.width / 3, inRect.height / 2 + SwitchSize);
			DrawColorSelection(colorContainerRect);

			Rect paintRect = new Rect(inRect.width / 3f - 5f, 5f, inRect.width / 3, inRect.height / 2 + SwitchSize);
			DrawPaintSelection(paintRect);

			//palette height = 157
			float panelWidth = inRect.width * 2 / 3 + 5f;
			float panelHeight = ((panelWidth - 5 - (ColorStorage.PaletteCount / ColorStorage.PaletteRowCount - 1) * 5f) / (ColorStorage.PaletteCountPerRow * 3)) * (ColorStorage.PaletteRowCount + 1);
			Rect paletteRect = new Rect(inRect.width / 3f - 5f, paintRect.height + 10f, panelWidth, panelHeight);
			DrawColorPalette(paletteRect);

			Vector2 display = vehicle.VehicleDef.drawProperties.colorPickerUICoord;
			Rect vehicleRect = new Rect(display.x, display.y, vehicle.VehicleDef.drawProperties.upgradeUISize.x, vehicle.VehicleDef.drawProperties.upgradeUISize.y);
			RenderHelper.DrawVehicleTexTiled(vehicleRect, vehicleTex, vehicle, selectedPattern, true, CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentColorThree.ToColor, additionalTiling);

			if (selectedPattern.properties.dynamicTiling)
			{
				Rect sliderRect = new Rect(0f, inRect.height - ButtonHeight * 3, ButtonWidth * 3, ButtonHeight);
				additionalTiling = Widgets.HorizontalSlider(sliderRect, additionalTiling, 0, 2);
			}

			Rect buttonRect = new Rect(0f, inRect.height - ButtonHeight, ButtonWidth, ButtonHeight);
			DoBottomButtons(buttonRect);
			Text.Font = font;
		}

		private void DrawPaintSelection(Rect paintRect)
		{
			paintRect = paintRect.ContractedBy(1);
			Widgets.DrawBoxSolid(paintRect, Greyist);

			Rect viewRect = paintRect;
			viewRect = viewRect.ContractedBy(10f);
			float gridSize = viewRect.width / GridDimensionSqr;

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
			int startingIndex = (pageNumber - 1) * (GridDimensionSqr * GridDimensionSqr);
			int maxIndex = Ext_Numeric.Clamp((pageNumber * (GridDimensionSqr * GridDimensionSqr)), 0, availableMasks.Count);
			int iteration = 0;
			Rect displayRect = new Rect(0, 0, gridSize, gridSize);

			for (int i = startingIndex; i < maxIndex; i++, iteration++)
			{
				displayRect.x = viewRect.x + (iteration % GridDimensionSqr) * gridSize;
				displayRect.y = viewRect.y + (Mathf.FloorToInt(iteration / GridDimensionSqr)) * gridSize;
				
				GenUI.DrawTextureWithMaterial(displayRect, VehicleTex.BlankPattern, maskMaterials[i]);
				Rect imageRect = new Rect(displayRect.x, displayRect.y, gridSize, gridSize);
				if (iteration % GridDimensionSqr == 0)
				{
					num += imageRect.height;
				}
				TooltipHandler.TipRegion(imageRect, availableMasks[i].LabelCap);
				if(Widgets.ButtonInvisible(imageRect))
				{
					selectedPattern = availableMasks[i];
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

			string c1Text = "ColorOne".Translate().ToString();
			string c2Text = "ColorTwo".Translate().ToString();
			string c3Text = "ColorThree".Translate().ToString();
			float cHeight = Text.CalcSize(c1Text).y;
			float c1Width = Text.CalcSize(c1Text).x;
			float c2Width = Text.CalcSize(c2Text).x;
			float c3Width = Text.CalcSize(c2Text).x;

			Rect colorPickerRect = RenderHelper.DrawColorPicker(colorRect);
			Rect buttonRect = new Rect(colorPickerRect.x, cHeight - 2, colorPickerRect.width, cHeight);

			Rect c1Rect = new Rect(buttonRect)
			{
				width = buttonRect.width / 3
			};
			Rect c2Rect = new Rect(buttonRect)
			{
				x = buttonRect.x + buttonRect.width / 3,
				width = buttonRect.width / 3
			};
			Rect c3Rect = new Rect(buttonRect)
			{
				x = buttonRect.x + (buttonRect.width / 3) * 2,
				width = buttonRect.width / 3
			};

			Rect reverseRect = new Rect(colorContainerRect.x + 11f, 20, SwitchSize / 2.75f, SwitchSize / 2.75f);
			if(Widgets.ButtonImage(reverseRect, VehicleTex.ReverseIcon))
			{
				SetColors(CurrentColorTwo.ToColor, CurrentColorThree.ToColor, CurrentColorOne.ToColor);
			}
			TooltipHandler.TipRegion(reverseRect, "SwapColors".Translate());

			Color c1Color = colorSelected == 1 ? Color.white : Color.gray;
			Color c2Color = colorSelected == 2 ? Color.white : Color.gray;
			Color c3Color = colorSelected == 3 ? Color.white : Color.gray;

			if (Mouse.IsOver(c1Rect) && colorSelected != 1)
			{
				c1Color = GenUI.MouseoverColor;
			}
			else if (Mouse.IsOver(c2Rect) && colorSelected != 2)
			{
				c2Color = GenUI.MouseoverColor;
			}
			else if (Mouse.IsOver(c3Rect) && colorSelected != 3)
			{
				c3Color = GenUI.MouseoverColor;
			}
			UIElements.DrawLabel(c1Rect, c1Text, Color.clear, c1Color, GameFont.Small, TextAnchor.MiddleLeft);
			UIElements.DrawLabel(c2Rect, c2Text, Color.clear, c2Color, GameFont.Small, TextAnchor.MiddleCenter);
			UIElements.DrawLabel(c3Rect, c3Text, Color.clear, c3Color, GameFont.Small, TextAnchor.MiddleRight);

			if (colorSelected != 1 && Widgets.ButtonInvisible(c1Rect))
			{
				colorSelected = 1;
				SetColor(CurrentColorOne.ToColor);
			}
			if (colorSelected != 2 && Widgets.ButtonInvisible(c2Rect))
			{
				colorSelected = 2;
				SetColor(CurrentColorTwo.ToColor);
			}
			if (colorSelected != 3 && Widgets.ButtonInvisible(c3Rect))
			{
				colorSelected = 3;
				SetColor(CurrentColorThree.ToColor);
			}

			Rect inputRect = new Rect(colorRect.x, colorRect.y + colorRect.height + 5f, colorRect.width / 2, 20f);
			ApplyActionSwitch(delegate (ref ColorInt c, ref string hex)
			{
				hex = UIElements.HexField("ColorPickerHex".Translate(), inputRect, hex);
				if (HexToColor(hex, out Color color) && color.a == 1)
				{
					c = new ColorInt(color);
				}
			});

			var saveText = string.Concat("VehicleSave".Translate(), " ", "PaletteText".Translate());
			inputRect.width = Text.CalcSize(saveText).x + 20f;
			inputRect.x = colorContainerRect.x + colorContainerRect.width - inputRect.width - 5f;
			
			if (Widgets.ButtonText(inputRect, saveText))
			{
				if (CurrentSelectedPalette >= 0 && CurrentSelectedPalette < ColorStorage.PaletteCount)
				{
					VehicleMod.settings.colorStorage.AddPalette(CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentColorThree.ToColor, CurrentSelectedPalette);
				}
				else
				{
					Messages.Message("MustSelectPalette".Translate(), MessageTypeDefOf.RejectInput);
				}
			}
		}

		private void DrawColorPalette(Rect rect)
		{
			var palettes = VehicleMod.settings.colorStorage.colorPalette; 
			Widgets.DrawBoxSolid(rect, Greyist);

			rect = rect.ContractedBy(5);

			float rectSize = (rect.width - (ColorStorage.PaletteCount / ColorStorage.PaletteRowCount - 1) * 5f) / (ColorStorage.PaletteCountPerRow * 3);
			Rect displayRect = new Rect(rect.x, rect.y, rectSize, rectSize);

			for (int i = 0; i < ColorStorage.PaletteCount; i++)
			{
				if (i % (ColorStorage.PaletteCount / ColorStorage.PaletteRowCount) == 0 && i != 0)
				{
					displayRect.y += rectSize + 5f;
					displayRect.x = rect.x;
				}
				Rect buttonRect = new Rect(displayRect.x, displayRect.y, displayRect.width * 3, displayRect.height);
				if (Widgets.ButtonInvisible(buttonRect))
				{
					if (CurrentSelectedPalette == i)
					{
						CurrentSelectedPalette = -1;
					}
					else 
					{
						CurrentSelectedPalette = i;
						SetColors(palettes[i].Item1, palettes[i].Item2, palettes[i].Item3);
					}
				}
				if (CurrentSelectedPalette == i)
				{
					Rect selectRect = buttonRect.ExpandedBy(1.5f);
					selectRect.height -= 0.5f;
					Widgets.DrawBoxSolid(selectRect, Color.white);
				}
				Widgets.DrawBoxSolid(displayRect, palettes[i].Item1);
				displayRect.x += rectSize;
				Widgets.DrawBoxSolid(displayRect, palettes[i].Item2);
				displayRect.x += rectSize;
				Widgets.DrawBoxSolid(displayRect, palettes[i].Item3);
				displayRect.x += rectSize + 5f;
			}
		}

		private void DoBottomButtons(Rect buttonRect)
		{
			if (Widgets.ButtonText(buttonRect, "Apply".Translate()))
			{
				vehicle.DrawColor = CurrentColorOne.ToColor;
				vehicle.DrawColorTwo = CurrentColorTwo.ToColor;
				vehicle.DrawColorThree = CurrentColorThree.ToColor;
				vehicle.pattern = selectedPattern;
				vehicle.tiles = additionalTiling;
				vehicle.Notify_ColorChanged();
				vehicle.CompCannons?.Cannons.ForEach(c => c.ResolveCannonGraphics(vehicle, true));
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
				selectedPattern = vehicle.pattern;
				additionalTiling = 1;
				if (CurrentSelectedPalette >= 0)
				{
					var palette = VehicleMod.settings.colorStorage.colorPalette[CurrentSelectedPalette];
					SetColors(palette.Item1, palette.Item2, palette.Item3);
				}
				else
				{
					SetColors(vehicle.DrawColor, vehicle.DrawColorTwo, vehicle.DrawColorThree);
				}
				
			}
		}

		private static void RecacheMaterials()
		{
			maskMaterials.Clear();
			foreach (PatternDef pattern in availableMasks)
			{
				MaterialRequestRGB req = new MaterialRequestRGB()
				{
					mainTex = VehicleTex.BlankPattern,
					shader = RGBShaderTypeDefOf.CutoutComplexPattern.Shader,
					properties = pattern.properties,
					color = pattern.properties.colorOne ?? CurrentColorOne.ToColor,
					colorTwo = pattern.properties.colorTwo ?? CurrentColorTwo.ToColor,
					colorThree = pattern.properties.colorThree ?? CurrentColorThree.ToColor,
					tiles = 1,
					isSkin = pattern is SkinDef,
					maskTex = PatternDefOf.Default[Rot8.North],
					patternTex = pattern[Rot8.North],
					shaderParameters = null
				};
				Material patMat = MaterialPoolExpanded.MatFrom(req, true);
				maskMaterials.Add(patMat);
			}
		}

		public static ColorInt ApplyActionSwitch(ActionRef<ColorInt, string> action)
		{
			switch(colorSelected)
			{
				case 1:
					action(ref CurrentColorOne, ref colorOneHex);
					return CurrentColorOne;
				case 2:
					action(ref CurrentColorTwo, ref colorTwoHex);
					return CurrentColorTwo;
				case 3:
					action(ref CurrentColorThree, ref colorThreeHex);
					return CurrentColorThree;
				default:
					throw new ArgumentOutOfRangeException("ColorSelection out of range. Must be between 1 and 3.");
			}
		}

		public static void SetColors(Color col1, Color col2, Color col3)
		{
			CurrentColorOne = new ColorInt(col1);
			CurrentColorTwo = new ColorInt(col2);
			CurrentColorThree = new ColorInt(col3);
			colorOneHex = ColorToHex(CurrentColorOne.ToColor);
			colorTwoHex = ColorToHex(CurrentColorTwo.ToColor);
			colorThreeHex = ColorToHex(CurrentColorThree.ToColor);
			ApplyActionSwitch(delegate (ref ColorInt c, ref string hex) 
			{ 
				Color.RGBToHSV(c.ToColor, out hue, out saturation, out value); 
				hex = ColorToHex(c.ToColor);
			});
			RecacheMaterials();
		}

		public static void SetColor(Color col)
		{
			ColorInt curColor = ApplyActionSwitch( delegate(ref ColorInt c, ref string hex) 
			{
				c = new ColorInt(col);
				hex = ColorToHex(c.ToColor);
			});
			Color.RGBToHSV(curColor.ToColor, out hue, out saturation, out value);
			RecacheMaterials();
		}

		public static bool SetColor(string hex)
		{
			if (HexToColor(hex, out Color color))
			{
				CurrentColorOne = new ColorInt(color);
				Color.RGBToHSV(CurrentColorOne.ToColor, out hue, out saturation, out value);
				RecacheMaterials();
				return true;
			}
			return false;
		}

		public static void SetColor(float h, float s, float b)
		{
			ColorInt curColor = ApplyActionSwitch(delegate(ref ColorInt c, ref string hex) 
			{ 
				c = new ColorInt(Color.HSVToRGB(h, s, b));
				hex = ColorToHex(c.ToColor);
			});
			RecacheMaterials();
		}
	}
}
