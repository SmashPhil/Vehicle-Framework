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

		public ITab_Vehicle_Health()
		{
			size = new Vector2(720, 430);
			labelKey = "TabComponents";
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		/// <summary>
		/// Recache height every time vehicle health tab is opened
		/// </summary>
		public override void OnOpen()
		{
			base.OnOpen();
			VehicleHealthTabHelper.InitHealthITab();
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

		protected override void FillTab()
		{
			PushGUIStatus();
			
			try
			{
				Rect rect = new Rect(0, 20, size.x, size.y - 20).Rounded();

				Rect infoPanelRect = new Rect(rect.x, rect.y, rect.width * 0.375f, rect.height).Rounded();
				Rect componentPanelRect = new Rect(infoPanelRect.xMax, rect.y, rect.width - infoPanelRect.width, rect.height);
				infoPanelRect.yMin += 11f; //Extra space for tab, excluded from componentPanelRect for top options

				VehicleHealthTabHelper.DrawHealthInfo(infoPanelRect, vehicle: Vehicle);
				ResetGUI();
				VehicleHealthTabHelper.DrawComponentsInfo(componentPanelRect, vehicle: Vehicle);
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
