using System;
using System.Collections.Generic;
using CoreLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

[PublicAPI]
public static class VehicleTabHelper_Passenger
{
	private const float PawnRowHeight = 50;
	private const float PawnRowPadding = 4;
	private const float ThingIconSize = 27;
	public const float PawnExtraButtonSize = 24;
	private const float LabelWidth = 100;

	private static readonly List<Need> TmpNeeds = [];

	//private static VehicleHandler editingPawnOverlayRenderer;
	private static Pawn draggedPawn;
	private static IThingHolder transferToHolder;
	private static Pawn hoveringOverPawn;

	private static bool overDropSpot;
	private static bool pawnSeatChanged;

	private static bool drawing;
	private static MouseState mouseState;
	private static MouseState desiredMouseState;

	public static bool PawnSeatChanged => pawnSeatChanged;

	public static void Start()
	{
		if (drawing)
		{
			Trace.Fail(
				$"{nameof(VehicleTabHelper_Passenger)} is not re-entrant and can only be called after the previous draw cycle has finished.");
			return;
		}
		drawing = true;
		overDropSpot = false;
		pawnSeatChanged = false;
		desiredMouseState = MouseState.None;
	}

	public static void End()
	{
		HandleDragEvent();

		if (!overDropSpot)
		{
			transferToHolder = null;
		}

		if (desiredMouseState != mouseState)
		{
			mouseState = desiredMouseState;
			switch (mouseState)
			{
				case MouseState.HoverOver:
					CursorSettings.SetCursor(CursorSettings.Type.OpenHand);
				break;
				case MouseState.Dragging:
					CursorSettings.SetCursor(CursorSettings.Type.CloseHand);
				break;
				case MouseState.None:
					CursorSettings.Reset();
				break;
			}
		}

		drawing = false;
	}

	public static void Clear()
	{
		draggedPawn = null;
	}

	/// <summary>
	/// Lists all pawns inside <paramref name="vehicle"/>
	/// </summary>
	/// <returns>Height used up for list</returns>
	public static void DrawPassengersFor(ref float curY, Rect viewRect, Vector2 scrollPos,
		VehiclePawn vehicle, ref Pawn moreDetailsForPawn)
	{
		foreach (VehicleRoleHandler handler in vehicle.handlers)
		{
			List<Pawn> pawns = handler.thingOwner.InnerListForReading;

			overDropSpot |= ListPawns(ref curY, viewRect, scrollPos, handler, handler.role.label, pawns,
				ref moreDetailsForPawn);
		}
	}

	public static bool ListPawns(ref float curY, Rect viewRect, Vector2 scrollPos,
		IThingHolder holder, string label, List<Pawn> pawns, ref Pawn moreDetailsForPawn)
	{
		bool overHandler = false;
		Rect handlerRect = new(0, curY, viewRect.width - PawnExtraButtonSize * 2,
			(PawnRowHeight / 2) + (PawnRowHeight * pawns.Count));
		if (draggedPawn != null && Mouse.IsOver(handlerRect) && draggedPawn.ParentHolder != holder)
		{
			transferToHolder = holder;
			overHandler = true;
			Widgets.DrawHighlight(handlerRect);
		}

		Widgets.ListSeparator(ref curY, viewRect.width, label);

		// TODO - implement runtime editing of pawn renderer positions for modders
		//if (holder is VehicleHandler handler && handler?.role.PawnRenderer != null && Prefs.DevMode && DebugSettings.godMode)
		//{
		//	Rect editPawnOverlayRect = new Rect(viewRect.width - 15, curY + 3, 15, 15);
		//	TooltipHandler.TipRegionByKey(editPawnOverlayRect, "VF_EditPawnOverlayRendererTooltip");
		//	Color baseColor = (editingPawnOverlayRenderer != handler) ? Color.white : Color.green;
		//	Color mouseoverColor = (editingPawnOverlayRenderer != handler) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
		//	if (Widgets.ButtonImage(editPawnOverlayRect, VehicleTex.Settings, baseColor, mouseoverColor))
		//	{
		//		if (editingPawnOverlayRenderer == null || editingPawnOverlayRenderer != handler)
		//		{
		//			SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
		//			editingPawnOverlayRenderer = handler;
		//		}
		//		else
		//		{
		//			SoundDefOf.TabClose.PlayOneShotOnCamera(null);
		//			editingPawnOverlayRenderer = null;
		//		}
		//	}
		//}

		foreach (Pawn pawn in pawns)
		{
			if (DoRow(curY, viewRect, scrollPos, pawn, ref moreDetailsForPawn, draggedPawn == null))
			{
				hoveringOverPawn = pawn;
			}
			curY += PawnRowHeight;
		}
		return overHandler;
	}

