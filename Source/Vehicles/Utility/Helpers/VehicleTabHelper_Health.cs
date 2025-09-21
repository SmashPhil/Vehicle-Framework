using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

[StaticConstructorOnStartup]
public static class VehicleTabHelper_Health
{
	public const float LeftWindowWidth = 250;
	public const float WindowHeight = 430;
	public const float LabelColumnWidth = 200;
	public const float ColumnWidth = 100;

	public const float ComponentRowHeight = 20f;
	public const float ComponentIndicatorIconSize = 20f;
	public const float MoreInfoIconSize = 24;

	private const int ColumnCount = 2;

	private static readonly Color SlightlyUpgraded = new(0.7f, 0.75f, 1);
	private static readonly Color HeavilyUpgraded = Color.cyan;

	private static readonly Color MouseOverColor = new(0.85f, 0.85f, 0.85f, 0.1f);
	private static readonly Color AlternatingColor = new(0.75f, 0.75f, 0.75f, 0.1f);

	private static readonly List<DamageArmorCategoryDef> ArmorRatingDefs;
	private static readonly StringBuilder TooltipBuilder = new();

	private static float componentListHeight;
	private static VehiclePawn inspectingVehicle;
	private static Vector2 size;
	private static bool compressed;
	private static bool moreInfo;

	private static ITab_Vehicle_Health.VehicleHealthTab onTab;
	private static Vector2 componentTabScrollPos;
	private static VehicleComponent selectedComponent;


	public static Vector2 Size => size;

	static VehicleTabHelper_Health()
	{
		ArmorRatingDefs = DefDatabase<DamageArmorCategoryDef>.AllDefsListForReading;
	}

	public static void Init()
	{
		componentTabScrollPos = Vector2.zero;
		selectedComponent = null;
		moreInfo = false;
		RecacheWindowWidth();
	}

	public static void Clear()
	{
		componentTabScrollPos = Vector2.zero;
		selectedComponent = null;
		moreInfo = false;
	}

	public static Vector2 Start(VehiclePawn vehicle, bool compressed = false, float height = WindowHeight)
	{
		size.y = height;
		if (vehicle != inspectingVehicle)
		{
			// Not captured by OnOpen when switching between vehicles with ITab already open
			inspectingVehicle = vehicle;
			VehicleTabHelper_Health.compressed = compressed;
			RecacheWindowWidth();
			RecacheComponentListHeight();
		}
		return Size;
	}

	public static void End()
	{
	}

	public static void DrawHealthPanel(VehiclePawn vehicle)
	{
		const float TopPadding = 20;

		Rect rect = new(0, TopPadding, Size.x, Size.y - TopPadding);

		Rect infoPanelRect = new Rect(rect.x, rect.y, LeftWindowWidth, rect.height).Rounded();
		Rect componentPanelRect =
			new(infoPanelRect.xMax, rect.y, Size.x - LeftWindowWidth, rect.height);

		infoPanelRect.yMin +=
			11f; //Extra space for tab, excluded from componentPanelRect for top options

		DrawHealthInfo(infoPanelRect, vehicle);
		DrawComponentsInfo(componentPanelRect, vehicle);
	}

	private static void DrawHealthInfo(Rect rect, VehiclePawn vehicle)
	{
		Widgets.DrawMenuSection(rect);
		List<TabRecord> list = [];
		list.Add(new TabRecord("HealthOverview".Translate(),
			delegate { onTab = ITab_Vehicle_Health.VehicleHealthTab.Overview; },
			onTab == ITab_Vehicle_Health.VehicleHealthTab.Overview));
		//list.Add(new TabRecord("VF_JobSettings".Translate(), delegate ()
		//{
		//	onTab = ITab_Vehicle_Health.VehicleHealthTab.JobSettings;
		//}, onTab == ITab_Vehicle_Health.VehicleHealthTab.JobSettings));
		TabDrawer.DrawTabs(rect, list);

		rect = rect.ContractedBy(9f);

		Widgets.BeginGroup(rect);
		using TextBlock infoBlock = new(GameFont.Small, TextAnchor.UpperLeft, Color.white);
		switch (onTab)
		{
			case ITab_Vehicle_Health.VehicleHealthTab.Overview:
				DrawVehicleInformation(rect, vehicle);
			break;
			case ITab_Vehicle_Health.VehicleHealthTab.JobSettings:
				//DrawJobSettings(rect, vehicle);
			break;
			default:
				throw new NotImplementedException(nameof(onTab));
		}
		Widgets.EndGroup();
	}

