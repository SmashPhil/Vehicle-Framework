using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Dialog_ConfigureVehicleAreas : Window
	{
		private const float ColumnGap = 20;
		private const float RowHeight = 24;
		private const float IntBoxSize = 125;
		private const float ButtonWidth = 90;
		private const float ButtonHeight = 30;

		private Map map;
		private Area area;
		private List<TabRecord> tabs = new List<TabRecord>();

		private readonly List<VehiclePawn> vehicles = new List<VehiclePawn>();
		private readonly List<VehicleDef> vehicleDefs = new List<VehicleDef>();

		private Vector2 scrollPosition;
		private VehicleConfigTab curTab;

		private VehiclePawn selectedVehicle;
		private VehicleDef selectedVehicleDef;

		public Dialog_ConfigureVehicleAreas(Map map, Area area)
		{
			this.map = map;
			this.area = area;
			tabs = new List<TabRecord>()
			{
				new TabRecord("VF_Vehicles".Translate(), delegate()
				{
					CurrentTab = VehicleConfigTab.Vehicles;
				}, () => CurrentTab == VehicleConfigTab.Vehicles),
				new TabRecord("VehicleDefs".Translate(), delegate ()
				{
					CurrentTab = VehicleConfigTab.VehicleDefs;
				}, () => CurrentTab == VehicleConfigTab.VehicleDefs)
			};

			CurrentTab = VehicleConfigTab.Vehicles;

			forcePause = true;
			closeOnClickedOutside = true;
			doCloseButton = true;

			vehicles = map.AllPawnsOnMap<VehiclePawn>(Faction.OfPlayer);
			vehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading;

			RecalculateHeight();
		}

		public override Vector2 InitialSize => new Vector2(500, 650);

		private VehicleConfigTab CurrentTab
		{
			get
			{
				return curTab;
			}
			set
			{
				if (curTab == value)
				{
					return;
				}
				curTab = value;
				RecalculateHeight();
			}
		}

		private float ScrollViewHeight { get; set; }

		public override void DoWindowContents(Rect inRect)
		{
			Rect menuRect = inRect.ContractedBy(10);
			menuRect.y += 32;
			menuRect.height -= (32 + ButtonHeight);

			Widgets.DrawMenuSection(menuRect);
			TabDrawer.DrawTabs(menuRect, tabs, 200f);
			Rect contentRect = new Rect(menuRect)
			{
				y = menuRect.y,
				height = menuRect.height - ButtonHeight
			};
			DrawVehicleConfigs(contentRect);
		}

		private void RecalculateHeight()
		{
			ScrollViewHeight = 0;
			switch (CurrentTab)
			{
				case VehicleConfigTab.Vehicles:
					ScrollViewHeight = vehicles.Count * (WidgetRowButBetter.IconSize + WidgetRowButBetter.DefaultGap);
					break;
				case VehicleConfigTab.VehicleDefs:
					ScrollViewHeight = vehicleDefs.Count * (WidgetRowButBetter.IconSize + WidgetRowButBetter.DefaultGap);
					break;
			}
		}

		public void DrawVehicleConfigs(Rect rect)
		{
			Rect topRect = new Rect(rect)
			{
				height = rect.height / 3
			};
			
			Widgets.BeginGroup(topRect);
			Rect iconRect = new Rect(0, 0, topRect.height, topRect.height);

			//UIElements.DrawLineHorizontalGrey(0, 0, topRect.width);
			//UIElements.DrawLineVerticalGrey(iconRect.width, 0, iconRect.height);

			bool disabled = selectedVehicle is null && selectedVehicleDef is null;
			int cost = 0;
			
			switch (CurrentTab)
			{
				case VehicleConfigTab.Vehicles:
					if (selectedVehicle != null)
					{
						cost = map.GetCachedMapComponent<VehicleAreaManager>().AdditionalTerrainCost(selectedVehicle, area);
						VehicleGUI.DrawVehicleDefOnGUI(iconRect, selectedVehicle.VehicleDef, selectedVehicle.patternData);
					}
					break;
				case VehicleConfigTab.VehicleDefs:
					if (selectedVehicleDef != null)
					{
						cost = map.GetCachedMapComponent<VehicleAreaManager>().AdditionalTerrainCost(selectedVehicleDef, area);
						VehicleGUI.DrawVehicleDefOnGUI(iconRect, selectedVehicleDef);
					}
					break;
			}
			cost = cost.Clamp(VehicleAreaManager.AreaPathConfig.MinPathCost, 55);
			bool enabled = GUI.enabled;
			GUI.enabled = !disabled;
			Rect areaConfigRect = new Rect(iconRect.width, 0, topRect.width - iconRect.width, iconRect.height).ContractedBy(10);
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(areaConfigRect);
			//listingStandard.CheckboxLabeledReturned("Impassable", ref )
			int costBefore = cost;
			listingStandard.SliderLabeled("Path Cost", "Something something tooltip", string.Empty, ref cost, -50, 55, 5, "Impassable".Translate());
			bool changeInCost = costBefore != cost;
			listingStandard.End();
			GUI.enabled = enabled;
			Widgets.EndGroup();

			switch (CurrentTab)
			{
				case VehicleConfigTab.Vehicles:
					if (selectedVehicle != null)
					{
						if (changeInCost)
						{
							map.GetCachedMapComponent<VehicleAreaManager>().UpdateArea(selectedVehicle, area, cost);
						}
					}
					break;
				case VehicleConfigTab.VehicleDefs:
					if (selectedVehicleDef != null)
					{
						if (changeInCost)
						{
							map.GetCachedMapComponent<VehicleAreaManager>().UpdateArea(selectedVehicleDef, area, cost);
						}
					}
					break;
			}

			
			Rect outRect = new Rect(rect)
			{
				y = topRect.yMax,
				height = rect.height * 2 / 3
			};
			Rect bottomRect = outRect;
			Rect viewRect = new Rect(outRect)
			{
				height = ScrollViewHeight
			};
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			Widgets.DrawMenuSection(bottomRect);
			bottomRect.width -= 16; //Scrollbar handle
			Widgets.BeginGroup(bottomRect);
			float rowY = 0;
			switch (CurrentTab)
			{
				case VehicleConfigTab.Vehicles:
					
					break;
				case VehicleConfigTab.VehicleDefs:
					foreach (VehicleDef vehicleDef in vehicleDefs)
					{
						WidgetRowButBetter widgetRow = new WidgetRowButBetter(0, rowY, UIDirection.RightThenUp);
						float width = bottomRect.width - widgetRow.CurX - (WidgetRowButBetter.IconSize + widgetRow.CellGap + WidgetRowButBetter.LabelGap * 2) * 2;
						widgetRow.Label(vehicleDef.LabelCap, width);
						int currentCost = map.GetCachedMapComponent<VehicleAreaManager>().AdditionalTerrainCost(vehicleDef, area);
						widgetRow.Label(currentCost > VehicleAreaManager.AreaPathConfig.MaxPathCost ? "\u221E" : currentCost.ToString(), WidgetRowButBetter.IconSize);
						bool appliesTo = currentCost != 0;
						if (widgetRow.Checkbox(ref appliesTo) && !appliesTo)
						{
							map.GetCachedMapComponent<VehicleAreaManager>().UpdateArea(vehicleDef, area, 0);
						}
						if (widgetRow.RowSelect(selectedVehicleDef == vehicleDef))
						{
							selectedVehicleDef = vehicleDef;
						}
						rowY += RowHeight;
					}
					break;
			}
			Widgets.EndGroup();
			Widgets.EndScrollView();
		}

		private enum VehicleConfigTab
		{
			Vehicles,
			VehicleDefs
		}
	}
}
