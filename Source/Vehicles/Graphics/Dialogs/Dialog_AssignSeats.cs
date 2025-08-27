using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace Vehicles.World;

public class Dialog_AssignSeats : Window
{
	private const float ButtonWidth = 120f;
	private const float ButtonHeight = 30f;
	private const float EntryButtonWidth = ButtonHeight * 3;
	private const float RowHeight = 30f;
	private const float RowPadding = 5f;

	private readonly ICaravanInfo parent;
	private readonly VehiclePawn vehicle;

	private Pawn draggedPawn;
	private Vector2 draggedItemPosOffset;
	private static Vector2 dialogPawnsScrollPos;
	private static Vector2 dialogPawnsAssignedScrollPos;

	private readonly List<Pawn> removalList = [];

	private readonly VehicleAssignment assigner = new();

	private readonly List<TransferableOneWay> transferablePawns = [];
	private readonly List<Pawn> pawns;
	private readonly HashSet<Pawn> insideVehicle;

	private readonly TransferableOneWay vehicleTransferable;

	private readonly RoleAssignment prioritizer = new();

	public Dialog_AssignSeats(ICaravanInfo parent, List<TransferableOneWay> pawns,
		TransferableOneWay vehicleTransferable)
	{
		this.parent = parent;
		vehicle = vehicleTransferable.AnyThing as VehiclePawn;
		Assert.IsNotNull(vehicle);
		insideVehicle = vehicle.AllPawnsAboard.ToHashSet();

		this.vehicleTransferable = vehicleTransferable;
		GetTransferablePawns(pawns, vehicle, transferablePawns);
		this.pawns = transferablePawns.Select(pawn => pawn.AnyThing as Pawn).OrderBy(p => p, new TransferablePawnComparer())
		 .ToList();

		absorbInputAroundWindow = true;
		closeOnCancel = true;

		dialogPawnsScrollPos = Vector2.zero;
		dialogPawnsAssignedScrollPos = Vector2.zero;

		foreach (VehicleRoleHandler handler in vehicle.handlers)
		{
			foreach (Pawn pawn in handler.thingOwner)
			{
				assigner.SetAssignment(new AssignedSeat(pawn, handler));
			}
		}
		List<AssignedSeat> curAssignments = CaravanHelper.assignedSeats.GetAssignments(vehicle);
		if (!curAssignments.NullOrEmpty())
		{
			foreach (AssignedSeat seat in curAssignments)
			{
				if (seat.Vehicle == vehicle)
					assigner.SetAssignment(seat);
			}
		}
	}

	private List<AssignedSeat> Assignments => assigner.GetAssignments(vehicle);

	public override Vector2 InitialSize => new(900, UI.screenHeight / 1.85f);

	private int PreAssignedCount(VehicleRoleHandler handler)
	{
		int count = 0;
		foreach (AssignedSeat seat in Assignments)
		{
			if (seat.handler == handler)
				count++;
		}
		return count;
	}

	public override void DoWindowContents(Rect rect)
	{
		DrawVehicleMenu(rect);
		DoBottomButtons(rect);
	}

	private void DrawVehicleMenu(Rect rect)
	{
		Rect vehicleRect = new(rect)
		{
			y = rect.y + 25,
			height = rect.height - 25 - ButtonHeight * 1.1f
		};

		Widgets.DrawMenuSection(vehicleRect);

		float windowSectionWidth = rect.width / 2 - 5;
		Rect pawnsRect = new(rect.x, rect.y, windowSectionWidth, vehicleRect.height - 15);
		Rect assignedRect = new(pawnsRect)
		{
			x = pawnsRect.x + pawnsRect.width + 10
		};

		if (draggedPawn != null)
		{
			float mousePosX = Event.current.mousePosition.x - draggedItemPosOffset.x;
			float mousePosY = Event.current.mousePosition.y - draggedItemPosOffset.y;
			float width = pawnsRect.width - 1;
			Rect draggedRect = new(mousePosX, mousePosY, width, RowHeight);
			// Unclickable floating row
			_ = DrawPawnRow(draggedRect, draggedPawn, null);
		}
		DrawPawns(pawnsRect);
		UIElements.DrawLineVerticalGrey(pawnsRect.x + pawnsRect.width + 5, vehicleRect.y,
			vehicleRect.height);
		DrawAssignees(assignedRect);
		if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
		{
			draggedPawn = null;
		}
	}