	// TODO 1.7 - Rip out job tab, it's completely unused
	//private static void DrawJobSettings(Rect leftRect, VehiclePawn vehicle)
	//{
	//  float curY = 0;
	//  Rect rect = new Rect(0f, curY, leftRect.width, 34f);

	//  rect.SplitVertically(rect.width / 2, out Rect _, out Rect buttonRect);

	//  if (Widgets.ButtonText(buttonRect, "ResetButton".Translate()))
	//  {
	//    //vehicle.jobLimitations.Clear();
	//  }
	//}

	private static void DrawVehicleInformation(Rect leftRect, VehiclePawn vehicle)
	{
		float curY = 0;
		Rect rect = new(0f, curY, leftRect.width, 34f);

		using (new TextBlock(TextAnchor.UpperCenter))
		{
			Widgets.Label(rect, vehicle.LabelCap);
		}
		if (Mouse.IsOver(rect))
		{
			string dateReadout =
				$"{Find.ActiveLanguageWorker.OrdinalNumber(vehicle.ageTracker.BirthDayOfSeasonZeroBased + 1)} {vehicle.ageTracker.BirthQuadrum.Label()}, {vehicle.ageTracker.BirthYear}";
			(GenTicks.TicksAbs - vehicle.ageTracker.BirthAbsTicks).TicksToPeriod(out int years,
				out int quadrums, out int days, out _);
			string chronologicalReadout = "AgeChronological".Translate(years, quadrums, days);

			TooltipHandler.TipRegion(rect,
				() => $"{"VF_VehicleAgeReadout".Translate(dateReadout)}\n{chronologicalReadout}",
				"HealthTab".GetHashCode());
			Widgets.DrawHighlight(rect);
		}
		curY += 34;

		Rect statRect = new(0, curY, leftRect.width, 34);
		foreach (VehicleStatDef statDef in vehicle.VehicleDef.StatCategoryDefs().Distinct())
		{
			curY = statDef.Worker.DrawVehicleStat(statRect, curY, vehicle);
			statRect.y = curY;
		}
	}

