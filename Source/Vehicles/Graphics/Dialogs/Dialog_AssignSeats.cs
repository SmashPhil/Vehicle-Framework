using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class Dialog_AssignSeats : Window
	{
		private const float ButtonWidth = 120f;
		private const float ButtonHeight = 30f;
		private const float RowHeight = 30f;

		private Pawn draggedPawn;
		private Vector2 draggedItemPosOffset;
		private Vector2 draggedIconPosOffset;
		private static Vector2 dialogPawnsScrollPos;
		private static Vector2 dialogPawnsAssignedScrollPos;

		private readonly Dictionary<Pawn, (VehiclePawn vehicle, VehicleHandler handler)> assignedSeats = new Dictionary<Pawn, (VehiclePawn, VehicleHandler)>();

		private readonly List<TransferableOneWay> transferablePawns;
		private readonly List<Pawn> pawns;

		private readonly TransferableOneWay transferable;

		public Dialog_AssignSeats(List<TransferableOneWay> pawns, TransferableOneWay transferable)
		{
			this.transferable = transferable;
			this.transferablePawns = pawns.Where(p => p.AnyThing is Pawn pawn && (Vehicle.handlers.NotNullAndAny(handler => handler.handlers.Contains(pawn)) ||
				!CaravanHelper.assignedSeats.ContainsKey(pawn) || (CaravanHelper.assignedSeats.ContainsKey(pawn) && CaravanHelper.assignedSeats[pawn].vehicle == Vehicle))).ToList();
			this.pawns = transferablePawns.Select(pawn => pawn.AnyThing as Pawn).ToList();

			absorbInputAroundWindow = true;
			closeOnCancel = true;

			dialogPawnsScrollPos = Vector2.zero;
			dialogPawnsAssignedScrollPos = Vector2.zero;

			foreach (VehicleHandler handler in Vehicle.handlers)
			{
				foreach (Pawn pawn in handler.handlers)
				{
					//Log.Message($"Adding {pawn} to assigned seating");
					assignedSeats[pawn] = (Vehicle, handler);
				}
			}
			foreach ((Pawn pawn, AssignedSeat seat) in CaravanHelper.assignedSeats)
			{
				if (!assignedSeats.ContainsKey(pawn))
				{
					//Log.Message($"Adding {pawn} to assigned seating from Caravanhelper");
					assignedSeats[pawn] = seat;
				}
			}
		}

		private VehiclePawn Vehicle => transferable.AnyThing as VehiclePawn;

		public override Vector2 InitialSize => new Vector2(900, (float)UI.screenHeight / 1.85f);

		public override void DoWindowContents(Rect rect)
		{
			DrawVehicleMenu(rect);
			DoBottomButtons(rect);
		}

		private void DrawVehicleMenu(Rect rect)
		{
			Rect vehicleRect = new Rect(rect)
			{
				y = rect.y + 25,
				height = rect.height - 25 - ButtonHeight * 1.1f
			};

			Widgets.DrawMenuSection(vehicleRect);

			float windowSectionWidth = rect.width / 2 - 5;
			Rect pawnsRect = new Rect(rect.x, rect.y, windowSectionWidth, vehicleRect.height - 15);
			Rect assignedRect = new Rect(pawnsRect)
			{
				x = pawnsRect.x + pawnsRect.width + 10
			};

			if (draggedPawn != null)
			{
				float mousePosX = Event.current.mousePosition.x - draggedItemPosOffset.x;
				float mousePosY = Event.current.mousePosition.y - draggedItemPosOffset.y;
				float width = pawnsRect.width - 1;
				Rect draggedRect = new Rect(mousePosX, mousePosY, width, RowHeight);
				DrawPawnRow(draggedRect, draggedPawn, (0, "[ERR]", null));
			}
			DrawPawns(pawnsRect);
			UIElements.DrawLineVerticalGrey(pawnsRect.x + pawnsRect.width + 5, vehicleRect.y, vehicleRect.height);
			DrawAssignees(assignedRect);
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				draggedPawn = null;
			}
		}

		private void DrawPawnRow(Rect rect, Pawn pawn, (float width, string label, Action onClick) button)
		{
			Rect colonistIconRect = new Rect(rect.x, rect.y, RowHeight, RowHeight);
			Rect colonistRect = new Rect(colonistIconRect.x + RowHeight, rect.y + 5, rect.width - 1, RowHeight);
			Widgets.Label(colonistRect, pawn.LabelCap);
			Widgets.ThingIcon(colonistIconRect, pawn);

			Rect buttonRect = new Rect(rect.x + colonistRect.width - 100, rect.y + 5, button.width, ButtonHeight - 10);
			if (button.onClick != null && Widgets.ButtonText(buttonRect, button.label))
			{
				button.onClick();
			}
		}

		private void DrawPawns(Rect rect)
		{
			Widgets.Label(rect, "VF_Colonists".Translate());
			Rect pawnRowRect = new Rect(rect.x, rect.y + RowHeight + 5, rect.width - 1, RowHeight);
			Rect outRect = new Rect(pawnRowRect)
			{
				x = rect.x,
				height = rect.height
			};
			Rect viewRect = new Rect(outRect)
			{
				width = outRect.width - 17,
				height = RowHeight * pawns.Count
			};
			Widgets.BeginScrollView(outRect, ref dialogPawnsScrollPos, viewRect);
			{
				foreach (Pawn pawn in pawns.Where(a => !assignedSeats.ContainsKey(a)))
				{
					float buttonWidth = ButtonHeight * 3;
					Rect entryButtonRect = new Rect(pawnRowRect.x + pawnRowRect.width - (buttonWidth + 10), pawnRowRect.y + 5, buttonWidth, ButtonHeight - 10);
					if (Mouse.IsOver(pawnRowRect) && !Mouse.IsOver(entryButtonRect))
					{
						Widgets.DrawHighlight(pawnRowRect);
						if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
						{
							draggedPawn = pawn;
							draggedItemPosOffset = Event.current.mousePosition - (pawnRowRect.position +  new Vector2(ButtonHeight, 0));
							draggedIconPosOffset = Event.current.mousePosition - pawnRowRect.position;
							Event.current.Use();
							SoundDefOf.Click.PlayOneShotOnCamera(null);
						}
					}

					if (draggedPawn != pawn)
					{
						DrawPawnRow(pawnRowRect, pawn, (ButtonHeight * 3, "VF_AddToRole".Translate(), delegate()
						{
							bool Validate(VehicleHandler handler) => assignedSeats.Where(seat => seat.Value.handler.role == handler.role).Select(p => p.Key).Count() < handler.role.slots;
							VehicleHandler firstHandler = Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(pawn) && Validate(handler));
							firstHandler ??= Vehicle.handlers.FirstOrDefault(handler => !handler.RequiredForMovement && Validate(handler));
							if (firstHandler != null)
							{
								if (!firstHandler.CanOperateRole(pawn))
								{
									if (firstHandler.role.handlingTypes.HasFlag(HandlingTypeFlags.Movement))
									{
										Messages.Message("VF_IncapableStatusForRole".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput);
									}
									else
									{
										Messages.Message("VF_IncapableStatusForRole".Translate(pawn.LabelShortCap), MessageTypeDefOf.CautionInput);
										assignedSeats.Add(pawn, (Vehicle, firstHandler));
									}
								}
								else
								{
									assignedSeats.Add(pawn, (Vehicle, firstHandler));
								}
							}
						}
						));
					}
					pawnRowRect.y += RowHeight + 5;
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawAssignees(Rect rect)
		{
			Widgets.Label(rect, "VF_Assigned".Translate());
			Predicate<VehicleHandler> seatsAvailable = (handler) => assignedSeats.Where(seat => seat.Value.handler.role == handler.role).Select(p => p.Key).Count() < handler.role.slots;
			Rect pawnRowRect = new Rect(rect.x, rect.y + RowHeight + 5, rect.width - 1, RowHeight);
			Rect outRect = new Rect(pawnRowRect)
			{
				x = rect.x,
				height = rect.height
			};
			Rect viewRect = new Rect(outRect)
			{
				width = outRect.width - 17,
				height = (Vehicle.handlers.Count + assignedSeats.Count) * RowHeight + Vehicle.handlers.Where(handler => seatsAvailable(handler)).Count() * RowHeight + 5
			};
			Widgets.BeginScrollView(outRect, ref dialogPawnsAssignedScrollPos, viewRect);
			{
				foreach (VehicleHandler handler in Vehicle.handlers)
				{
					int seatsOccupied = assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count();
					Color countColor = handler.role.RequiredForCaravan ? seatsOccupied < handler.role.slotsToOperate ? Color.red : seatsOccupied == handler.role.slots ? Color.grey : Color.white : seatsOccupied == handler.role.slots ? Color.grey : Color.white;
					
					UIElements.LabelUnderlined(pawnRowRect, handler.role.label, $"({handler.role.slots - assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count()})", Color.white, countColor, Color.white);
					pawnRowRect.y += RowHeight;

					Rect roleRect = new Rect(pawnRowRect.x, pawnRowRect.y, pawnRowRect.width, RowHeight + RowHeight * assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count());

					bool open = seatsAvailable(handler);
					if (open && Mouse.IsOver(roleRect) && draggedPawn != null)
					{
						if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
						{
							if (handler.role.handlingTypes > HandlingTypeFlags.None && !draggedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || draggedPawn.Downed || draggedPawn.Dead)
							{
								if (handler.role.handlingTypes.HasFlag(HandlingTypeFlags.Movement))
								{
									Messages.Message("VF_IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), MessageTypeDefOf.RejectInput);
								}
								else
								{
									Messages.Message("VF_IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), MessageTypeDefOf.CautionInput);
									assignedSeats.Add(draggedPawn, (Vehicle, handler));
								}
							}
							else
							{
								assignedSeats.Add(draggedPawn, (Vehicle, handler));
							}
						}
					}
					List<Pawn> removalList = new List<Pawn>();
					foreach ((Pawn pawn, (VehiclePawn vehicle, VehicleHandler assignedHandler)) in assignedSeats.Where(r => r.Value.handler.role == handler.role))
					{
						Action onClick = null;
						if (!vehicle.AllPawnsAboard.Contains(pawn))
						{
							onClick = () => removalList.Add(pawn);
						}
						DrawPawnRow(pawnRowRect, pawn, (ButtonHeight * 3, "VF_RemoveFromRole".Translate(), onClick));
						pawnRowRect.y += RowHeight;
					}
					foreach (Pawn pawn in removalList)
					{
						assignedSeats.Remove(pawn);
					}
					if (open)
					{
						if (draggedPawn != null && Mouse.IsOver(pawnRowRect))
						{
							Widgets.DrawHighlight(pawnRowRect);
						}
						pawnRowRect.y += RowHeight;
					}
				}
			}
			Widgets.EndScrollView();
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect buttonRect = new Rect(rect.width - ButtonWidth * 3, rect.height - ButtonHeight, ButtonWidth, ButtonHeight);
			if (Widgets.ButtonText(buttonRect, "VF_Assign".Translate()))
			{
				if (!FinalizeSeats(out string failReason))
				{
					Messages.Message("VF_AssignFailure".Translate(failReason), MessageTypeDefOf.RejectInput);
					return;
				}
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "CancelButton".Translate()))
			{
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "VF_ClearSeats".Translate()))
			{
				SoundDefOf.Click.PlayOneShotOnCamera(null);
				assignedSeats.RemoveAll(kvp => pawns.Contains(kvp.Key) && !Vehicle.AllPawnsAboard.Contains(kvp.Key));
			}
		}

		private bool FinalizeSeats(out string failReason)
		{
			failReason = string.Empty;
			foreach (VehicleHandler handler in Vehicle.handlers.Where(x => x.role.RequiredForCaravan))
			{
				if (assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(k => k.Key).Count() < handler.role.slotsToOperate)
				{
					failReason = "VF_CantAssignVehicle".Translate(Vehicle.LabelCap);
					return false;
				}
			}
			try
			{
				CaravanHelper.ClearAssignedSeats(Vehicle, (Pawn pawn) => transferablePawns.FirstOrDefault(p => (p.AnyThing as Pawn) == pawn)?.ForceTo(0));

				//Update all current pawns being assigned to this vehicle in Pawns tab
				foreach (Pawn assignedPawn in assignedSeats.Keys)
				{
					transferablePawns.FirstOrDefault(p => (p.AnyThing as Pawn) == assignedPawn)?.ForceTo(1);
				}

				//Add all pawns to assigned seating registry and refresh caravan dialog
				foreach ((Pawn pawn, (VehiclePawn vehicle, VehicleHandler handler) assignment) in assignedSeats)
				{
					if (CaravanHelper.assignedSeats.ContainsKey(pawn))
					{
						CaravanHelper.assignedSeats.Remove(pawn);
					}
					CaravanHelper.assignedSeats.Add(pawn, assignment);
				}
				transferable.AdjustTo(transferable.GetMaximumToTransfer());
				Dialog_FormVehicleCaravan.MarkDirty();
			}
			catch(Exception ex)
			{
				Log.Error($"Failed to finalize assigning vehicle seats. Message: {ex}");
				failReason = ex.Message;
				return false;
			}
			return true;
		}

		
	}
}
