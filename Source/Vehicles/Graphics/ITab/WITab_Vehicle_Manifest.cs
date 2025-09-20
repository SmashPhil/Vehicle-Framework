using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace Vehicles.World;

// ReSharper disable once InconsistentNaming
// It's cleaner to just stick with RimWorld's naming convention when deriving from their types.
public class WITab_Vehicle_Manifest : WITab
{
	private Vector2 scrollPosition;
	private Vector2 thoughtScrollPosition;
	private float scrollViewHeight;

	private Pawn moreDetailsForPawn;
	private readonly List<Pawn> dismountedPawns = [];
	private bool canUseCargoHold;
	private IVehicleWorldObject cachedSelObject;

	public WITab_Vehicle_Manifest()
	{
		size = new Vector2(ITab_Vehicle_Passengers.WindowWidth, ITab_Vehicle_Passengers.WindowHeight);
		labelKey = "VF_TabPassengers";
	}

	public override bool IsVisible => true;

	public IVehicleWorldObject VehicleObject => SelObject as IVehicleWorldObject;

	private float MoreDetailsWidth
	{
		get
		{
			if (moreDetailsForPawn.DestroyedOrNull())
			{
				return 0;
			}
			if (moreDetailsForPawn is VehiclePawn)
			{
				return VehicleTabHelper_Health.Size.x;
			}
			return NeedsCardUtility.GetSize(moreDetailsForPawn).x;
		}
	}

	private void RecachePawnLists()
	{
		cachedSelObject = VehicleObject;
		dismountedPawns.Clear();

		dismountedPawns.AddRange(VehicleObject.DismountedPawns);
		canUseCargoHold = VehicleObject.Vehicles.NotNullAndAny(HasCargoPawn) || dismountedPawns.Exists(IsCargoPawn);
		return;

		static bool HasCargoPawn(VehiclePawn vehicle)
		{
			return vehicle.AllInventoryPawns.Count > 0;
		}

		static bool IsCargoPawn(Pawn pawn)
		{
			return pawn.CanBeTransferredToVehiclesCargo();
		}
	}

	/// <summary>
	/// Recache height on open
	/// </summary>
	public override void OnOpen()
	{
		base.OnOpen();
		Assert.IsNotNull(VehicleObject);
		RecachePawnLists();
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		CursorSettings.Reset();
	}

	protected override void FillTab()
	{
		if (cachedSelObject != VehicleObject)
		{
			RecachePawnLists();
		}
		EnsureSpecificNeedsTabForPawnValid();

		using TextBlock textFont = new(GameFont.Small);
		Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
		Rect viewRect = new(0f, 0f, rect.width - 16f, scrollViewHeight);

		// Begin ScrollView
		Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
		float yMax = DrawTab(viewRect);
		Widgets.EndScrollView();
		// End ScrollView

		if (!Mouse.IsOver(rect) && !Input.GetMouseButton(0))
		{
			CursorSettings.Reset();
			VehicleTabHelper_Passenger.Clear();
		}
		if (VehicleTabHelper_Passenger.PawnSeatChanged)
		{
			RecachePawnLists();
		}
		if (Event.current.type is EventType.Layout)
		{
			scrollViewHeight = yMax + 30f;
		}
	}