	[MustUseReturnValue]
	private static bool DrawPawnRow(Rect rect, Pawn pawn, string label)
	{
		Rect colonistIconRect = new(rect.x, rect.y, RowHeight, RowHeight);
		Rect colonistRect = new(colonistIconRect.x + RowHeight, rect.y + 5, rect.width - 1,
			RowHeight);
		Widgets.Label(colonistRect, pawn.LabelCap);
		Widgets.ThingIcon(colonistIconRect, pawn);

		Rect buttonRect = new(rect.x + colonistRect.width - 100, rect.y + 5, EntryButtonWidth,
			ButtonHeight - 10);

		if (label.NullOrEmpty())
			return false;
		bool clicked = Widgets.ButtonText(buttonRect, label);
		if (clicked)
			SoundDefOf.Click.PlayOneShotOnCamera();
		return clicked;
	}

	private void DrawPawns(Rect rect)
	{
		Widgets.Label(rect, "VF_Colonists".Translate());
		Rect pawnRowRect = new(rect.x, rect.y + RowHeight + 5, rect.width - 1, RowHeight);
		Rect outRect = new(pawnRowRect)
		{
			x = rect.x,
			height = rect.height
		};
		Rect viewRect = new(outRect)
		{
			width = outRect.width - 17,
			height = (RowHeight + RowPadding) * pawns.Count
		};

		Widgets.BeginScrollView(outRect, ref dialogPawnsScrollPos, viewRect);
		foreach (Pawn pawn in pawns)
		{
			if (assigner.IsAssigned(pawn))
				continue;

			Rect entryButtonRect = new(pawnRowRect.x + pawnRowRect.width - (EntryButtonWidth + 10),
				pawnRowRect.y + 5, EntryButtonWidth, ButtonHeight - 10);
			if (Mouse.IsOver(pawnRowRect) && !Mouse.IsOver(entryButtonRect))
			{
				Widgets.DrawHighlight(pawnRowRect);
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					draggedPawn = pawn;
					draggedItemPosOffset = Event.current.mousePosition -
						(pawnRowRect.position + new Vector2(ButtonHeight, 0));
					Event.current.Use();
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}

			if (draggedPawn != pawn && DrawPawnRow(pawnRowRect, pawn, "VF_AddToRole".Translate()))
			{
				VehicleRoleHandler firstHandler = vehicle.handlers.FirstOrDefault(handler =>
					handler.CanOperateRole(pawn) && MatchingHandler(handler));
				firstHandler ??= vehicle.handlers.FirstOrDefault(handler =>
					!handler.RequiredForMovement && MatchingHandler(handler));
				if (firstHandler != null)
				{
					bool canAssign = true;
					if (!firstHandler.CanOperateRole(pawn))
					{
						canAssign = !firstHandler.role.HandlingTypes.HasFlag(HandlingType.Movement);
						MessageTypeDef msgTypeDef = canAssign ?
							MessageTypeDefOf.CautionInput :
							MessageTypeDefOf.RejectInput;
						Messages.Message("VF_IncapableStatusForRole".Translate(pawn.LabelShortCap),
							msgTypeDef);
					}
					if (canAssign)
						assigner.SetAssignment(new AssignedSeat(pawn, firstHandler));
				}
			}
			pawnRowRect.y += RowHeight + RowPadding;
		}
		Widgets.EndScrollView();
		return;

		bool MatchingHandler(VehicleRoleHandler handler)
		{
			int count = 0;
			foreach (AssignedSeat assignedSeat in Assignments)
			{
				if (assignedSeat.handler.role == handler.role)
					count++;
			}
			return count < handler.role.Slots;
		}
	}

