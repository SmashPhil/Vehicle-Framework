using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class WITab_Vehicle_Manifest : WITab
	{
		private Vector2 scrollPosition;
		private Vector2 thoughtScrollPosition;
		private float scrollViewHeight;

		private Pawn moreDetailsForPawn;

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
					return VehicleTabHelper_Health.LeftWindowWidth;
				}
				return NeedsCardUtility.GetSize(moreDetailsForPawn).x;
			}
		}

		/// <summary>
		/// Recache height on open
		/// </summary>
		public override void OnOpen()
		{
			base.OnOpen();
			if (VehicleObject == null)
			{
				SmashLog.ErrorOnce($"{GetType()} used on world object not implementing <type>WorldObject</type>. This is not allowed, as inner vehicles are then unable to be fetched.", SelObject.GetHashCode());
				CloseTab();
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

				float curY = 0;
				GUIState.Push();
				Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
				{
					VehicleTabHelper_Passenger.Start();
					{
						foreach (VehiclePawn vehicle in VehicleObject.Vehicles)
						{
							Color baseColor = (vehicle != moreDetailsForPawn) ? Color.white : Color.green;
							Color mouseoverColor = (vehicle != moreDetailsForPawn) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
							if (SectionLabel(viewRect, ref curY, vehicle.Label, baseColor, mouseoverColor, CaravanThingsTabUtility.SpecificTabButtonTex))
							{
								if (vehicle == moreDetailsForPawn)
								{
									moreDetailsForPawn = null;
									SoundDefOf.TabClose.PlayOneShotOnCamera(null);
								}
								else
								{
									moreDetailsForPawn = vehicle;
									VehicleTabHelper_Health.Init();
									SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
								}
							}
							GUIState.Reset();

							VehicleTabHelper_Passenger.DrawPassengersFor(ref curY, viewRect, scrollPosition, vehicle, ref moreDetailsForPawn);
						}

						if (VehicleObject.CanDismount)
						{
							SectionLabel(viewRect, ref curY, "VF_Caravan_Dismounted".Translate());
							VehicleTabHelper_Passenger.ListPawns(ref curY, viewRect, scrollPosition, VehicleObject, string.Empty, VehicleObject.DismountedPawns.ToList(), ref moreDetailsForPawn);
						}
					}
					VehicleTabHelper_Passenger.End();
				}
				Widgets.EndScrollView();
				GUIState.Pop();

				if (Event.current.type is EventType.Layout)
				{
					scrollViewHeight = curY + 30f;
				}
			}
			GUIState.Pop();
		}

		protected override void ExtraOnGUI()
		{
			EnsureSpecificNeedsTabForPawnValid();
			base.ExtraOnGUI();
			if (moreDetailsForPawn != null)
			{
				Rect tabRect = TabRect;
				Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, MoreDetailsWidth, tabRect.height);
				Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
				{
					if (moreDetailsForPawn.DestroyedOrNull())
					{
						return;
					}

					DrawMoreDetailsWindow(rect.AtZero());

					if (Widgets.CloseButtonFor(rect.AtZero()))
					{
						moreDetailsForPawn = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
				});
			}
		}

		private void DrawMoreDetailsWindow(Rect rect)
		{
			if (moreDetailsForPawn is VehiclePawn vehicle)
			{
				VehicleTabHelper_Health.Start(vehicle);
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

		private bool SectionLabel(Rect viewRect, ref float curY, string label, Texture2D configureButton = null)
		{
			return SectionLabel(viewRect, ref curY, label, Color.white, GenUI.MouseoverColor, configureButton: configureButton);
		}

		private bool SectionLabel(Rect viewRect, ref float curY, string label, Color baseColor, Color mouseoverColor, Texture2D configureButton = null)
		{
			bool clicked = false;
			Text.Anchor = TextAnchor.UpperCenter;
			{
				Rect labelRect = new Rect(0, curY, viewRect.width, Text.CalcSize(label).y);
				Widgets.Label(labelRect, label.Truncate(viewRect.width));

				Rect buttonRect = new Rect(labelRect.width - VehicleTabHelper_Passenger.PawnExtraButtonSize, curY, VehicleTabHelper_Passenger.PawnExtraButtonSize, VehicleTabHelper_Passenger.PawnExtraButtonSize);
				if (configureButton != null)
				{
					if (Widgets.ButtonImageFitted(buttonRect, configureButton, baseColor, mouseoverColor))
					{
						clicked = true;

					}
					curY += labelRect.height; //New line only if configure button shown
				}
			}
			GUIState.Reset();

			return clicked;
		}

		protected override void UpdateSize()
		{
			EnsureSpecificNeedsTabForPawnValid();
			base.UpdateSize();

			Vector2 preferredSize = new Vector2(ITab_Vehicle_Passengers.WindowWidth, 0);
			foreach (VehiclePawn vehicle in VehicleObject.Vehicles)
			{
				Vector2 sizeForVehicle = VehicleTabHelper_Passenger.GetSize(vehicle.AllPawnsAboard, PaneTopY);
				preferredSize.x = Mathf.Max(preferredSize.x, sizeForVehicle.x);
				preferredSize.y += sizeForVehicle.y;
			}
			Vector2 sizeForDismounts = VehicleTabHelper_Passenger.GetSize(VehicleObject.DismountedPawns, PaneTopY);
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
					if (!VehicleObject.Vehicles.Any(vehicle => vehicle.AllPawnsAboard.Contains(moreDetailsForPawn)) && !VehicleObject.DismountedPawns.Contains(moreDetailsForPawn))
					{
						moreDetailsForPawn = null;
					}
				}
			}
		}
	}
}
