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
		public static readonly Vector2 panelSize = new Vector2(100, 100);

		public ITab_Vehicle_Health()
		{
			size = panelSize;
			labelKey = "VF_TabComponents";
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		/// <summary>
		/// Recache height every time vehicle health tab is opened
		/// </summary>
		public override void OnOpen()
		{
			base.OnOpen();
			VehicleTabHelper_Health.Init();
		}

		protected override void CloseTab()
		{
			base.CloseTab();
			Vehicle.HighlightedComponent = null;
		}

		protected override void FillTab()
		{
			Vector2 size = VehicleTabHelper_Health.Start(Vehicle);
			{
				if (this.size != size)
				{
					this.size = size;
					UpdateSize();
				}
				VehicleTabHelper_Health.DrawHealthPanel(Vehicle);
			}
			VehicleTabHelper_Health.End();
		}

		public enum VehicleHealthTab
		{
			Overview,
			JobSettings
		}
	}
}