	private void DrawAssignees(Rect rect)
	{
		Widgets.Label(rect, "VF_Assigned".Translate());
		Rect pawnRowRect = new(rect.x, rect.y + RowHeight + 5, rect.width - 1, RowHeight);
		Rect outRect = new(pawnRowRect)
		{
			x = rect.x,
			height = rect.height
		};
		Rect viewRect = new(outRect)
		{
			width = outRect.width - 17,
			height = (vehicle.handlers.Count + Assignments.Count) * RowHeight +
				vehicle.handlers.Count(handler => handler.AreSlotsAvailableAndReservable) * RowHeight + 5
		};
		Widgets.BeginScrollView(outRect, ref dialogPawnsAssignedScrollPos, viewRect);
		foreach (VehicleRoleHandler handler in vehicle.handlers)
		{
			int seatsOccupied = PreAssignedCount(handler);
			Color countColor = handler.role.RequiredForCaravan ?
				seatsOccupied < handler.role.SlotsToOperate ? Color.red :
				seatsOccupied == handler.role.Slots ? Color.grey : Color.white :
				seatsOccupied == handler.role.Slots ?
					Color.grey :
					Color.white;

			UIElements.LabelUnderlined(pawnRowRect, handler.role.label,
				$"({handler.role.Slots - seatsOccupied})",
				Color.white, countColor, Color.white);
			pawnRowRect.y += RowHeight;

			Rect roleRect = new(pawnRowRect.x, pawnRowRect.y, pawnRowRect.width,
				RowHeight + RowHeight * seatsOccupied);

			int slotsAvailable = handler.role.Slots - seatsOccupied;
			if (slotsAvailable > 0 && Mouse.IsOver(roleRect) && draggedPawn != null)
			{
				if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
				{
					bool canAssign = true;
					if (!handler.CanOperateRole(draggedPawn))
					{
						canAssign = !handler.role.HandlingTypes.HasFlag(HandlingType.Movement);
						MessageTypeDef msgTypeDef = canAssign ?
							MessageTypeDefOf.CautionInput :
							MessageTypeDefOf.RejectInput;
						Messages.Message("VF_IncapableStatusForRole".Translate(draggedPawn.LabelShortCap),
							msgTypeDef);
					}
					if (canAssign)
						assigner.SetAssignment(new AssignedSeat(draggedPawn, handler));
				}
			}

			foreach (AssignedSeat assignedSeat in Assignments)
			{
				if (assignedSeat.handler.role != handler.role)
					continue;

				if (DrawPawnRow(pawnRowRect, assignedSeat.pawn,
					insideVehicle.Contains(assignedSeat.pawn) ? null : "VF_RemoveFromRole".Translate()))
				{
					removalList.Add(assignedSeat.pawn);
				}
				pawnRowRect.y += RowHeight;
			}
			foreach (Pawn pawn in removalList)
				assigner.RemoveAssignment(pawn);
			removalList.Clear();

			if (slotsAvailable > 0)
			{
				if (draggedPawn != null)
				{
					if (Mouse.IsOver(pawnRowRect))
						Widgets.DrawHighlight(pawnRowRect);
					else
						Widgets.DrawHighlight(pawnRowRect, 0.5f);
				}
				pawnRowRect.y += RowHeight;
			}
		}
		Widgets.EndScrollView();
	}

