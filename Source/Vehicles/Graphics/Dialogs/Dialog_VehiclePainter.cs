using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;

namespace Vehicles;

// TODO - Use material property block and avoid writing over vehicle / def material
[StaticConstructorOnStartup]
public class Dialog_VehiclePainter : Window
{
	private const float ButtonWidth = 90f;
	private const float ButtonHeight = 30f;
	private const float SliderHeight = 40;
	private const float IconButtonSize = 48;

	private const float SwitchSize = 60f;

	private const float SamplePadding = 2;
	private const float FramePadding = 1;

	private const int GridDimensionColumns = 2;
	private const int GridDimensionRows = 2;
	private const int SampleCount = GridDimensionColumns * GridDimensionRows;

	private int pageNumber;
	private int pageCount;

	private static PatternDef selectedPattern;
	private Rot8 displayRotation;

	// Color Picker
	private readonly ColorPicker colorPicker = new();

	private float hue;
	private float saturation;
	private float value;
	private string hex;

	private Color currentColorOne;
	private Color currentColorTwo;
	private Color currentColorThree;


	private ColorIndex colorSelected = ColorIndex.One;
	private float additionalTiling = 1;
	private float displacementX;
	private float displacementY;

	// Event handling
	private bool draggingDisplacement;

	private float initialDragDifferenceX;
	private float initialDragDifferenceY;

	private bool showPatterns = true;
	private bool mouseOver;

	public delegate void SaveColor(Color r, Color g, Color b, PatternDef pattern,
		Vector2 displacement, float tiles);

	private Dialog_VehiclePainter(VehicleDef vehicleDef)
	{
		VehicleDef = vehicleDef;
	}

	private Dialog_VehiclePainter(VehiclePawn vehicle) : this(vehicle.VehicleDef)
	{
		Vehicle = vehicle;
	}

	private VehiclePawn Vehicle { get; }

	private VehicleDef VehicleDef { get; }

	private PatternData PatternData { get; init; }

	private List<PatternDef> AvailablePatterns { get; set; }

	/// <summary>
	/// ColorOne, ColorTwo, ColorThree, PatternDef, Displacement, Tiles
	/// </summary>
	private SaveColor OnSave { get; init; }

	// OnGUI manages to get 1 more call in even after dialog is removed from WindowStack, so it's
	// easiest to just block any further gui events once the RenderTextures have been returned to pool.
	private bool IsClosing { get; set; }

	private int CurrentSelectedPalette { get; set; }

	public override Vector2 InitialSize => new(900f, 540f);

	private Rot8 DisplayRotation
	{
		get { return displayRotation; }
		set { displayRotation = value; }
	}

	private Color CurrentColor
	{
		get
		{
			return colorSelected switch
			{
				ColorIndex.One   => currentColorOne,
				ColorIndex.Two   => currentColorTwo,
				ColorIndex.Three => currentColorThree,
				_                => throw new NotImplementedException(nameof(ColorIndex))
			};
		}
	}

	private static string ColorToHex(Color col)
	{
		return ColorUtility.ToHtmlStringRGB(col);
	}

	private static bool HexToColor(string hexColor, out Color color)
	{
		return ColorUtility.TryParseHtmlString("#" + hexColor, out color);
	}

	/// <summary>
	/// Open ColorPicker for <paramref name="vehicle"/> and apply changes via <paramref name="onSave"/>
	/// </summary>
	/// <param name="vehicle"></param>
	/// <param name="onSave"></param>
	public static void OpenColorPicker(VehiclePawn vehicle, SaveColor onSave)
	{
		Dialog_VehiclePainter colorPickerDialog = new(vehicle)
		{
			OnSave = onSave,
			PatternData = new PatternData(vehicle)
		};
		Open(colorPickerDialog);
	}

