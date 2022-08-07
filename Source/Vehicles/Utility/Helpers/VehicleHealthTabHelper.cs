using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public static class VehicleHealthTabHelper
	{
		private static ITab_Vehicle_Health.VehicleHealthTab onTab;

		public static void DrawHealthInfo(Rect rect, VehiclePawn vehicle)
		{
			Widgets.DrawMenuSection(rect);
			List<TabRecord> list = new List<TabRecord>();
			list.Add(new TabRecord("HealthOverview".Translate(), delegate ()
			{
				onTab = ITab_Vehicle_Health.VehicleHealthTab.Overview;
			}, onTab == ITab_Vehicle_Health.VehicleHealthTab.Overview));
			if (vehicle.CompUpgradeTree != null)
			{
				list.Add(new TabRecord("VF_JobSettings".Translate(), delegate ()
				{
					onTab = ITab_Vehicle_Health.VehicleHealthTab.JobSettings;
				}, onTab == ITab_Vehicle_Health.VehicleHealthTab.JobSettings));
			}
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
						break;
				}
			}
			Widgets.EndGroup();
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
			foreach (VehicleStatDef statDef in vehicle.VehicleDef.StatCategoryDefs())
			{
				curY = statDef.Worker.DrawVehicleStat(statRect, curY, vehicle);
				statRect.y = curY;
			}
		}

		public static void DrawComponentsInfo(Rect rect, VehiclePawn vehicle)
		{
		}
	}
}
