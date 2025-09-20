using RimWorld;
using UnityEngine;

namespace Vehicles;

public class ITab_Vehicle_Health : ITab
{
	private static readonly Vector2 PanelSize = new(100, 100);

	public ITab_Vehicle_Health()
	{
		size = PanelSize;
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
		UpdateSize();
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		Vehicle.HighlightedComponent = null;
	}

	protected override void FillTab()
	{
		Vector2 panelSize = VehicleTabHelper_Health.Start(Vehicle);
		if (size != panelSize)
		{
			size = panelSize;
			UpdateSize();
		}
		VehicleTabHelper_Health.DrawHealthPanel(Vehicle);
		VehicleTabHelper_Health.End();
	}

	public enum VehicleHealthTab
	{
		Overview,
		JobSettings
	}
}