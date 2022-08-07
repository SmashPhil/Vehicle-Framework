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
		private GameFont originalFont;
		private TextAnchor originalAnchor;
		private Color originalGUIColor;

		private float componentListHeight;

		private static VehiclePawn inspectingVehicle;

		public ITab_Vehicle_Health()
		{
			size = new Vector2(720, 430);
			labelKey = "TabComponents";
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		public (float left, float right) PanelWidths => (size.x * 0.375f, size.x * 0.625f);

		/// <summary>
		/// Recache height every time vehicle health tab is opened
		/// </summary>
		public override void OnOpen()
		{
			base.OnOpen();
			VehicleHealthTabHelper.InitHealthITab();
			RecacheComponentListHeight(PanelWidths.right);
		}

		protected override void CloseTab()
		{
			base.CloseTab();
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

		private void RecacheComponentListHeight(float width, float lineHeight = VehicleHealthTabHelper.ComponentRowHeight)
		{
			componentListHeight = 0;
			foreach (VehicleComponent component in Vehicle.statHandler.components)
			{
				float textHeight = Text.CalcHeight(component.props.label, width);
				componentListHeight += Mathf.Max(lineHeight, textHeight);
			}
		}

		protected override void FillTab()
		{
			PushGUIStatus();
			
			try
			{
				if (Vehicle != inspectingVehicle)
				{
					//Not captured by OnOpen when switching between vehicles with ITab already open
					inspectingVehicle = Vehicle;
					RecacheComponentListHeight(PanelWidths.right);
				}
				Rect rect = new Rect(0, 20, size.x, size.y - 20);
				Rect infoPanelRect = new Rect(rect.x, rect.y, PanelWidths.left, rect.height).Rounded();
				Rect componentPanelRect = new Rect(infoPanelRect.xMax, rect.y, PanelWidths.right, rect.height);
				infoPanelRect.yMin += 11f; //Extra space for tab, excluded from componentPanelRect for top options

				VehicleHealthTabHelper.DrawHealthInfo(infoPanelRect, vehicle: Vehicle);
				ResetGUI();
				VehicleHealthTabHelper.DrawComponentsInfo(componentPanelRect, vehicle: Vehicle, componentViewHeight: componentListHeight);
			}
			finally
			{
				ResetGUI();
			}
		}

		public enum VehicleHealthTab
		{
			Overview,
			JobSettings
		}
	}
}
