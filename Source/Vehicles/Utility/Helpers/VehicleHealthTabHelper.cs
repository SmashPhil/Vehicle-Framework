using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Vehicles
{
	public static class VehicleHealthTabHelper
	{
		public const float ComponentRowHeight = 20f;
		public const float ComponentIndicatorIconSize = 20f;

		private static readonly Color MouseOverColor = new Color(0.85f, 0.85f, 0.85f, 0.1f);
		private static readonly Color AlternatingColor = new Color(0.75f, 0.75f, 0.75f, 0.1f);

		private static ITab_Vehicle_Health.VehicleHealthTab onTab;
		private static Vector2 componentTabScrollPos;
		private static VehicleComponent selectedComponent;
		private static VehicleComponent highlightedComponent;

		public static void InitHealthITab()
		{
			componentTabScrollPos = Vector2.zero;
			selectedComponent = null;
			highlightedComponent = null;
		}

		public static void DrawHealthInfo(Rect rect, VehiclePawn vehicle)
		{
			Widgets.DrawMenuSection(rect);
			List<TabRecord> list = new List<TabRecord>();
			list.Add(new TabRecord("HealthOverview".Translate(), delegate ()
			{
				onTab = ITab_Vehicle_Health.VehicleHealthTab.Overview;
			}, onTab == ITab_Vehicle_Health.VehicleHealthTab.Overview));
			list.Add(new TabRecord("VF_JobSettings".Translate(), delegate ()
			{
				onTab = ITab_Vehicle_Health.VehicleHealthTab.JobSettings;
			}, onTab == ITab_Vehicle_Health.VehicleHealthTab.JobSettings));
			TabDrawer.DrawTabs(rect, list);

			rect = rect.ContractedBy(9f);
			Widgets.BeginGroup(rect);
			{
				Text.Font = GameFont.Medium;
				GUI.color = Color.white;
				Text.Anchor = TextAnchor.UpperCenter;

				switch (onTab)
				{
					case ITab_Vehicle_Health.VehicleHealthTab.Overview:
						DrawVehicleInformation(rect, vehicle);
						break;
					case ITab_Vehicle_Health.VehicleHealthTab.JobSettings:
						DrawJobSettings(rect, vehicle);
						break;
				}
			}
			Widgets.EndGroup();
		}

		public static void DrawJobSettings(Rect leftRect, VehiclePawn vehicle)
		{

		}

		public static void DrawVehicleInformation(Rect leftRect, VehiclePawn vehicle)
		{
			float curY = 0;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = new Color(0.9f, 0.9f, 0.9f);

			Rect rect = new Rect(0f, curY, leftRect.width, 34f);

			Widgets.Label(rect, vehicle.LabelCap);
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => vehicle.ageTracker.AgeTooltipString, "HealthTab".GetHashCode());
				Widgets.DrawHighlight(rect);
			}
			GUI.color = Color.white;
			curY += 34;

			Text.Font = GameFont.Small;
			Rect statRect = new Rect(0, curY, leftRect.width, 34);
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
		/// <param name="componentViewHeight">Cached height of full component list, taking into account extra space of longer labels</param>
		public static void DrawComponentsInfo(Rect rect, VehiclePawn vehicle, float componentViewHeight)
		{
			Text.Font = GameFont.Small;
			float textHeight = Text.CalcSize("VehicleComponentHealth".Translate()).y;
			float columnWidth = 75 - (ComponentIndicatorIconSize / 3f);
			float labelWidth = rect.width - (columnWidth * 3) - ComponentIndicatorIconSize * 2;
			//Skip header for component name column
			Rect topLabelRect = new Rect(rect.x + labelWidth, rect.y, columnWidth, textHeight);

			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(topLabelRect, "VehicleComponentHealth".Translate());
			topLabelRect.x += topLabelRect.width;
			Widgets.Label(topLabelRect, "VehicleComponentEfficiency".Translate());
			topLabelRect.x += topLabelRect.width;
			Widgets.Label(topLabelRect, "VehicleComponentArmor".Translate());
			topLabelRect.x += topLabelRect.width;

			GUI.color = TexData.MenuBGColor;
			Widgets.DrawLineHorizontal(rect.x, topLabelRect.y + textHeight / 1.25f, rect.width);
			GUI.color = Color.white;

			rect.y += textHeight / 1.25f + 1; //+1 for H. line
			rect.x += 2.5f;
			rect.width -= 5;

			Rect scrollView = new Rect(rect.x, rect.y + topLabelRect.height * 2, rect.width, componentViewHeight);
			bool alternatingRow = false;
			Widgets.BeginScrollView(rect, ref componentTabScrollPos, scrollView);
			{
				highlightedComponent = null;
				float curY = scrollView.y;
				bool highlighted = false;
				foreach (VehicleComponent component in vehicle.statHandler.components)
				{
					Rect compRect = new Rect(rect.x, curY, rect.width - 16, ComponentRowHeight);
					float usedHeight = DrawCompRow(compRect, component, labelWidth, columnWidth, alternatingRow);
					//TooltipHandler.TipRegion(compRect, "VehicleComponentClickMoreInfo".Translate());
					Rect highlightingRect = new Rect(compRect)
					{
						height = usedHeight
					};
					if (Mouse.IsOver(highlightingRect))
					{
						highlightedComponent = component;
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
						SoundDefOf.Click.PlayOneShotOnCamera(null);
						if (selectedComponent != component)
						{
							selectedComponent = component;
						}
						else
						{
							selectedComponent = null;
						}
					}
					curY += usedHeight;
					alternatingRow = !alternatingRow;
				}
				if (!highlighted)
				{
					vehicle.HighlightedComponent = null;
				}
			}
			Widgets.EndScrollView();
		}

		private static float DrawCompRow(Rect rect, VehicleComponent component, float labelWidth, float columnWidth, bool highlighted)
		{
			float textHeight = Text.CalcHeight(component.props.label, labelWidth);
			float labelHeight = Mathf.Max(rect.height, textHeight);
			Rect labelRect = new Rect(rect.x, rect.y, labelWidth, labelHeight);

			if (highlighted)
			{
				//+16 for full coverage even if scrollbar is hidden
				Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width + 16, labelHeight), AlternatingColor);
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, component.props.label);
			labelRect.x += labelRect.width;

			labelRect.width = columnWidth;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(labelRect, component.HealthPercent.ToStringPercent().Colorize(component.ComponentEfficiencyColor()));
			labelRect.x += columnWidth;
			string efficiencyEntry = component.props.categories.NullOrEmpty() ? "-" : component.Efficiency.ToStringPercent().Colorize(component.ComponentEfficiencyColor());
			Widgets.Label(labelRect, efficiencyEntry);
			labelRect.x += columnWidth;
			Widgets.Label(labelRect, component.ArmorRating(null).ToStringPercent());
			labelRect.x += columnWidth;

			Rect iconRect = new Rect(labelRect.x, labelRect.y, ComponentIndicatorIconSize, ComponentIndicatorIconSize);
			component.DrawIcon(iconRect);

			return labelHeight;
		}

		public static Color ComponentEfficiencyColor(this VehicleComponent component)
		{
			float efficiency = component.Efficiency;

			if (efficiency <= 0)
			{
				return Color.gray;
			}
			else if (efficiency < 0.4f)
			{
				return HealthUtility.RedColor;
			}
			else if (efficiency < 0.7f)
			{
				return HealthUtility.ImpairedColor;
			}
			else if (efficiency < 0.999f)
			{
				return HealthUtility.SlightlyImpairedColor;
			}
			return HealthUtility.GoodConditionColor;
		}
	}
}
