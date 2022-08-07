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
		public const float ComponentRowHeight = 20;

		private static readonly Color MouseOverColor = new Color(0.75f, 0.75f, 0.75f, 0.1f);
		private static readonly Color SelectedCompColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);

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

		public static void DrawComponentsInfo(Rect rect, VehiclePawn vehicle)
		{
			Text.Font = GameFont.Small;
			float textHeight = Text.CalcSize("VehicleComponentHealth".Translate()).y;
			Rect topLabelRect = new Rect(rect.x, rect.y, rect.width / 4, textHeight);

			topLabelRect.x += topLabelRect.width;
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

			rect.y += textHeight / 1.25f;
			rect.x += 2.5f;
			rect.width -= 5;

			float totalHeight = 0;
			foreach (VehicleComponent component in vehicle.statHandler.components)
			{
				//float textHeight = Text.CalcHeight(component.props.label, labelWidth);
				//float labelHeight = Mathf.Max(rect.height, textHeight);
				//vehicle.statHandler.components.Count* ComponentRowHeight
			}

			Rect scrollView = new Rect(rect.x, topLabelRect.y + topLabelRect.height * 2, rect.width, vehicle.statHandler.components.Count * ComponentRowHeight);

			Widgets.BeginScrollView(rect, ref componentTabScrollPos, scrollView);
			{
				highlightedComponent = null;
				float buttonY = scrollView.y;
				bool highlighted = false;
				foreach (VehicleComponent component in vehicle.statHandler.components)
				{
					Rect compRect = new Rect(rect.x, buttonY, rect.width, textHeight);
					DrawCompRow(compRect, component);
					TooltipHandler.TipRegion(compRect, "VehicleComponentClickMoreInfo".Translate());
					if (Mouse.IsOver(compRect))
					{
						highlightedComponent = component;
						Rect highlightRect = new Rect(compRect)
						{
							width = rect.width
						};
						Widgets.DrawBoxSolid(highlightRect, MouseOverColor);
						//For debug drawing of component hitbox
						vehicle.HighlightedComponent = component;
						highlighted = true;
					}
					else if (selectedComponent == component)
					{
						Widgets.DrawBoxSolid(compRect, SelectedCompColor);
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
					buttonY += ComponentRowHeight;
				}
				if (!highlighted)
				{
					vehicle.HighlightedComponent = null;
				}
			}
			Widgets.EndScrollView();
		}

		private static float DrawCompRow(Rect rect, VehicleComponent component)
		{
			float labelWidth = rect.width / 4;
			float textHeight = Text.CalcHeight(component.props.label, labelWidth);
			float labelHeight = Mathf.Max(rect.height, textHeight);
			Rect labelRect = new Rect(rect.x, rect.y, labelWidth, labelHeight);

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, component.props.label);
			labelRect.x += labelRect.width;

			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(labelRect, component.HealthPercentStringified);
			labelRect.x += labelRect.width;
			Widgets.Label(labelRect, component.EfficiencyPercent);
			labelRect.x += labelRect.width;
			Widgets.Label(labelRect, component.ArmorPercent);

			return labelHeight;
		}
	}
}
