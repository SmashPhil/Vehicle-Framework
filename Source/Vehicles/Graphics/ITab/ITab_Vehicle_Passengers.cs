using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

public class ITab_Vehicle_Passengers : ITab
{
	public const float PawnRowHeight = 50;

	public const float WindowWidth = 520;
	public const float WindowHeight = 450;

	private static List<Need> tmpNeeds = [];

	private Vector2 scrollPosition;
	private Vector2 thoughtScrollPosition;
	private float scrollViewHeight;

	private VehicleRoleHandler editingPawnOverlayRenderer;
	private Pawn specificNeedsTabForPawn;

	public ITab_Vehicle_Passengers()
	{
		size = new Vector2(WindowWidth, WindowHeight);
		labelKey = "VF_TabPassengers";
	}

	public VehiclePawn Vehicle => SelPawn as VehiclePawn;

	private float SpecificNeedsTabWidth => specificNeedsTabForPawn.DestroyedOrNull() ?
		0f :
		NeedsCardUtility.GetSize(specificNeedsTabForPawn).x;

	public override bool IsVisible
	{
		get { return !Vehicle.beached; }
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		CursorSettings.Reset();
	}

	protected override void FillTab()
	{
		EnsureSpecificNeedsTabForPawnValid();

		using TextBlock textFont = new(GameFont.Small);
		Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
		Rect viewRect = new(0f, 0f, rect.width - 16f, scrollViewHeight);

		float curY = 0f;
		// Begin ScrollView
		Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
		using (new VehicleTabHelper_Passenger.DrawBlock())
		{
			VehicleTabHelper_Passenger.DrawPassengersFor(ref curY, viewRect, scrollPosition, Vehicle,
				ref specificNeedsTabForPawn);
			VehicleTabHelper_Passenger.ListPawns(ref curY, viewRect, scrollPosition, Vehicle.inventory,
				"VF_Caravan_Cargo".Translate(), Vehicle.AllInventoryPawns, ref specificNeedsTabForPawn);
		}
		Widgets.EndScrollView();
		// End ScrollView

		if (!Mouse.IsOver(rect) && !Input.GetMouseButton(0))
		{
			CursorSettings.Reset();
			VehicleTabHelper_Passenger.Clear();
		}
		if (Event.current.type is EventType.Layout)
		{
			scrollViewHeight = curY + 30f;
		}
	}

	protected override void UpdateSize()
	{
		EnsureSpecificNeedsTabForPawnValid();
		base.UpdateSize();

		size = VehicleTabHelper_Passenger.GetSize(Vehicle.AllPawnsAboard.Concat(Vehicle.AllInventoryPawns), PaneTopY);
		size.y = Mathf.Max(size.y, NeedsCardUtility.FullSize.y);
	}

	protected override void ExtraOnGUI()
	{
		EnsureSpecificNeedsTabForPawnValid();
		base.ExtraOnGUI();
		if (specificNeedsTabForPawn != null)
		{
			Rect tabRect = TabRect;
			float specificNeedsTabWidth = SpecificNeedsTabWidth;
			Rect rect = new(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth,
				tabRect.height);
			Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
			{
				if (specificNeedsTabForPawn.DestroyedOrNull())
				{
					return;
				}
				NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), specificNeedsTabForPawn,
					ref thoughtScrollPosition);
				if (Widgets.CloseButtonFor(rect.AtZero()))
				{
					specificNeedsTabForPawn = null;
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
			});
		}
		else if (editingPawnOverlayRenderer != null)
		{
			Rect pawnOverlayRect = new(size.x + 1, TabRect.yMin, 600, size.y);
			Find.WindowStack.ImmediateWindow(
				editingPawnOverlayRenderer.role.GetHashCode() ^ Vehicle.GetHashCode(), pawnOverlayRect,
				WindowLayer.GameUI, delegate
				{
					if (editingPawnOverlayRenderer is null ||
						editingPawnOverlayRenderer.vehicle.DestroyedOrNull())
					{
						return;
					}
					Rect rendererRect = new Rect(0, 0, pawnOverlayRect.width, pawnOverlayRect.height)
					 .ContractedBy(5);
					editingPawnOverlayRenderer.role.PawnRenderer.RenderEditor(rendererRect);
					if (Widgets.CloseButtonFor(rendererRect))
					{
						editingPawnOverlayRenderer = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera();
					}
				});
		}
	}

	public override void Notify_ClearingAllMapsMemory()
	{
		base.Notify_ClearingAllMapsMemory();
		specificNeedsTabForPawn = null;
	}

	private void EnsureSpecificNeedsTabForPawnValid()
	{
		if (specificNeedsTabForPawn is not { Destroyed: false })
			return;

		if (!Vehicle.AllPawnsAboard.Contains(specificNeedsTabForPawn) &&
			!Vehicle.AllInventoryPawns.Contains(specificNeedsTabForPawn))
		{
			specificNeedsTabForPawn = null;
		}
		if (editingPawnOverlayRenderer != null && (specificNeedsTabForPawn != null ||
			editingPawnOverlayRenderer.thingOwner.Count == 0))
		{
			editingPawnOverlayRenderer = null;
		}
	}
}