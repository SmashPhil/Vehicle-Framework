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

		private static readonly IntRange VehicleCountPerColumn = new IntRange(1, 3);
		private static readonly IntRange VehicleCountPerRow = new IntRange(2, 3);
		private const int GridDimensionColumns = 2;
		private const int GridDimensionRows = 2;

		private int pageNumber;
		
		private int pageCount;
		private static PatternDef selectedPattern;

		public static float hue;
		public static float saturation;
		public static float value;

		//must keep as fields for pass-by-ref
		public static ColorInt CurrentColorOne;
		public static ColorInt CurrentColorTwo;
		public static ColorInt CurrentColorThree;

		public static string colorOneHex;
		public static string colorTwoHex;
		public static string colorThreeHex;

		public static int colorSelected = 1;
		public static float additionalTiling = 1;
		public static float displacementX = 0;
		public static float displacementY = 0;
		public static float initialDragDifferenceX = 0;
		public static float initialDragDifferenceY = 0;

		private static bool mouseOver = false;

		private Dialog_ColorPicker() 
		{
		}

		public static Dialog_ColorPicker Instance { get; private set; }

		private static VehicleDef VehicleDef { get; set; }

		private static Rot8? DisplayRotation { get; set; }

		private static Graphic_Vehicle VehicleGraphic { get; set; }

		private static PatternData PatternData { get; set; }

		private static List<PatternDef> AvailablePatterns { get; set; }

		/// <summary>
		/// ColorOne, ColorTwo, ColorThree, PatternDef, Displacement, Tiles
		/// </summary>
		private static Action<Color, Color, Color, PatternDef, Vector2, float> OnSave { get; set; }

		public static int CurrentSelectedPalette { get; set; }

		public override Vector2 InitialSize => new Vector2(900f, 540f);

		public static string ColorToHex(Color col) => ColorUtility.ToHtmlStringRGB(col);

		public static bool HexToColor(string hexColor, out Color color) => ColorUtility.TryParseHtmlString("#" + hexColor, out color);

		/// <summary>
		/// Open ColorPicker for <paramref name="vehicle"/> and apply changes via <paramref name="onSave"/>
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="onSave"></param>
		public static void OpenColorPicker(VehiclePawn vehicle, Action<Color, Color, Color, PatternDef, Vector2, float> onSave)
		{
			Instance = new Dialog_ColorPicker();
			VehicleDef = vehicle.VehicleDef;
			VehicleGraphic = vehicle.VehicleGraphic;
			OnSave = onSave;
			PatternData = new PatternData(vehicle);
			Instance.SetColors(vehicle.DrawColor, vehicle.DrawColorTwo, vehicle.DrawColorThree);
			additionalTiling = PatternData.tiles;
			displacementX = PatternData.displacement.x;
			displacementY = PatternData.displacement.y;
			Instance.Init();
		}

		/// <summary>
		/// Open ColorPicker for <paramref name="vehicleDef"/> and apply changes via <paramref name="onSave"/>
		/// </summary>
		/// <param name="vehicleDef"></param>
		/// <param name="onSave"></param>
		public static void OpenColorPicker(VehicleDef vehicleDef, Action<Color, Color, Color, PatternDef, Vector2, float> onSave)
		{
			Instance = new Dialog_ColorPicker();
			VehicleDef = vehicleDef;
			VehicleGraphic = vehicleDef.graphicData.Graphic as Graphic_Vehicle;
			OnSave = onSave;
			PatternData = new PatternData(VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, vehicleDef.graphicData));
			Instance.SetColors(PatternData.color, PatternData.colorTwo, PatternData.colorThree);
			additionalTiling = PatternData.tiles;
			displacementX = PatternData.displacement.x;
			displacementY = PatternData.displacement.y;
			Instance.Init();	
		}

		private void Init()
		{
			CurrentSelectedPalette = -1;

			pageNumber = 1;
			AvailablePatterns = DefDatabase<PatternDef>.AllDefs.Where(p => p.ValidFor(VehicleDef)).ToList();
			float ratio = (float)AvailablePatterns.Count / (GridDimensionColumns * GridDimensionRows);
			Instance.pageCount = Mathf.CeilToInt(ratio);

			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			selectedPattern = AvailablePatterns.Contains(PatternData.patternDef) ? PatternData.patternDef ?? PatternDefOf.Default : PatternDefOf.Default;
			Find.WindowStack.Add(Instance);
		}

		public override void PostClose()
		{
			base.PostClose();
			RenderHelper.draggingCP = false;
			RenderHelper.draggingHue = false;
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

			if (Widgets.ButtonText(new Rect(0f, 0f, ButtonWidth * 1.5f, ButtonHeight), "VF_ResetColorPalettes".Translate()))
			{
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VF_ResetColorPalettesConfirmation".Translate(), delegate ()
				{
					VehicleMod.settings.colorStorage.ResetPalettes();
				}, false, null));
			}
			if (VehicleDef.graphicData.drawRotated && VehicleDef.graphicData.Graphic is Graphic_Vehicle graphicVehicle)
			{
				Rect rotateVehicleRect = new Rect(inRect.width / 3 - 10 - ButtonHeight, 0, ButtonHeight, ButtonHeight);
				Widgets.DrawHighlightIfMouseover(rotateVehicleRect);
				TooltipHandler.TipRegionByKey(rotateVehicleRect, "VF_RotateVehicleRendering");
				Widgets.DrawTextureFitted(rotateVehicleRect, VehicleTex.ReverseIcon, 1);
				if (Widgets.ButtonInvisible(rotateVehicleRect))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					DisplayRotation ??= VehicleDef.drawProperties.displayRotation;
					List<Rot8> validRotations = graphicVehicle.RotationsRenderableByUI.ToList();
					for (int i = 0; i < 4; i++)
					{
						DisplayRotation = DisplayRotation.Value.Rotated(RotationDirection.Clockwise, false);
						if (validRotations.Contains(DisplayRotation.Value)) { break; }
					}
				}
			}
			float topBoxHeight = inRect.height / 2 + SwitchSize;
			Rect colorContainerRect = new Rect(inRect.width / 1.5f, 15f, inRect.width / 3, topBoxHeight);
			DrawColorSelection(colorContainerRect);

			Rect paintRect = new Rect(inRect.width / 3f - 5f, colorContainerRect.y, inRect.width / 3, topBoxHeight);
			DrawPaintSelection(paintRect);

			//palette height = 157
			float panelWidth = inRect.width * 2 / 3 + 5f;
			float panelHeight = inRect.height - topBoxHeight - 20;
			Rect paletteRect = new Rect(inRect.width / 3f - 5f, inRect.height - panelHeight, panelWidth, panelHeight);
			DrawColorPalette(paletteRect);

			Rect displayRect = new Rect(0, paintRect.y + ((paintRect.height / 2) - ((paintRect.width - 15) / 2)), paintRect.width - 15, paintRect.width - 15);
			RenderHelper.DrawVehicleDef(displayRect, VehicleDef, material: null, new PatternData(CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentColorThree.ToColor, selectedPattern, 
				new Vector2(displacementX, displacementY), additionalTiling), DisplayRotation);
			Rect dragBoxRect = new Rect(0f, ButtonHeight * 1.5f, ButtonWidth * 3, inRect.height - ButtonHeight * 5);
			HandleDisplacementDrag(dragBoxRect);
			var color = GUI.color;
			if (!selectedPattern.properties.dynamicTiling)
			{
				GUI.enabled = false;
				GUI.color = UIElements.InactiveColor;
			}
			Rect sliderRect = new Rect(0f, inRect.height - ButtonHeight * 3, ButtonWidth * 3, ButtonHeight);
			UIElements.SliderLabeled(sliderRect, "VehiclePatternZoom".Translate(), "VehiclePatternZoomTooltip".Translate(), string.Empty, ref additionalTiling, 0.01f, 2);
			Rect positionLeftBox = new Rect(sliderRect)
			{
				y = sliderRect.y + sliderRect.height,
				width = (sliderRect.width / 2) * 0.95f
			};
			Rect positionRightBox = new Rect(positionLeftBox)
			{
				x = positionLeftBox.x + (sliderRect.width / 2) * 1.05f
			};
			
			UIElements.SliderLabeled(positionLeftBox, "VehiclePatternDisplacementX".Translate(), string.Empty, string.Empty, ref displacementX, -1.5f, 1.5f);
			UIElements.SliderLabeled(positionRightBox, "VehiclePatternDisplacementY".Translate(), string.Empty, string.Empty, ref displacementY, -1.5f, 1.5f);
			GUI.enabled = true;
			GUI.color = color;

			Rect buttonRect = new Rect(0f, inRect.height - ButtonHeight, ButtonWidth, ButtonHeight);
			DoBottomButtons(buttonRect);
			Text.Font = font;
		}

		private void HandleDisplacementDrag(Rect rect)
		{
			if (selectedPattern.properties.dynamicTiling && Mouse.IsOver(rect))
			{
				if (!mouseOver)
				{
					mouseOver = true;
					Cursor.SetCursor(AssetBundleDatabase.MouseHandOpen, new Vector2(3, 3), CursorMode.Auto);
				}
				if (Input.GetMouseButtonDown(0) && !RenderHelper.draggingDisplacement)
				{
					RenderHelper.draggingDisplacement = true;
					initialDragDifferenceX = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x) * 2 - 1 - displacementX;
					initialDragDifferenceY = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y) * 2 - 1 - displacementY;
					Cursor.SetCursor(AssetBundleDatabase.MouseHandClosed, new Vector2(3, 3), CursorMode.Auto);
				}
				if (RenderHelper.draggingDisplacement && Event.current.isMouse)
				{
					displacementX = (Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x) * 2 - 1 - initialDragDifferenceX).Clamp(-1.5f, 1.5f);
					displacementY = (Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y) * 2 - 1 - initialDragDifferenceY).Clamp(-1.5f, 1.5f);
				}
				if (Input.GetMouseButtonUp(0))
				{
					RenderHelper.draggingDisplacement = false;
					Cursor.SetCursor(AssetBundleDatabase.MouseHandOpen, new Vector2(3, 3), CursorMode.Auto);
				}
			}
			else
			{
				if (mouseOver)
				{
					mouseOver = false;
					RenderHelper.draggingDisplacement = false;
					CustomCursor.Activate();
				}
			}
		}

		private void DrawPaintSelection(Rect paintRect)
		{
			Widgets.DrawBoxSolid(paintRect, RenderHelper.Greyist);

			Rect outRect = paintRect;
			outRect = outRect.ContractedBy(10f);
			float sqrGridSize = outRect.width / GridDimensionColumns;
			float gridSizeX = sqrGridSize;
			float gridSizeY = sqrGridSize;

			/* Scale down if dimensions are not equal */
			Texture2D displayTex = VehicleGraphic.TexAt(Rot8.North);
			if (displayTex.width < displayTex.height)
			{
				gridSizeX *= (float)displayTex.width / displayTex.height;
				outRect.x += (sqrGridSize - gridSizeX) / 2;
			}
			/* -------------------------------------- */

			float num = 0f;
			int startingIndex = (pageNumber - 1) * (GridDimensionColumns * GridDimensionRows);
			int maxIndex = Ext_Numeric.Clamp(pageNumber * (GridDimensionColumns * GridDimensionRows), 0, AvailablePatterns.Count);
			int iteration = 0;
			Rect displayRect = new Rect(0, 0, gridSizeX, gridSizeY);
			Rect paginationRect = new Rect(paintRect.x + 5, paintRect.y + paintRect.height - ButtonHeight, paintRect.width - 10, ButtonHeight * 0.75f);
			if (pageCount > 1)
			{
				UIHelper.DrawPagination(paginationRect, ref pageNumber, pageCount);
			}
			for (int i = startingIndex; i < maxIndex; i++, iteration++)
			{
				PatternDef pattern = AvailablePatterns[i];
				displayRect.x = outRect.x + (iteration % GridDimensionColumns) * sqrGridSize;
				displayRect.y = outRect.y + (Mathf.FloorToInt(iteration / GridDimensionRows)) * gridSizeY;
				PatternData patternData = new PatternData(CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentColorThree.ToColor, pattern, new Vector2(displacementX, displacementY), additionalTiling);
				RenderHelper.DrawVehicleDef(displayRect, VehicleDef, material: null, patternData, DisplayRotation);
				Rect imageRect = new Rect(displayRect.x, displayRect.y, gridSizeX, gridSizeY);
				if (iteration % GridDimensionColumns == 0)
				{
					num += imageRect.height;
				}
				TooltipHandler.TipRegion(imageRect, pattern.LabelCap);
				if (Widgets.ButtonInvisible(imageRect))
				{
					selectedPattern = pattern;
				}
			}
		}

		private void DrawColorSelection(Rect colorContainerRect)
		{
			Rect colorRect = new Rect(colorContainerRect);
			colorRect.x += 5f;
			colorRect.y = SwitchSize / 2 + 5f;
			colorRect.height = colorContainerRect.height - SwitchSize;
			
			Widgets.DrawBoxSolid(colorContainerRect, RenderHelper.Greyist);

			string c1Text = "ColorOne".Translate().ToString();
			string c2Text = "ColorTwo".Translate().ToString();
			string c3Text = "ColorThree".Translate().ToString();
			float cHeight = Text.CalcSize(c1Text).y;
			float c1Width = Text.CalcSize(c1Text).x;
			float c2Width = Text.CalcSize(c2Text).x;
			float c3Width = Text.CalcSize(c2Text).x;

			Rect colorPickerRect = RenderHelper.DrawColorPicker(colorRect, ref hue, ref saturation, ref value, SetColor);
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
			if (Widgets.ButtonImage(reverseRect, VehicleTex.ReverseIcon))
			{
				SetColors(CurrentColorTwo.ToColor, CurrentColorThree.ToColor, CurrentColorOne.ToColor);
			}
			TooltipHandler.TipRegion(reverseRect, "VF_SwapColors".Translate());

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

			string saveText = "VF_SaveColorPalette".Translate();
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
			Widgets.DrawBoxSolid(rect, RenderHelper.Greyist);

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
			if (Widgets.ButtonText(buttonRect, "ApplyButton".Translate()))
			{
				OnSave(CurrentColorOne.ToColor, CurrentColorTwo.ToColor, CurrentColorThree.ToColor, selectedPattern, new Vector2(displacementX, displacementY), additionalTiling);
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "CancelButton".Translate()))
			{
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "ResetButton".Translate()))
			{
				SoundDefOf.Click.PlayOneShotOnCamera(null);
				selectedPattern = PatternData.patternDef;
				additionalTiling = PatternData.tiles;
				displacementX = PatternData.displacement.x;
				displacementY = PatternData.displacement.y;
				if (CurrentSelectedPalette >= 0)
				{
					var palette = VehicleMod.settings.colorStorage.colorPalette[CurrentSelectedPalette];
					SetColors(palette.Item1, palette.Item2, palette.Item3);
				}
				else
				{
					SetColors(PatternData.color, PatternData.colorTwo, PatternData.colorThree);
				}
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

		public void SetColors(Color col1, Color col2, Color col3)
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
		}

		public void SetColor(Color col)
		{
			ColorInt curColor = ApplyActionSwitch( delegate(ref ColorInt c, ref string hex) 
			{
				c = new ColorInt(col);
				hex = ColorToHex(c.ToColor);
			});
			Color.RGBToHSV(curColor.ToColor, out hue, out saturation, out value);
		}

		public bool SetColor(string hex)
		{
			if (HexToColor(hex, out Color color))
			{
				CurrentColorOne = new ColorInt(color);
				Color.RGBToHSV(CurrentColorOne.ToColor, out hue, out saturation, out value);
				return true;
			}
			return false;
		}

		public void SetColor(float h, float s, float b)
		{
			ColorInt curColor = ApplyActionSwitch(delegate(ref ColorInt c, ref string hex) 
			{ 
				c = new ColorInt(Color.HSVToRGB(h, s, b));
				hex = ColorToHex(c.ToColor);
			});
		}
	}
}