	private void DoBottomButtons(Rect rect)
	{
		Rect leftButtonRect = new(rect.x, rect.yMax - ButtonHeight, ButtonWidth, ButtonHeight);
		Rect rightButtonRect = new(rect.xMax - ButtonWidth, rect.yMax - ButtonHeight, ButtonWidth,
			ButtonHeight);

		if (Widgets.ButtonText(leftButtonRect, "CancelButton".Translate()))
		{
			Close();
		}

		Rect buttonRect = new(rect.center.x, rect.height - ButtonHeight,
			ButtonWidth, ButtonHeight);
		if (Widgets.ButtonText(buttonRect, "VF_AutoAssign".Translate()))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			AutoAssign();
		}
		buttonRect.x -= ButtonWidth;
		if (Widgets.ButtonText(buttonRect, "VF_ClearSeats".Translate()))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			assigner.RemoveAll(pawn => !vehicle.AllPawnsAboard.Contains(pawn));
		}

		if (Widgets.ButtonText(rightButtonRect, "Confirm".Translate()))
		{
			if (!FinalizeSeats(out string failReason))
			{
				Messages.Message("VF_AssignFailure".Translate(failReason), MessageTypeDefOf.RejectInput);
				return;
			}
			Close();
		}
	}

	private void AutoAssign()
	{
		prioritizer.Set(pawns.Where(pawn =>
			!assigner.IsAssigned(pawn) && !vehicle.AllPawnsAboard.Contains(pawn)));

		// Fill until operational, then fill by priority
		foreach (VehicleRoleHandler handler in vehicle.handlers.OrderBy(handler => handler))
		{
			Assert.IsNotNull(handler);
			int slotsToOperate = handler.role.SlotsToOperate - PreAssignedCount(handler);
			for (int i = 0; i < slotsToOperate; i++)
			{
				if (!prioritizer.TryPull(handler, out Pawn bestPawn))
					return;

				if (bestPawn != null && handler.AreSlotsAvailableAndReservable)
					assigner.SetAssignment(new AssignedSeat(bestPawn, handler));
			}
		}
		foreach (VehicleRoleHandler handler in vehicle.handlers.OrderBy(handler => handler))
		{
			Assert.IsNotNull(handler);
			int slotsAvailable = handler.role.Slots - PreAssignedCount(handler);
			for (int i = 0; i < slotsAvailable; i++)
			{
				if (!prioritizer.TryPull(handler, out Pawn bestPawn))
					return;

				if (bestPawn != null && handler.AreSlotsAvailableAndReservable)
					assigner.SetAssignment(new AssignedSeat(bestPawn, handler));
			}
		}
	}

	private bool FinalizeSeats(out string failReason)
	{
		failReason = string.Empty;

		foreach (VehicleRoleHandler handler in vehicle.handlers)
		{
			if (!handler.role.RequiredForCaravan)
				continue;

			if (PreAssignedCount(handler) < handler.role.SlotsToOperate)
			{
				failReason = "VF_CantAssignVehicle".Translate(vehicle.LabelCap);
				return false;
			}
		}
		try
		{
			foreach (AssignedSeat seat in CaravanHelper.assignedSeats.GetAssignments(vehicle))
			{
				transferablePawns.FirstOrDefault(transferable => transferable.AnyThing == seat.pawn)
				?.ForceTo(0);
			}

			// Update all current pawns being assigned to this vehicle in Pawns tab
			foreach (Pawn pawn in assigner.AllAssignments.Keys)
			{
				transferablePawns.FirstOrDefault(transferable => transferable.AnyThing == pawn)?.ForceTo(1);
			}

			// Add all pawns to assigned seating registry and refresh caravan dialog
			CaravanHelper.assignedSeats.SetAssignments(vehicle, Assignments);
			int transferCount = Assignments.Count > 0 ? vehicleTransferable.GetMaximumToTransfer() : 0;
			vehicleTransferable.AdjustTo(transferCount);
			Dialog_FormVehicleCaravan.MarkDirty();
			parent.NotifyTransferablesChanged();
		}
		catch (Exception ex)
		{
			Log.Error($"Failed to finalize assigning vehicle seats.\n{ex}");
			failReason = ex.Message;
			return false;
		}
		return true;
	}

	private static void GetTransferablePawns(List<TransferableOneWay> pawns, VehiclePawn vehicle,
		List<TransferableOneWay> outList)
	{
		outList.Clear();
		foreach (TransferableOneWay transferable in pawns)
		{
			if (transferable.AnyThing is not Pawn pawn)
				continue;

			if (!CaravanHelper.assignedSeats.IsAssigned(pawn) ||
				CaravanHelper.assignedSeats.GetAssignment(pawn).Vehicle == vehicle)
			{
				outList.Add(transferable);
				continue;
			}

			foreach (VehicleRoleHandler handler in vehicle.handlers)
			{
				if (handler.thingOwner.Contains(pawn))
				{
					outList.Add(transferable);
					break;
				}
			}
		}
	}

	private class TransferablePawnComparer : Comparer<Pawn>
	{
		public override int Compare(Pawn lhs, Pawn rhs)
		{
			if (lhs is null && rhs is not null)
				return 1;
			if (lhs is not null && rhs is null)
				return -1;
			if (ReferenceEquals(lhs, rhs))
				return 0;

			// Sort by category, then by name if same pawn types.
			int compInt = PawnPriority(lhs).CompareTo(PawnPriority(rhs));
			if (compInt != 0)
				return compInt;

			if (lhs.Name is null)
				return 1;
			if (rhs.Name is null)
				return -1;

			return string.Compare(lhs.Name.ToStringFull, rhs.Name.ToStringFull, StringComparison.OrdinalIgnoreCase);

			static int PawnPriority(Pawn pawn)
			{
				SemanticOrdering priority = pawn switch
				{
					{ IsColonist: true }      => SemanticOrdering.Colonist,
					{ IsSlaveOfColony: true } => SemanticOrdering.SlaveOfColony,
					{ IsColonyMech: true }    => SemanticOrdering.ColonyMech,
					{ IsAnimal: true }        => SemanticOrdering.Animal,
					_                         => SemanticOrdering.Undefined
				};
				return (int)priority;
			}
		}

		private enum SemanticOrdering
		{
			Colonist,
			SlaveOfColony,
			ColonyMech,
			Animal,
			Undefined
		}
	}
}