	public static bool DoRow(float curY, Rect viewRect, Vector2 scrollPos, Pawn pawn,
		ref Pawn moreDetailsForPawn, bool highlight)
	{
		float minY = scrollPos.y - PawnRowHeight;
		float maxY = scrollPos.y + ITab_Vehicle_Passengers.WindowHeight;

		bool isDraggingPawn = pawn == draggedPawn;

		if (!isDraggingPawn && (curY <= minY || curY >= maxY))
		{
			return false;
		}

		float nonRefY = isDraggingPawn ? (Event.current.mousePosition.y - PawnRowHeight / 2) : curY;
		float nonRefX = isDraggingPawn ?
			(Event.current.mousePosition.x - (LabelWidth + ThingIconSize) / 2) :
			0;
		Rect pawnRect = new(nonRefX, nonRefY, viewRect.width, PawnRowHeight);

		Widgets.BeginGroup(pawnRect);
		Rect fullRect = pawnRect.AtZero();

		Rect dragRect = new(0, 0, LabelWidth + ThingIconSize + PawnRowPadding, PawnRowHeight);
		bool mouseOver = Mouse.IsOver(dragRect);
		if (draggedPawn == null && mouseOver)
		{
			desiredMouseState = Ext_Enum.Max(desiredMouseState, MouseState.HoverOver);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				draggedPawn = pawn;
				Event.current.Use();
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
		}

		if (draggedPawn != null)
		{
			desiredMouseState = Ext_Enum.Max(desiredMouseState, MouseState.Dragging);
		}

		Widgets.InfoCardButton(fullRect.width - PawnExtraButtonSize,
			(pawnRect.height - PawnExtraButtonSize) / 2f, pawn);
		fullRect.width -= PawnExtraButtonSize;
		if (!pawn.Dead)
		{
			OpenSpecificTabButton(fullRect, pawn, ref moreDetailsForPawn);
			fullRect.width -= PawnExtraButtonSize;
		}

		if (highlight)
		{
			Widgets.DrawHighlightIfMouseover(dragRect);
		}
		Rect iconRect = new(PawnRowPadding, (pawnRect.height - ThingIconSize) / 2f,
			ThingIconSize, ThingIconSize);
		Widgets.ThingIcon(iconRect, pawn);
		Rect bgRect = new(iconRect.xMax + PawnRowPadding, 16f, LabelWidth, 18f);
		GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, LabelWidth, null, GameFont.Small, false, false);

		using ClearOnDispose<Need> cod = new(TmpNeeds);
		List<Need> allNeeds = pawn.needs.AllNeeds;
		foreach (Need need in allNeeds)
		{
			// Change for all needs?
			if (need.def.showForCaravanMembers)
				TmpNeeds.Add(need);
		}
		PawnNeedsUIUtility.SortInDisplayOrder(TmpNeeds);

		float xMax = bgRect.xMax;
		foreach (Need need in TmpNeeds)
		{
			Rect needRect = new(xMax, 0f, LabelWidth, PawnRowHeight);
			need.DrawOnGUI(needRect, customMargin: 10, drawArrows: false, doTooltip: true);
			xMax = needRect.xMax;
		}

		if (pawn.Downed)
		{
			using TextBlock guiColor = new(new Color(1f, 0f, 0f, 0.5f));
			Widgets.DrawLineHorizontal(0f, pawnRect.height / 2f, pawnRect.width);
		}
		Widgets.EndGroup();

