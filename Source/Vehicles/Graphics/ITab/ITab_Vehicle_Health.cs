using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class ITab_Vehicle_Health : ITab
	{
		private const float TopPadding = 35;
		private const float TabMaxWidth = 150;
		private const float ComponentRowHeight = 20;

		private static readonly Color MouseOverColor = new Color(0.75f, 0.75f, 0.75f, 0.1f);
		private static readonly Color SelectedCompColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);

		private float componentsHeight;

		private Listing_SplitColumns lister;

		private Vector2 scrollViewPosition;
		private VehicleComponent selectedComponent;
		private VehicleComponent highlightedComponent;

		private GameFont originalFont;
		private TextAnchor originalAnchor;
		private Color originalGUIColor;

		public ITab_Vehicle_Health()
		{
			size = new Vector2(630, 430);
			labelKey = "TabComponents";
			lister = new Listing_SplitColumns();
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		public VehicleComponent CurComponent => selectedComponent ?? highlightedComponent;

		/// <summary>
		/// Recache height every time vehicle health tab is opened
		/// </summary>
		public override void OnOpen()
		{
			base.OnOpen();
			componentsHeight = Vehicle.statHandler.components.Count * ComponentRowHeight;
		}

		protected override void CloseTab()
		{
			base.CloseTab();
			selectedComponent = null;
			Vehicle.HighlightedComponent = null;
		}

		private void PushGUIStatus()
		{
			originalFont = Text.Font;
			originalAnchor = Text.Anchor;
			originalGUIColor = GUI.color;
		}
		
		private void ResetGUI()
		{
			Text.Font = originalFont;
			Text.Anchor = originalAnchor;
			GUI.color = originalGUIColor;
		}

		protected override void FillTab()
		{
			PushGUIStatus();
			
			try
			{
				Rect rect = new Rect(0, 20, size.x, size.y - 20).Rounded();

				Rect infoPanelRect = new Rect(rect.x, rect.y, rect.width * 0.375f, rect.height).Rounded();
				Rect componentPanelRect = new Rect(infoPanelRect.xMin, rect.y, rect.width - infoPanelRect.width, rect.height);
				infoPanelRect.yMin += 11f; //Extra space for tab, excluded from componentPanelRect for top options

				VehicleHealthTabHelper.DrawHealthInfo(infoPanelRect, vehicle: Vehicle);
				ResetGUI();
				VehicleHealthTabHelper.DrawComponentsInfo(componentPanelRect, vehicle: Vehicle);

				return;
				Text.Font = GameFont.Small;
				float textHeight = Text.CalcHeight("Part", 999);
				Rect topLabelRect = new Rect(componentPanelRect.x, componentPanelRect.y, componentPanelRect.width / 4, textHeight);

				topLabelRect.x += topLabelRect.width;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(topLabelRect, "VehicleComponentHealth".Translate());
				topLabelRect.x += topLabelRect.width;
				Widgets.Label(topLabelRect, "VehicleComponentEfficiency".Translate());
				topLabelRect.x += topLabelRect.width;
				Widgets.Label(topLabelRect, "VehicleComponentArmor".Translate());
				topLabelRect.x += topLabelRect.width;

				GUI.color = TexData.MenuBGColor;
				//Widgets.DrawLineHorizontal(0, topLabelRect.y + textHeight / 1.25f, InfoPanelWidth);
				GUI.color = Color.white;

				componentPanelRect.y += textHeight / 1.25f;
				Rect scrollView = new Rect(componentPanelRect.x, topLabelRect.y + topLabelRect.height * 2, 0/*InfoPanelWidth*/, componentsHeight);

				Widgets.BeginScrollView(componentPanelRect, ref scrollViewPosition, scrollView);
				highlightedComponent = null;
				float buttonY = scrollView.y;
				bool highlighted = false;
				foreach (VehicleComponent component in Vehicle.statHandler.components)
				{
					Rect compRect = new Rect(componentPanelRect.x, buttonY, componentPanelRect.width, ComponentRowHeight);
					DrawCompRow(compRect, component);
					TooltipHandler.TipRegion(compRect, "VehicleComponentClickMoreInfo".Translate());
					if (Mouse.IsOver(compRect))
					{
						highlightedComponent = component;
						Rect highlightRect = new Rect(compRect)
						{
							x = 0,
							width = 0/*InfoPanelWidth*/
						};
						Widgets.DrawBoxSolid(highlightRect, MouseOverColor);
						/* For Debug Drawing */
						Vehicle.HighlightedComponent = component;
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
					Vehicle.HighlightedComponent = null;
				}
				Widgets.EndScrollView();

				Rect detailWindowRect = new Rect(infoPanelRect.width, infoPanelRect.y, rect.width - infoPanelRect.width, rect.height).ContractedBy(5);
				DrawDetailedComponents(detailWindowRect);
			}
			finally
			{
				ResetGUI();
			}
		}

		private void DrawCompRow(Rect rect, VehicleComponent component)
		{
			Rect labelRect = new Rect(rect.x, rect.y, rect.width / 4, rect.height);

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, component.props.label);
			labelRect.x += labelRect.width;

			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(labelRect, component.HealthPercentStringified);
			labelRect.x += labelRect.width;
			Widgets.Label(labelRect, component.EfficiencyPercent);
			labelRect.x += labelRect.width;
			Widgets.Label(labelRect, component.ArmorPercent);
		}

		private void DrawDetailedComponents(Rect rect)
		{
			var inspectedComponent = highlightedComponent ?? selectedComponent; 
			Rect topRect = new Rect(rect)
			{
				height = rect.height / 2
			};

			lister.Begin(topRect, 1);
			Text.Anchor = TextAnchor.MiddleLeft;
			foreach (VehicleStatDef statDef in Vehicle.VehicleDef.StatCategoryDefs())
			{
				//statDef.Worker.DrawVehicleStat(lister, Vehicle);
			}
			lister.End();
			Widgets.DrawLineHorizontal(rect.x, topRect.y + topRect.height, topRect.width);
			if (inspectedComponent != null)
			{
				Rect bottomRect = new Rect(topRect)
				{
					y = topRect.y + topRect.height + 10
				};

				lister.Begin(bottomRect, 1);
				lister.FillableBarLabeled(inspectedComponent.HealthPercent, "Health", TexData.RedTex, TexData.RedAddedStatBarTexture, 
					TexData.FillableBarInnerTex, TexData.FillableBarBackgroundTex, null, 0, 0 , new float[] { 20, 40, 60, 80, 100});

				lister.End();
			}
		}

		public enum VehicleHealthTab
		{
			Overview,
			JobSettings
		}
	}
}