	/// <summary>
	/// Draw component list with health, efficiency, and armor values
	/// </summary>
	/// <param name="rect"></param>
	/// <param name="vehicle"></param>
	private static void DrawComponentsInfo(Rect rect, VehiclePawn vehicle)
	{
		using TextBlock textFont = new(GameFont.Small, TextAnchor.MiddleCenter);

		// Skip header for component name column
		float textHeight = Text.CalcSize("VF_ComponentHealth".Translate()).y;

		Rect topLabelRect = new(rect.x + LabelColumnWidth, rect.y, ColumnWidth, textHeight);
		Widgets.Label(topLabelRect, "VF_ComponentHealth".Translate());

		Rect efficiencyRect = topLabelRect with { x = topLabelRect.x + ColumnWidth };
		Widgets.Label(efficiencyRect, "VF_ComponentEfficiency".Translate());
		topLabelRect.x = efficiencyRect.xMax;

		if (!compressed)
		{
			const float MoreInfoLabelOffset = 50;

			Rect moreInfoButtonRect = new(efficiencyRect.x + MoreInfoLabelOffset, 0,
				MoreInfoIconSize, MoreInfoIconSize);
			Color trueBaseColor = GUI.color;
			Color baseColor = !moreInfo ? Color.white : Color.green;
			Color mouseoverColor = !moreInfo ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
			if (Widgets.ButtonImageFitted(moreInfoButtonRect,
				CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor))
			{
				moreInfo = !moreInfo;
				RecacheWindowWidth();

				if (moreInfo)
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
				else
					SoundDefOf.TabClose.PlayOneShotOnCamera();
			}
			// TODO - This shouldn't be necessary but ButtonImageFitted doesn't reset to white, it resets to baseColor.
			// This can be removed if / when Ludeon fixes it.
			GUI.color = trueBaseColor;

			if (moreInfo)
			{
				foreach (DamageArmorCategoryDef armorCategoryDef in ArmorRatingDefs)
				{
					Widgets.Label(topLabelRect, armorCategoryDef.armorRatingStat.LabelCap);
					topLabelRect.x += topLabelRect.width;
				}
			}
		}

		using (new TextBlock(UIElements.menuSectionBGBorderColor))
		{
			Widgets.DrawLineHorizontal(rect.x, topLabelRect.y + textHeight / 1.25f, rect.width);
		}

		rect.yMin += textHeight / 1.25f + 1; //+1 for H. line
		rect.x += 2.5f;
		rect.width -= 5;

		// Begin ScrollView
		Rect scrollView = new(rect.x, rect.y + topLabelRect.height * 2, rect.width - UIData.ScrollbarSize,
			componentListHeight);
		bool alternatingRow = false;
		Widgets.BeginScrollView(rect, ref componentTabScrollPos, scrollView);
		float curY = scrollView.y;
		bool highlighted = false;
		foreach (VehicleComponent component in vehicle.statHandler.components)
		{
			Rect compRect = new(rect.x, curY, rect.width - 16, ComponentRowHeight);
			float usedHeight = DrawCompRow(compRect, component, LabelColumnWidth, ColumnWidth,
				alternatingRow);
			//TooltipHandler.TipRegion(compRect, "VF_ComponentClickMoreInfoTooltip".Translate());
			Rect highlightingRect = new(compRect)
			{
				height = usedHeight
			};
			if (Mouse.IsOver(highlightingRect))
			{
				Widgets.DrawBoxSolid(highlightingRect, MouseOverColor);
				//For debug drawing of component hitbox
				vehicle.HighlightedComponent = component;
				highlighted = true;
			}
			else if (selectedComponent == component)
			{
				Widgets.DrawBoxSolid(highlightingRect, MouseOverColor);
				highlighted = true;
			}
			if (Widgets.ButtonInvisible(compRect))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				selectedComponent = selectedComponent != component ? component : null;
			}
			curY += usedHeight;
			alternatingRow = !alternatingRow;
		}
		if (!highlighted)
		{
			vehicle.HighlightedComponent = null;
		}
		Widgets.EndScrollView();
		// End ScrollView
	}

	private static float DrawCompRow(Rect rect, VehicleComponent component, float labelWidth,
		float columnWidth, bool highlighted)
	{
		float textHeight = Text.CalcHeight(component.props.label, labelWidth);
		float labelHeight = Mathf.Max(rect.height, textHeight);
		Rect labelRect = new(rect.x, rect.y, labelWidth, labelHeight);

		if (highlighted)
		{
			//+16 for full coverage even if scrollbar is hidden
			Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width + 16, labelHeight),
				AlternatingColor);
		}

		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(labelRect, component.props.label);
		labelRect.x += labelRect.width;

		labelRect.width = columnWidth;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(labelRect,
			component.HealthPercent.ToStringPercent().Colorize(component.ComponentEfficiencyColor()));
		TooltipHandler.TipRegion(labelRect, $"{component.Health:F0}/{component.MaxHealth:F0}");
		labelRect.x += columnWidth;
		string efficiencyEntry;
		string efficiencyTooltip = null;
		if (!component.props.categories.NullOrEmpty())
		{
			efficiencyEntry = component.Efficiency.ToStringPercent().Colorize(component.ComponentEfficiencyColor());

			using ClearStringOnDispose csod = new(TooltipBuilder);
			TooltipBuilder.AppendLine("VF_EfficiencyEffector".Translate());
			foreach (VehicleStatDef statDef in component.props.categories)
			{
				TooltipBuilder.AppendLine($" - {statDef.LabelCap}");
			}
			efficiencyTooltip = TooltipBuilder.ToString();
		}
		else
		{
			efficiencyEntry = "-";
		}
		Widgets.Label(labelRect, efficiencyEntry);
		if (efficiencyTooltip != null)
		{
			TooltipHandler.TipRegion(labelRect, efficiencyTooltip);
		}

		if (!compressed && moreInfo)
		{
			foreach (DamageArmorCategoryDef armorCategoryDef in ArmorRatingDefs)
			{
				labelRect.x += columnWidth;
				float armorRating = component.ArmorRating(armorCategoryDef, out float upgraded);
				string armorLabel = armorRating.ToStringByStyle(armorCategoryDef.armorRatingStat.toStringStyle);
				armorLabel = armorLabel.Colorize(ArmorUpgradeQualityColor(upgraded));
				Widgets.Label(labelRect, armorLabel);
				if (!Mathf.Approximately(upgraded, 0))
				{
					string baseArmorReadout =
						(armorRating - upgraded).ToStringByStyle(armorCategoryDef.armorRatingStat.toStringStyle);
					TooltipHandler.TipRegion(labelRect, "VF_BaseArmorRating".Translate(baseArmorReadout));
				}
			}
		}

		Rect iconRect = new(labelRect.xMax, labelRect.y, ComponentIndicatorIconSize,
			ComponentIndicatorIconSize);
		component.DrawIcon(iconRect);

		return labelHeight;
	}

	private static Color ArmorUpgradeQualityColor(float upgraded)
	{
		return upgraded switch
		{
			< -0.5f               => HealthUtility.RedColor,
			>= -0.5f and < -0.25f => HealthUtility.ImpairedColor,
			>= -0.25f and < 0     => HealthUtility.SlightlyImpairedColor,
			0                     => HealthUtility.GoodConditionColor,
			> 0 and < 0.5f        => SlightlyUpgraded,
			>= 0.5f               => HeavilyUpgraded,
			_                     => HealthUtility.GoodConditionColor
		};
	}

	private static void RecacheWindowWidth()
	{
		size.x = LeftWindowWidth + LabelColumnWidth + ColumnCount * ColumnWidth +
			ComponentIndicatorIconSize + 20;
		if (!compressed && moreInfo)
		{
			size.x += ColumnWidth * ArmorRatingDefs.Count;
		}
	}

	private static void RecacheComponentListHeight(float lineHeight = ComponentRowHeight)
	{
		componentListHeight = 0;
		foreach (VehicleComponent component in inspectingVehicle.statHandler.components)
		{
			float textHeight = Text.CalcHeight(component.props.label, Size.x - LeftWindowWidth);
			componentListHeight += Mathf.Max(lineHeight, textHeight);
		}
	}

	public static Color ComponentEfficiencyColor(this VehicleComponent component)
	{
		return component.Efficiency switch
		{
			<= 0                 => Color.gray,
			> 0 and < 0.4f       => HealthUtility.RedColor,
			>= 0.4f and < 0.7f   => HealthUtility.ImpairedColor,
			>= 0.7f and < 0.999f => HealthUtility.SlightlyImpairedColor,
			_                    => HealthUtility.GoodConditionColor
		};
	}

	public readonly struct DrawBlock : IDisposable
	{
		public DrawBlock(VehiclePawn vehicle, bool compressed)
		{
			Start(vehicle, compressed);
		}

		void IDisposable.Dispose()
		{
			End();
		}
	}
}