	/// <summary>
	/// Open ColorPicker for <paramref name="vehicleDef"/> and apply changes via <paramref name="onSave"/>
	/// </summary>
	public static void OpenColorPicker(VehicleDef vehicleDef, SaveColor onSave)
	{
		Dialog_VehiclePainter colorPickerDialog = new(vehicleDef)
		{
			OnSave = onSave,
			PatternData =
				new PatternData(
					VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName,
						vehicleDef.graphicData)),
		};
		Open(colorPickerDialog);
	}

	private static void Open(Dialog_VehiclePainter colorPickerDialog)
	{
		colorPickerDialog.Init();
		Find.WindowStack.Add(colorPickerDialog);
	}

	private void Init()
	{
		additionalTiling = PatternData.tiles;
		displacementX = PatternData.displacement.x;
		displacementY = PatternData.displacement.y;
		SetColors(PatternData.color, PatternData.colorTwo, PatternData.colorThree);

		CurrentSelectedPalette = -1;

		doCloseX = true;
		forcePause = true;
		absorbInputAroundWindow = true;

		// Setting initial rotation will also dirty the render textures
		DisplayRotation = VehicleDef.drawProperties.displayRotation;
		RecacheAvailablePatterns();
	}

	private void UpdateHSV(Color color)
	{
		Color.RGBToHSV(color, out hue, out saturation, out value);
		hex = ColorToHex(color);
	}

	private void SetColor(Color color)
	{
		SetColor(color, colorSelected);
	}

	/// <summary>
	/// Set current selected color and update hsv, hex, and render texture cached values.
	/// </summary>
	/// <param name="color">Color to assign.</param>
	/// <param name="index">Color index.</param>
	private void SetColor(Color color, ColorIndex index)
	{
		switch (index)
		{
			case ColorIndex.One:
				currentColorOne = color;
			break;
			case ColorIndex.Two:
				currentColorTwo = color;
			break;
			case ColorIndex.Three:
				currentColorThree = color;
			break;
			default:
				throw new NotImplementedException(nameof(ColorIndex));
		}

		// HSV and hex fields represent the current selected mask index, but we still want to update
		// any time the color is set here so we need this hardcoded check on the current color. We may
		// also be toggling between colors so this needs updating regardless of whether the color set
		// differs from the assigned color index, as the HSV values still need to be reassigned.
		UpdateHSV(CurrentColor);
	}

	private void SetColors(Color color1, Color color2, Color color3)
	{
		SetColor(color1, ColorIndex.One);
		SetColor(color2, ColorIndex.Two);
		SetColor(color3, ColorIndex.Three);
	}

	private void SetColor(string hex)
	{
		if (HexToColor(hex, out Color color))
		{
			SetColor(color);
		}
	}

	private void SetColor(float h, float s, float v)
	{
		SetColor(Color.HSVToRGB(h, s, v));
	}

	private void RecacheAvailablePatterns()
	{
		if (showPatterns)
		{
			AvailablePatterns = DefDatabase<PatternDef>.AllDefsListForReading.Where(patternDef =>
				patternDef is not SkinDef && patternDef.ValidFor(VehicleDef)).ToList();
		}
		else
		{
			AvailablePatterns = DefDatabase<SkinDef>.AllDefsListForReading
			 .Where(patternDef => patternDef.ValidFor(VehicleDef)).Cast<PatternDef>().ToList();
		}

		float ratio = (float)AvailablePatterns.Count / (GridDimensionColumns * GridDimensionRows);
		pageCount = Mathf.CeilToInt(ratio);
		pageNumber = 1;

		selectedPattern = AvailablePatterns.Contains(PatternData.patternDef) ?
			PatternData.patternDef :
			AvailablePatterns.FirstOrDefault();
		selectedPattern ??= PatternDefOf.Default;
	}

	public override void PostClose()
	{
		base.PostClose();

		// For some reason, OnGUI can still end up executing for 1 more frame after PostClose is called,
		// which results in null textures being accessed. We can just skip this last frame.
		IsClosing = true;
		CursorSettings.Reset();
	}

	public override void PostOpen()
	{
		base.PostOpen();
		hex = ColorToHex(CurrentColor);
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (IsClosing)
			return;

		using TextBlock fontBlock = new(GameFont.Small);

		if (Widgets.ButtonText(new Rect(0f, 0f, ButtonWidth * 1.5f, ButtonHeight),
			"VF_ResetColorPalettes".Translate()))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
				"VF_ResetColorPalettesConfirmation".Translate(),
				VehicleMod.settings.colorStorage.ResetPalettes));
		}

		if (VehicleDef.graphicData.drawRotated &&
			VehicleDef.graphicData.Graphic is Graphic_Vehicle graphicVehicle)
		{
			Rect rotateVehicleRect = new(inRect.width / 3 - 10 - ButtonHeight, 0, ButtonHeight,
				ButtonHeight);
			Widgets.DrawHighlightIfMouseover(rotateVehicleRect);
			TooltipHandler.TipRegionByKey(rotateVehicleRect, "VF_RotateVehicleRendering");
			Widgets.DrawTextureFitted(rotateVehicleRect, VehicleTex.Rotate, 1);
			if (Widgets.ButtonInvisible(rotateVehicleRect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				List<Rot8> validRotations = graphicVehicle.RotationsRenderableByUI.ToList();
				for (int i = 0; i < 4; i++)
				{
					DisplayRotation = DisplayRotation.Rotated(RotationDirection.Clockwise, false);
					if (validRotations.Contains(DisplayRotation))
						break;
				}
			}
		}
		float topBoxHeight = inRect.height / 2 + SwitchSize;
		Rect colorContainerRect = new(inRect.width / 1.5f, 15f, inRect.width / 3, topBoxHeight);
		DrawColorSelection(colorContainerRect);

		Rect paintRect = new(inRect.width / 3f - 5f, colorContainerRect.y, inRect.width / 3,
			topBoxHeight);
		DrawPaintSelection(paintRect);

		float panelWidth = inRect.width * 2 / 3 + 5f;
		float panelHeight = inRect.height - topBoxHeight - 20;
		Rect paletteRect = new(inRect.width / 3f - 5f, inRect.height - panelHeight, panelWidth,
			panelHeight);
		DrawColorPalette(paletteRect);

		Rect displayRect = new(0, paintRect.y + ((paintRect.height / 2) - ((paintRect.width - 15) / 2)),
			paintRect.width - 15, paintRect.width - 15);

		HandleDisplacementDrag(displayRect);

		PatternData patternData = new(currentColorOne, currentColorTwo,
			currentColorThree, selectedPattern, new Vector2(displacementX, displacementY),
			additionalTiling);

		BlitRequest request = Vehicle != null ?
			BlitRequest.For(Vehicle) with { patternData = patternData, rot = DisplayRotation } :
			BlitRequest.For(VehicleDef) with { patternData = patternData, rot = DisplayRotation };
		VehicleGui.DrawVehicleOnGUI(displayRect, request);

		// Disable displacement sliders
		if (!selectedPattern.properties.dynamicTiling)
			GUIState.Disable();

		Rect sliderRect =
			new(0f, inRect.height - SliderHeight * 3, ButtonWidth * 3, SliderHeight);
		UIElements.SliderLabeled(sliderRect, "VF_PatternZoom".Translate(),
			"VF_PatternZoomTooltip".Translate(), string.Empty, ref additionalTiling, 0.01f, 2);
		Rect positionLeftBox = new(sliderRect)
		{
			y = sliderRect.y + sliderRect.height,
			width = (sliderRect.width / 2) * 0.95f
		};
		Rect positionRightBox = new(positionLeftBox)
		{
			x = positionLeftBox.x + (sliderRect.width / 2) * 1.05f
		};

		UIElements.SliderLabeled(positionLeftBox, "VF_PatternDisplacementX".Translate(),
			"VF_PatternDisplacementXTooltip".Translate(), string.Empty, ref displacementX, -1.5f, 1.5f);
		UIElements.SliderLabeled(positionRightBox, "VF_PatternDisplacementY".Translate(),
			"VF_PatternDisplacementYTooltip".Translate(), string.Empty, ref displacementY, -1.5f, 1.5f);

		// Re-enable after displacement sliders
		GUIState.Enable();

		Rect buttonRect = new(0f, inRect.height - SliderHeight, ButtonWidth, SliderHeight);
		DoBottomButtons(buttonRect);
	}

	private void HandleDisplacementDrag(Rect rect)
	{
		if (selectedPattern.properties.dynamicTiling && Mouse.IsOver(rect))
		{
			if (!mouseOver)
			{
				mouseOver = true;
				CursorSettings.SetCursor(CursorSettings.Type.OpenHand);
			}
			if (Input.GetMouseButtonDown(0) && !draggingDisplacement)
			{
				draggingDisplacement = true;
				initialDragDifferenceX =
					Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x) * 2 - 1 -
					displacementX;
				initialDragDifferenceY =
					Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y) * 2 - 1 -
					displacementY;
				CursorSettings.SetCursor(CursorSettings.Type.CloseHand);
			}
			if (draggingDisplacement && Event.current.isMouse)
			{
				float prevDisplacementX = displacementX;
				float prevDisplacementY = displacementY;

				displacementX =
					(Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x) * 2 - 1 -
						initialDragDifferenceX).Clamp(-1.5f, 1.5f);
				displacementY =
					(Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y) * 2 - 1 -
						initialDragDifferenceY).Clamp(-1.5f, 1.5f);

				if (!Mathf.Approximately(displacementX, prevDisplacementX) ||
					!Mathf.Approximately(displacementY, prevDisplacementY))
				{
					SoundDefOf.DragSlider.PlayOneShotOnCamera();
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				draggingDisplacement = false;
				CursorSettings.SetCursor(CursorSettings.Type.OpenHand);
			}
		}
		else
		{
			if (mouseOver)
			{
				mouseOver = false;
				draggingDisplacement = false;
				CursorSettings.Reset();
			}
		}
	}

	private void DrawPaintSelection(Rect paintRect)
	{
		const float PaginationBarHeight = 20;

		Widgets.DrawMenuSection(paintRect);

		string patternsLabel = "VF_Patterns".Translate();
		string skinsLabel = "VF_Skins".Translate();
		float labelHeight = Text.CalcSize(patternsLabel).y;
		Rect switchRect = new(paintRect.x, paintRect.y, paintRect.width / 2 - IconButtonSize / 2,
			labelHeight);

		Color patternLabelColor = showPatterns ? Color.white : Color.gray;
		Color skinLabelColor = !showPatterns ? Color.white : Color.gray;

		UIElements.DrawLabel(switchRect, patternsLabel, Color.clear, patternLabelColor,
			GameFont.Small, TextAnchor.MiddleRight);

		Rect toggleRect = new(switchRect.xMax, switchRect.y - IconButtonSize / 4 - 2,
			IconButtonSize, IconButtonSize);
		bool mouseOverToggle = Mouse.IsOver(toggleRect);
		if (Widgets.ButtonImage(toggleRect,
			showPatterns ? VehicleTex.SwitchLeft : VehicleTex.SwitchRight))
		{
			showPatterns = !showPatterns;
			RecacheAvailablePatterns();
			SoundDefOf.Click.PlayOneShotOnCamera();
		}

		switchRect.x = toggleRect.xMax;
		UIElements.DrawLabel(switchRect, skinsLabel, Color.clear, skinLabelColor, GameFont.Small);

		Rect outRect = paintRect with
		{
			yMin = switchRect.yMax, yMax = paintRect.yMax - PaginationBarHeight
		};
		outRect = outRect.ContractedBy(10f);
		float gridSize = Mathf.Min(outRect.width, outRect.height) / GridDimensionColumns;

		Rect displayRect = new(0, 0, gridSize, gridSize);
		Rect paginationRect = new(paintRect.x + 5, paintRect.y + paintRect.height - ButtonHeight,
			paintRect.width - 10, PaginationBarHeight);
		if (pageCount > 1)
		{
			UIHelper.DrawPagination(paginationRect, ref pageNumber, pageCount);
		}

		Rect showcaseRect = outRect.ToSquare();
		int startingIndex = (pageNumber - 1) * (GridDimensionColumns * GridDimensionRows);
		int maxIndex =
			(pageNumber * GridDimensionColumns * GridDimensionRows).Clamp(0, AvailablePatterns.Count);
		int iteration = 0;
		for (int i = startingIndex; i < maxIndex; i++, iteration++)
		{
			PatternDef pattern = AvailablePatterns[i];
			displayRect.x = showcaseRect.x + iteration % GridDimensionColumns * gridSize;
			displayRect.y = showcaseRect.y +
				Mathf.FloorToInt(iteration / (float)GridDimensionRows) * gridSize;
			PatternData patternData = new(currentColorOne, currentColorTwo,
				currentColorThree, pattern, new Vector2(displacementX, displacementY),
				additionalTiling);
			BlitRequest request = Vehicle != null ?
				BlitRequest.For(Vehicle) with { patternData = patternData, rot = DisplayRotation } :
				BlitRequest.For(VehicleDef) with
				{
					patternData = patternData,
					rot = DisplayRotation
				};
			VehicleGui.DrawVehicleOnGUI(displayRect, request);

			Rect imageRect = new(displayRect.x, displayRect.y, gridSize, gridSize);
			if (!mouseOverToggle)
			{
				TooltipHandler.TipRegion(imageRect, pattern.LabelCap);
				if (Widgets.ButtonInvisible(imageRect))
				{
					selectedPattern = pattern;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}
		}
	}

	private void DrawColorSelection(Rect colorContainerRect)
	{
		Rect colorRect = new(colorContainerRect);
		colorRect.x += 5f;
		colorRect.y = SwitchSize / 2 + 5f;
		colorRect.height = colorContainerRect.height - SwitchSize;

		Widgets.DrawMenuSection(colorContainerRect);

		string c1Text = "VF_ColorOne".Translate().ToString();
		string c2Text = "VF_ColorTwo".Translate().ToString();
		string c3Text = "VF_ColorThree".Translate().ToString();
		float cHeight = Text.CalcSize(c1Text).y;

		Rect colorPickerRect =
			colorPicker.Draw(colorRect, ref hue, ref saturation, ref value, SetColor);
		Rect buttonRect = new(colorPickerRect.x, cHeight - 2, colorPickerRect.width, cHeight);

		Rect c1Rect = new(buttonRect)
		{
			width = buttonRect.width / 3
		};
		Rect c2Rect = new(buttonRect)
		{
			x = buttonRect.x + buttonRect.width / 3,
			width = buttonRect.width / 3
		};
		Rect c3Rect = new(buttonRect)
		{
			x = buttonRect.x + (buttonRect.width / 3) * 2,
			width = buttonRect.width / 3
		};

		Rect reverseRect = new(colorContainerRect.x + 11f, 20, SwitchSize / 2.75f, SwitchSize / 2.75f);
		if (Widgets.ButtonImage(reverseRect, VehicleTex.SwapColors))
		{
			SetColors(currentColorTwo, currentColorThree, currentColorOne);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		TooltipHandler.TipRegion(reverseRect, "VF_SwapColors".Translate());

		Color c1Color = colorSelected == ColorIndex.One ? Color.white : Color.gray;
		Color c2Color = colorSelected == ColorIndex.Two ? Color.white : Color.gray;
		Color c3Color = colorSelected == ColorIndex.Three ? Color.white : Color.gray;

		if (Mouse.IsOver(c1Rect) && colorSelected != ColorIndex.One)
		{
			c1Color = GenUI.MouseoverColor;
		}
		else if (Mouse.IsOver(c2Rect) && colorSelected != ColorIndex.Two)
		{
			c2Color = GenUI.MouseoverColor;
		}
		else if (Mouse.IsOver(c3Rect) && colorSelected != ColorIndex.Three)
		{
			c3Color = GenUI.MouseoverColor;
		}
		// ReSharper disable once RedundantArgumentDefaultValue
		// NOTE - TextAnchor optional parameter is more readable left in, as the other 2
		// parameters related to other alignments for the top Color label buttons
		UIElements.DrawLabel(c1Rect, c1Text, Color.clear, c1Color, GameFont.Small,
			TextAnchor.MiddleLeft);
		UIElements.DrawLabel(c2Rect, c2Text, Color.clear, c2Color, GameFont.Small,
			TextAnchor.MiddleCenter);
		UIElements.DrawLabel(c3Rect, c3Text, Color.clear, c3Color, GameFont.Small,
			TextAnchor.MiddleRight);

		if (colorSelected != ColorIndex.One && Widgets.ButtonInvisible(c1Rect))
		{
			colorSelected = ColorIndex.One;
			SetColor(currentColorOne);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		if (colorSelected != ColorIndex.Two && Widgets.ButtonInvisible(c2Rect))
		{
			colorSelected = ColorIndex.Two;
			SetColor(currentColorTwo);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		if (colorSelected != ColorIndex.Three && Widgets.ButtonInvisible(c3Rect))
		{
			colorSelected = ColorIndex.Three;
			SetColor(currentColorThree);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}

		Rect inputRect = new(colorRect.x, colorRect.y + colorRect.height + 5f,
			colorRect.width / 2, 20f);

		string hexChange = UIElements.HexField("VF_ColorPickerHex".Translate(), inputRect, hex);
		if (hex != hexChange)
		{
			hex = hexChange;
			SetColor(hex);
		}

		string saveText = "VF_SaveColorPalette".Translate();
		inputRect.width = Text.CalcSize(saveText).x + 20f;
		inputRect.x = colorContainerRect.x + colorContainerRect.width - inputRect.width - 5f;
		if (Widgets.ButtonText(inputRect, saveText))
		{
			if (CurrentSelectedPalette is >= 0 and < ColorStorage.PaletteCount)
			{
				VehicleMod.settings.colorStorage.AddPalette(currentColorOne,
					currentColorTwo, currentColorThree, CurrentSelectedPalette);
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			else
			{
				Messages.Message("VF_MustSelectColorPalette".Translate(), MessageTypeDefOf.RejectInput);
			}
		}
	}

	private void DrawColorPalette(Rect rect)
	{
		List<(Color, Color, Color)> palettes = VehicleMod.settings.colorStorage.colorPalette;
		Widgets.DrawMenuSection(rect);

		rect = rect.ContractedBy(5);

		float rectSize =
			(rect.width - ((float)ColorStorage.PaletteCount / ColorStorage.PaletteRowCount - 1) * 5f) /
			(ColorStorage.PaletteCountPerRow * 3);
		Rect displayRect = new(rect.x, rect.y, rectSize, rectSize);

		for (int i = 0; i < ColorStorage.PaletteCount; i++)
		{
			if (i % (ColorStorage.PaletteCount / ColorStorage.PaletteRowCount) == 0 && i != 0)
			{
				displayRect.y += rectSize + 5f;
				displayRect.x = rect.x;
			}
			Rect buttonRect = new(displayRect.x, displayRect.y, displayRect.width * 3,
				displayRect.height);
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
		if (Widgets.ButtonText(buttonRect, "VF_ApplyButton".Translate()))
		{
			OnSave(currentColorOne, currentColorTwo, currentColorThree,
				selectedPattern, new Vector2(displacementX, displacementY), additionalTiling);
			Close();
		}
		buttonRect.x += ButtonWidth;
		if (Widgets.ButtonText(buttonRect, "CancelButton".Translate()))
		{
			Close();
		}
		buttonRect.x += ButtonWidth;
		if (Widgets.ButtonText(buttonRect, "ResetButton".Translate()))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			selectedPattern = PatternData.patternDef;
			additionalTiling = PatternData.tiles;
			displacementX = PatternData.displacement.x;
			displacementY = PatternData.displacement.y;
			if (CurrentSelectedPalette >= 0)
			{
				(Color one, Color two, Color three) palette =
					VehicleMod.settings.colorStorage.colorPalette[CurrentSelectedPalette];
				SetColors(palette.one, palette.two, palette.three);
			}
			else
			{
				SetColors(PatternData.color, PatternData.colorTwo, PatternData.colorThree);
			}
		}
	}

	private enum ColorIndex
	{
		One,
		Two,
		Three
	}
}