	private float DrawTab(Rect viewRect)
	{
		float curY = 0;
		using VehicleTabHelper_Passenger.DrawBlock db = new();
		foreach (VehiclePawn vehicle in VehicleObject.Vehicles)
		{
			Color baseColor = vehicle != moreDetailsForPawn ? Color.white : Color.green;
			Color mouseoverColor = vehicle != moreDetailsForPawn ?
				GenUI.MouseoverColor :
				new Color(0f, 0.5f, 0f);
			if (SectionLabel(viewRect, ref curY, vehicle.Label, baseColor, mouseoverColor,
				CaravanThingsTabUtility.SpecificTabButtonTex))
			{
				if (vehicle == moreDetailsForPawn)
				{
					moreDetailsForPawn = null;
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
				else
				{
					moreDetailsForPawn = vehicle;
					VehicleTabHelper_Health.Init();
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
				}
			}
			VehicleTabHelper_Passenger.DrawPassengersFor(ref curY, viewRect, scrollPosition, vehicle,
				ref moreDetailsForPawn);
			if (canUseCargoHold)
			{
				VehicleTabHelper_Passenger.ListPawns(ref curY, viewRect, scrollPosition, vehicle.inventory,
					"VF_Caravan_Cargo".Translate(), vehicle.AllInventoryPawns, ref moreDetailsForPawn);
			}
		}

		if (VehicleObject.CanDismount)
		{
			SectionLabel(viewRect, ref curY, "VF_Caravan_Dismounted".Translate());
			VehicleTabHelper_Passenger.ListPawns(ref curY, viewRect, scrollPosition, VehicleObject,
				string.Empty, dismountedPawns, ref moreDetailsForPawn);
		}
		return curY;
	}

	protected override void ExtraOnGUI()
	{
		EnsureSpecificNeedsTabForPawnValid();
		base.ExtraOnGUI();
		if (moreDetailsForPawn != null)
		{
			Rect tabRect = TabRect;
			Rect rect = new(tabRect.xMax - 1f, tabRect.yMin, MoreDetailsWidth, tabRect.height);
			Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
			{
				if (moreDetailsForPawn.DestroyedOrNull())
					return;

				DrawMoreDetailsWindow(rect.AtZero());

				if (Widgets.CloseButtonFor(rect.AtZero()))
				{
					moreDetailsForPawn = null;
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
			});
		}
	}

	private void DrawMoreDetailsWindow(Rect rect)
	{
		if (moreDetailsForPawn is VehiclePawn vehicle)
		{
			VehicleTabHelper_Health.Start(vehicle, compressed: true);
			{
				VehicleTabHelper_Health.DrawHealthPanel(vehicle);
			}
			VehicleTabHelper_Health.End();
		}
		else
		{
			NeedsCardUtility.DoNeedsMoodAndThoughts(rect, moreDetailsForPawn, ref thoughtScrollPosition);
		}
	}

	private static bool SectionLabel(Rect viewRect, ref float curY, string label,
		Texture2D buttonTex = null)
	{
		return SectionLabel(viewRect, ref curY, label, Color.white, GenUI.MouseoverColor,
			buttonTex: buttonTex);
	}

	private static bool SectionLabel(Rect viewRect, ref float curY, string label, Color baseColor,
		Color mouseoverColor, Texture2D buttonTex = null)
	{
		using TextBlock textAnchor = new(GameFont.Medium, TextAnchor.UpperCenter);

		bool clicked = false;
		Rect labelRect = new(0, curY, viewRect.width, Text.CalcSize(label).y);
		Widgets.Label(labelRect, label.Truncate(viewRect.width));

		Rect buttonRect = new(labelRect.width - VehicleTabHelper_Passenger.PawnExtraButtonSize,
			curY, VehicleTabHelper_Passenger.PawnExtraButtonSize,
			VehicleTabHelper_Passenger.PawnExtraButtonSize);
		if (buttonTex != null)
		{
			if (Widgets.ButtonImageFitted(buttonRect, buttonTex, baseColor, mouseoverColor))
			{
				clicked = true;
			}
		}
		curY += labelRect.height;
		return clicked;
	}

	protected override void UpdateSize()
	{
		EnsureSpecificNeedsTabForPawnValid();
		base.UpdateSize();

		Vector2 preferredSize = new(ITab_Vehicle_Passengers.WindowWidth, 0);
		foreach (VehiclePawn vehicle in VehicleObject.Vehicles)
		{
			Vector2 sizeForVehicle = VehicleTabHelper_Passenger.GetSize(vehicle.AllPawnsAboard, PaneTopY);
			preferredSize.x = Mathf.Max(preferredSize.x, sizeForVehicle.x);
			preferredSize.y += sizeForVehicle.y;
		}
		Vector2 sizeForDismounts =
			VehicleTabHelper_Passenger.GetSize(VehicleObject.DismountedPawns, PaneTopY);
		preferredSize.x = Mathf.Max(preferredSize.x, sizeForDismounts.x);
		preferredSize.y += sizeForDismounts.y;

		size.y = Mathf.Max(size.y, NeedsCardUtility.FullSize.y);
	}

	private void EnsureSpecificNeedsTabForPawnValid()
	{
		if (moreDetailsForPawn != null)
		{
			bool destroyed = moreDetailsForPawn.Destroyed;
			//Destroyed or non-vehicle pawn
			if (destroyed || !(moreDetailsForPawn is VehiclePawn))
			{
				//Pawn not in vehicle or dismounted pawn list
				if (!VehicleObject.Vehicles.Any(vehicle =>
						vehicle.AllPawnsAboard.Contains(moreDetailsForPawn)) &&
					!VehicleObject.DismountedPawns.Contains(moreDetailsForPawn))
				{
					moreDetailsForPawn = null;
				}
			}
		}
	}
}