		return mouseOver && !isDraggingPawn;
	}

	private static void OpenSpecificTabButton(Rect rowRect, Pawn pawn, ref Pawn moreDetailsForPawn)
	{
		Color baseColor = (pawn != moreDetailsForPawn) ? Color.white : Color.green;
		Color mouseoverColor =
			(pawn != moreDetailsForPawn) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
		Rect rect = new(rowRect.width - PawnExtraButtonSize,
			(rowRect.height - PawnExtraButtonSize) / 2f,
			PawnExtraButtonSize, PawnExtraButtonSize);

		if (Widgets.ButtonImage(rect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor,
			mouseoverColor))
		{
			if (pawn == moreDetailsForPawn)
			{
				moreDetailsForPawn = null;
				SoundDefOf.TabClose.PlayOneShotOnCamera();
			}
			else
			{
				moreDetailsForPawn = pawn;
				SoundDefOf.TabOpen.PlayOneShotOnCamera();
			}
		}
		TooltipHandler.TipRegion(rect, "OpenSpecificTabButtonTip".Translate());
	}

	public static void HandleDragEvent()
	{
		if (draggedPawn == null)
			return;
		if (Event.current.type != EventType.MouseUp || Event.current.button != 0)
			return;

		if (transferToHolder == null)
		{
			Clear();
			SoundDefOf.ClickReject.PlayOneShotOnCamera();
			return;
		}
		try
		{
			if (transferToHolder is VehicleRoleHandler { AreSlotsAvailable: false } transferToHandler)
			{
				if (hoveringOverPawn != null &&
					draggedPawn.ParentHolder is VehicleRoleHandler curHandler &&
					curHandler != transferToHolder && transferToHandler.CanOperateRole(draggedPawn) &&
					curHandler.CanOperateRole(hoveringOverPawn))
				{
					curHandler.thingOwner.Swap(transferToHandler.thingOwner, draggedPawn,
						hoveringOverPawn);
					SoundDefOf.Click.PlayOneShotOnCamera();
					transferToHandler.vehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
				}
				else
				{
					Messages.Message("VF_HandlerNotEnoughRoom".Translate(draggedPawn, transferToHandler.role.label),
						MessageTypeDefOf.RejectInput);
				}
			}
			else if (draggedPawn.ParentHolder != transferToHolder)
			{
				switch (transferToHolder)
				{
					case VehicleRoleHandler targetHandler:
						if (!targetHandler.CanOperateRole(draggedPawn))
						{
							bool canAssign = (targetHandler.role.HandlingTypes & HandlingType.Movement) == 0;
							MessageTypeDef msgTypeDef = canAssign ?
								MessageTypeDefOf.CautionInput :
								MessageTypeDefOf.RejectInput;
							Messages.Message("VF_IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), msgTypeDef);

							if (!canAssign)
								return;
						}
					break;
					case Pawn_InventoryTracker:
						if (!draggedPawn.CanBeTransferredToVehiclesCargo())
						{
							Messages.Message("VF_CannotAddToCargo".Translate(draggedPawn.LabelShortCap),
								MessageTypeDefOf.RejectInput);
							return;
						}
					break;
				}

				IThingHolder previousHolder = draggedPawn.ParentHolder;
				if (!transferToHolder.GetDirectlyHeldThings()
				 .TryAddOrTransfer(draggedPawn, canMergeWithExistingStacks: false))
				{
					Log.Warning($"Unable to add {draggedPawn} to {transferToHolder}.");
					return;
				}
				SoundDefOf.Click.PlayOneShotOnCamera();
				OnPawnChangedSeats(previousHolder);
				draggedPawn.GetVehicleCaravan()?.RecacheVehicles();
			}
		}
		finally
		{
			draggedPawn = null;
		}
	}

	private static void OnPawnChangedSeats(IThingHolder previousHolder)
	{
		VehiclePawn fromVehicle = GetVehicle(previousHolder);
		VehiclePawn toVehicle = GetVehicle(transferToHolder);

		pawnSeatChanged = true;
		if (fromVehicle != null && toVehicle == null)
		{
			if (!draggedPawn.Spawned && !draggedPawn.IsWorldPawn())
			{
				Find.WorldPawns.PassToWorld(draggedPawn);
			}
			fromVehicle.EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
		}
		if (toVehicle != null)
		{
			if (fromVehicle == toVehicle)
			{
				toVehicle.EventRegistry[VehicleEventDefOf.PawnChangedSeats].ExecuteEvents();
			}
			else
			{
				if (!draggedPawn.Spawned && draggedPawn.IsWorldPawn())
				{
					Find.WorldPawns.RemovePawn(draggedPawn);
				}
				fromVehicle?.EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
				toVehicle.EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
			}
		}

		return;

		static VehiclePawn GetVehicle(IThingHolder holder)
		{
			return holder switch
			{
				VehicleRoleHandler handler      => handler.vehicle,
				Pawn_InventoryTracker inventory => inventory.pawn as VehiclePawn,
				_                               => null
			};
		}
	}

	public static Vector2 GetSize(IEnumerable<Pawn> pawns, float paneTopY, bool doNeeds = true)
	{
		float width = LabelWidth;
		if (doNeeds)
		{
			width += MaxNeedsCount(pawns) * LabelWidth;
		}
		width += PawnExtraButtonSize;
		Vector2 result;
		result.x = LabelWidth + width + 16f + 3; //Scrollbar=16 Padding=3
		result.y = Mathf.Min(ITab_Vehicle_Passengers.WindowHeight, paneTopY - 30f);
		return result;
	}

	private static int MaxNeedsCount(IEnumerable<Pawn> pawns)
	{
		int maxNeeds = 0;
		List<Need> pawnNeeds = new List<Need>();
		foreach (Pawn pawn in pawns)
		{
			if (pawn.needs != null)
			{
				foreach (Need need in pawn.needs.AllNeeds)
				{
					if (need.def.showForCaravanMembers)
					{
						pawnNeeds.Add(need);
					}
				}
				maxNeeds = Mathf.Max(maxNeeds, pawnNeeds.Count);
				pawnNeeds.Clear();
			}
		}
		return maxNeeds;
	}

	private enum MouseState
	{
		None,
		HoverOver,
		Dragging
	}

	public readonly struct DrawBlock : IDisposable
	{
		public DrawBlock()
		{
			Start();
		}

		void IDisposable.Dispose()
		{
			End();
		}
	}
}