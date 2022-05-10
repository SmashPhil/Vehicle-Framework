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

		private Pawn draggedPawn;
		private Vector2 draggedItemPosOffset;
		private Vector2 draggedIconPosOffset;

		private readonly Dictionary<Pawn, (VehiclePawn vehicle, VehicleHandler handler)> assignedSeats = new Dictionary<Pawn, (VehiclePawn, VehicleHandler)>();

		private readonly List<TransferableOneWay> pawns;
		
		private readonly TransferableOneWay transferable;

		public Dialog_AssignSeats(List<TransferableOneWay> pawns, TransferableOneWay transferable)
		{
			this.transferable = transferable;
			this.pawns = pawns.Where(p => p.AnyThing is Pawn pawn && (Vehicle.handlers.NotNullAndAny(handler => handler.handlers.Contains(pawn)) ||
				!CaravanHelper.assignedSeats.ContainsKey(pawn) || (CaravanHelper.assignedSeats.ContainsKey(pawn) && CaravanHelper.assignedSeats[pawn].vehicle == Vehicle))).ToList();

			absorbInputAroundWindow = true;
			closeOnCancel = true;

			assignedSeats ??= new Dictionary<Pawn, (VehiclePawn, VehicleHandler)>();

			foreach (VehicleHandler handler in Vehicle.handlers)
			{
				foreach (Pawn pawn in handler.handlers)
				{
					assignedSeats.Add(pawn, (Vehicle, handler));
				}
			}
			foreach (var preassignedSeat in CaravanHelper.assignedSeats)
			{
				if (!assignedSeats.ContainsKey(preassignedSeat.Key))
				{
					assignedSeats.Add(preassignedSeat.Key, preassignedSeat.Value);
				}
			}
		}

		private VehiclePawn Vehicle => transferable.AnyThing as VehiclePawn;

		public override Vector2 InitialSize => new Vector2(1024f, (float)Verse.UI.screenHeight / 2);

		public override void DoWindowContents(Rect rect)
		{
			DrawVehicleMenu(rect);
			//y = assignedMenu.y
			Rect displayRect = new Rect(0, 35, rect.width / 3, rect.height - ButtonHeight).ContractedBy(ButtonHeight);
			RenderHelper.DrawVehicle(displayRect, Vehicle, Vehicle.Pattern, true, Vehicle.DrawColor, Vehicle.DrawColorTwo, Vehicle.DrawColorThree);
			DoBottomButtons(rect);
		}

		private void DrawVehicleMenu(Rect rect)
		{
			Rect assignedRect = new Rect(rect.width * .6666f, 10f, rect.width / 3, rect.height - ButtonHeight * 3);
			Rect pawnsRect = new Rect(rect.width * .3333f, 10f, rect.width / 3, rect.height - ButtonHeight * 3);

			Rect assignedMenu = new Rect(assignedRect.x - 1, assignedRect.y + 25f, assignedRect.width, assignedRect.height);
			Rect pawnsMenu = new Rect(pawnsRect.x - 1, pawnsRect.y + 25f, pawnsRect.width, pawnsRect.height);

			Widgets.DrawMenuSection(assignedMenu);
			Widgets.DrawMenuSection(pawnsMenu);
			assignedRect.x += 1;
			pawnsRect.x += 1;

			Widgets.Label(assignedRect, "Assigned".Translate());
			foreach (VehicleHandler handler in Vehicle.handlers)
			{
				assignedRect.y += 30f;
				//TODO - Color for required seats

				int seatsOccupied = assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count();
				Color countColor = handler.role.RequiredForCaravan ? seatsOccupied < handler.role.slotsToOperate ? Color.red : seatsOccupied == handler.role.slots ? Color.grey : Color.white : seatsOccupied == handler.role.slots ? Color.grey : Color.white;
				UIElements.LabelUnderlined(assignedRect, handler.role.label, $"({handler.role.slots - assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count()})", Color.white, countColor, Color.white);
				Rect assignedPawnIconRect = new Rect(assignedRect.x, assignedRect.y, 30f, 30f);
				Rect assignedPawnRect = new Rect(assignedPawnIconRect.x + 30f, assignedRect.y, pawnsRect.width, 30f);

				assignedRect.y += 30f;
				assignedPawnRect.y += 30f;
				assignedPawnIconRect.y += 35f;

				Rect roleRect = new Rect(assignedRect.x, assignedRect.y, assignedRect.width, 30f + 30f * assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count());
				//Widgets.DrawBoxSolid(roleRect, Color.red); //Draw drop area
				bool slotsAvailable = assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(p => p.Key).Count() < handler.role.slots;
				if (slotsAvailable && Mouse.IsOver(roleRect) && draggedPawn != null)
				{
					if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
					{
						if (!handler.role.handlingTypes.NullOrEmpty() && !draggedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || draggedPawn.Downed || draggedPawn.Dead)
						{
							if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
							{
								Messages.Message("IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), MessageTypeDefOf.RejectInput);
							}
							else
							{
								Messages.Message("IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), MessageTypeDefOf.CautionInput);
								assignedSeats.Add(draggedPawn, (Vehicle, handler));
							}
						}
						else
						{
							assignedSeats.Add(draggedPawn, (Vehicle, handler));
						}
					}
				}
				var removalList = new List<Pawn>();
				foreach (KeyValuePair<Pawn, (VehiclePawn vehicle, VehicleHandler handler)> assignedKVP in assignedSeats.Where(r => r.Value.handler.role == handler.role))
				{
					Widgets.Label(assignedPawnRect, assignedKVP.Key.LabelCap);
					Widgets.ThingIcon(assignedPawnIconRect, assignedKVP.Key);
					Rect removalButtonRect = new Rect(roleRect.x + roleRect.width - 100f, assignedPawnRect.y, 90f, 20f);
					if(!assignedKVP.Value.vehicle.AllPawnsAboard.Contains(assignedKVP.Key) && Widgets.ButtonText(removalButtonRect, "RemoveFromRole".Translate()))
					{
						removalList.Add(assignedKVP.Key);
					}
					assignedRect.y += 30f;
					assignedPawnRect.y += 30f;
					assignedPawnIconRect.y += 30f;
				}
				foreach (Pawn pawn in removalList)
				{
					 assignedSeats.Remove(pawn);
				}
				if (!slotsAvailable)
				{
					assignedRect.y -= 15f;
				}
			}
			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				draggedPawn = null;
			}
			
			
			Widgets.Label(pawnsRect, "Colonists".Translate());
			Rect colonistIconRect = new Rect(pawnsRect.x, pawnsRect.y + 30f, 30f, 30f);
			Rect colonistRect = new Rect(colonistIconRect.x + 30f, pawnsRect.y + 35f, pawnsRect.width, 30f);
			foreach (Pawn pawn in pawns.Select(p => p.AnyThing as Pawn).Where(a => !assignedSeats.ContainsKey(a)))
			{
				Rect entryButtonRect = new Rect(colonistIconRect.x + colonistRect.width - 100f, colonistRect.y, 90f, 20f);
				Rect fullColonistBarRect = new Rect(colonistRect)
				{
					x = colonistIconRect.x
				};
				if (Mouse.IsOver(fullColonistBarRect) && !Mouse.IsOver(entryButtonRect))
				{
					Widgets.DrawHighlight(fullColonistBarRect);
					if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						draggedPawn = pawn;
						draggedItemPosOffset = Event.current.mousePosition - colonistRect.position;
						draggedIconPosOffset = Event.current.mousePosition - colonistIconRect.position;
						Event.current.Use();
						SoundDefOf.Click.PlayOneShotOnCamera(null);
					}
				}

				if (draggedPawn == pawn)
				{
					Rect draggedRect = new Rect(Event.current.mousePosition.x - draggedItemPosOffset.x, Event.current.mousePosition.y - draggedItemPosOffset.y, colonistRect.width, colonistRect.height);
					Rect draggedIconRect = new Rect(Event.current.mousePosition.x - draggedIconPosOffset.x, Event.current.mousePosition.y - draggedIconPosOffset.y, colonistIconRect.width, colonistIconRect.height);

					Widgets.Label(draggedRect, pawn.LabelCap);
					Widgets.ThingIcon(draggedIconRect, pawn);
				}
				else
				{
					if (Widgets.ButtonText(entryButtonRect, "AddToRole".Translate()))
					{
						bool Validate(VehicleHandler handler) => assignedSeats.Where(seat => seat.Value.handler.role == handler.role).Select(p => p.Key).Count() < handler.role.slots;
						VehicleHandler firstHandler = Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(pawn) && Validate(handler));
						firstHandler ??= Vehicle.handlers.FirstOrDefault(handler => !handler.RequiredForMovement && Validate(handler));
						if (firstHandler != null)
						{
							if (!firstHandler.CanOperateRole(pawn))
							{
								if (firstHandler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
								{
									Messages.Message("IncapableStatusForRole".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput);
								}
								else
								{
									Messages.Message("IncapableStatusForRole".Translate(pawn.LabelShortCap), MessageTypeDefOf.CautionInput);
									assignedSeats.Add(pawn, (Vehicle, firstHandler));
								}
							}
							else
							{
								assignedSeats.Add(pawn, (Vehicle, firstHandler));
							}
						}
					}
					Widgets.Label(colonistRect, pawn.LabelCap);
					Widgets.ThingIcon(colonistIconRect, pawn);
				}
				colonistIconRect.y += 35f;
				colonistRect.y += 35f;
			}
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect buttonRect = new Rect(rect.width - ButtonWidth * 3, rect.height - ButtonHeight, ButtonWidth, ButtonHeight);
			if (Widgets.ButtonText(buttonRect, "Assign".Translate()))
			{
				if (!FinalizeSeats(out string failReason))
				{
					Messages.Message("AssignFailure".Translate(failReason), MessageTypeDefOf.RejectInput);
					return;
				}
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "CancelAssigning".Translate()))
			{
				Close(true);
			}
			buttonRect.x += ButtonWidth;
			if (Widgets.ButtonText(buttonRect, "ClearSeats".Translate()))
			{
				SoundDefOf.Click.PlayOneShotOnCamera(null);
				foreach (Pawn pawn in assignedSeats.Keys)
				{
					if (pawns.Select(p => p.AnyThing as Pawn).Contains(pawn) && assignedSeats.ContainsKey(pawn))
						assignedSeats.Remove(pawn);
				}
			}
		}

		private bool FinalizeSeats(out string failReason)
		{
			failReason = string.Empty;
			foreach (VehicleHandler handler in Vehicle.handlers.Where(x => x.role.RequiredForCaravan))
			{
				if (assignedSeats.Where(r => r.Value.handler.role == handler.role).Select(k => k.Key).Count() < handler.role.slotsToOperate)
				{
					failReason = "CantAssignVehicle".Translate(Vehicle.LabelCap);
					return false;
				}
			}
			try
			{
				CaravanHelper.ClearAssignedSeats(Vehicle, (Pawn pawn) => pawns.FirstOrDefault(p => (p.AnyThing as Pawn) == pawn)?.ForceTo(0));

				//Update all current pawns being assigned to this vehicle in Pawns tab
				foreach (Pawn assignedPawn in assignedSeats.Keys)
				{
					pawns.FirstOrDefault(p => (p.AnyThing as Pawn) == assignedPawn)?.ForceTo(1);
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
				Log.Error($"Failed to finalize assigning vehicle seats. Message: {ex.Message} ST: {ex.StackTrace}");
				failReason = ex.Message;
				return false;
			}
			return true;
		}

		
	}
}
