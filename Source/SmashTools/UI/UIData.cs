using UnityEngine;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
public static class UIData
{
	public static readonly Texture2D FillableBarTexture =
		SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);

	public static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;

	/// <summary>
	/// Solid Color Textures
	/// </summary>
	public static readonly Texture2D FillableBarBackgroundTex =
		SolidColorMaterials.NewSolidColorTexture(Color.black);

	public static readonly Texture2D FillableBarInnerTex =
		SolidColorMaterials.NewSolidColorTexture(new ColorInt(19, 22, 27).ToColor);

	public static readonly Color ProgressBarRed = new Color(0.9f, 0.15f, 0.1f, 1f);

	public static readonly Texture2D FillableBarProgressBar =
		SolidColorMaterials.NewSolidColorTexture(ProgressBarRed);

	public static readonly Texture2D FillableBarProgressBarBG =
		SolidColorMaterials.NewSolidColorTexture(0.35f, 0.35f, 0.35f, 1f);

	public static readonly Texture2D TransparentBlackBG =
		SolidColorMaterials.NewSolidColorTexture(0.1f, 0.1f, 0.1f, 0.1f);

	public static readonly Texture2D CurvePoint =
		ContentFinder<Texture2D>.Get("UI/Widgets/Dev/CurvePoint");

	// For some reason Ludeon made the Off texture private, but the On texture public... stupid, I know.
	public static readonly Texture2D RadioButOffTex =
		ContentFinder<Texture2D>.Get("UI/Widgets/RadioButOff");

	public static readonly Texture2D TargetLevelArrow =
		ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");
}