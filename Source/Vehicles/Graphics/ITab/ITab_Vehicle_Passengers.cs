using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class ITab_Vehicle_Passengers : ITab
	{
		public const float PawnRowHeight = 50;

		public const float WindowWidth = 520;
		public const float WindowHeight = 450;

		private static List<Need> tmpNeeds = new List<Need>();

		private Vector2 scrollPosition;
		private Vector2 thoughtScrollPosition;
		private float scrollViewHeight;

		private VehicleHandler editingPawnOverlayRenderer;
		private Pawn specificNeedsTabForPawn;

		public ITab_Vehicle_Passengers()
		{
			size = new Vector2(WindowWidth, WindowHeight);
			labelKey = "VF_TabPassengers";
		}

		public VehiclePawn Vehicle => SelPawn as VehiclePawn;

		private float SpecificNeedsTabWidth => specificNeedsTabForPawn.DestroyedOrNull() ? 0f : NeedsCardUtility.GetSize(specificNeedsTabForPawn).x;

		public override bool IsVisible
		{
			get
			{
				return !Vehicle.beached;
			}
		}

		protected override void FillTab()
		{
			GUIState.Push();
			{
				EnsureSpecificNeedsTabForPawnValid();

				Text.Font = GameFont.Small;
				Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
				Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);

				float curY = 0f;
				Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
				{
					VehicleTabHelper_Passenger.Start();
					{
						VehicleTabHelper_Passenger.DrawPassengersFor(ref curY, viewRect, scrollPosition, Vehicle, ref specificNeedsTabForPawn);
					}
					VehicleTabHelper_Passenger.End();
				}
				Widgets.EndScrollView();

				if (Event.current.type is EventType.Layout)
				{
					scrollViewHeight = curY + 30f;
				}
			}
			GUIState.Pop();
		}

		protected override void UpdateSize()
		{
			EnsureSpecificNeedsTabForPawnValid();
			base.UpdateSize();

			size = VehicleTabHelper_Passenger.GetSize(Vehicle.AllPawnsAboard, PaneTopY);
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
				Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth, tabRect.height);
				Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
				{
					if (specificNeedsTabForPawn.DestroyedOrNull())
					{
						return;
					}
					NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), specificNeedsTabForPawn, ref thoughtScrollPosition);
					if (Widgets.CloseButtonFor(rect.AtZero()))
					{
						specificNeedsTabForPawn = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
				}, true, false, 1f);
			}
			else if (editingPawnOverlayRenderer != null)
			{
				Rect pawnOverlayRect = new Rect(size.x + 1, TabRect.yMin, 600, size.y);
				Find.WindowStack.ImmediateWindow(editingPawnOverlayRenderer.role.GetHashCode() ^ Vehicle.GetHashCode(), pawnOverlayRect, WindowLayer.GameUI, delegate ()
				{
					if (editingPawnOverlayRenderer is null || editingPawnOverlayRenderer.vehicle.DestroyedOrNull())
					{
						return;
					}
					Rect rendererRect = new Rect(0, 0, pawnOverlayRect.width, pawnOverlayRect.height).ContractedBy(5);
					editingPawnOverlayRenderer.role.PawnRenderer.RenderEditor(rendererRect);
					if (Widgets.CloseButtonFor(rendererRect))
					{
						editingPawnOverlayRenderer = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
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
			if (specificNeedsTabForPawn != null && (specificNeedsTabForPawn.Destroyed || !Vehicle.AllPawnsAboard.Contains(specificNeedsTabForPawn)))
			{
				specificNeedsTabForPawn = null;
			}
			if (editingPawnOverlayRenderer != null && (specificNeedsTabForPawn != null || editingPawnOverlayRenderer.handlers.Count == 0))
			{
				editingPawnOverlayRenderer = null;
			}
		}
	}
}