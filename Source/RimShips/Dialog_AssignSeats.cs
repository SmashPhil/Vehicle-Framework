using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace Vehicles
{
    public class Dialog_AssignSeats : Window
    {
        public Dialog_AssignSeats(List<TransferableOneWay> pawns, TransferableOneWay vehicle)
        {
            this.pawns = pawns;
            this.vehicle = vehicle.AnyThing as VehiclePawn;
            trad = vehicle;
            vehicleTex = ContentFinder<Texture2D>.Get(this.vehicle.kindDef.lifeStages.FirstOrDefault().bodyGraphicData.texPath + "_north", true);

            absorbInputAroundWindow = true;
            closeOnCancel = true;

            if (assignedSeats is null)
                assignedSeats = new Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>>();

            foreach(VehicleHandler handler in this.vehicle.GetComp<CompVehicle>().handlers)
            {
                foreach(Pawn pawn in handler.handlers)
                {
                    assignedSeats.Add(pawn, new Pair<VehiclePawn, VehicleHandler>(this.vehicle, handler));
                }
            }
        }

        public override Vector2 InitialSize => new Vector2(1024f, (float)Verse.UI.screenHeight / 2);

        public override void DoWindowContents(Rect inRect)
        {
            DrawVehicleMenu(inRect);
            DrawVehicleTex();
            DoBottomButtons(inRect);
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
            foreach(VehicleHandler handler in vehicle.GetComp<CompVehicle>().handlers)
            {
                assignedRect.y += 30f;
                //TODO - Color for required seats

                int seatsOccupied = assignedSeats.Where(r => r.Value.Second.role == handler.role).Select(p => p.Key).Count();
                Color countColor = handler.role.RequiredForCaravan ? seatsOccupied < handler.role.slotsToOperate ? Color.red : seatsOccupied == handler.role.slots ? Color.grey : Color.white : seatsOccupied == handler.role.slots ? Color.grey : Color.white;
                HelperMethods.LabelUnderlined(assignedRect, handler.role.label, $"({handler.role.slots - assignedSeats.Where(r => r.Value.Second.role == handler.role).Select(p => p.Key).Count()})", Color.white, countColor, Color.white);
                Rect assignedPawnIconRect = new Rect(assignedRect.x, assignedRect.y, 30f, 30f);
                Rect assignedPawnRect = new Rect(assignedPawnIconRect.x + 30f, assignedRect.y, pawnsRect.width, 30f);

                assignedRect.y += 30f;
                assignedPawnRect.y += 30f;
                assignedPawnIconRect.y += 35f;

                Rect roleRect = new Rect(assignedRect.x, assignedRect.y, assignedRect.width, 30f + 30f * assignedSeats.Where(r => r.Value.Second.role == handler.role).Select(p => p.Key).Count());
                //Widgets.DrawBoxSolid(roleRect, Color.red); //Draw drop area
                bool slotsAvailable = assignedSeats.Where(r => r.Value.Second.role == handler.role).Select(p => p.Key).Count() < handler.role.slots;
                if(slotsAvailable && Mouse.IsOver(roleRect) && draggedPawn != null)
                {
                    if(Event.current.type == EventType.MouseUp && Event.current.button == 0)
                    {
                        if(!handler.role.handlingTypes.NullOrEmpty() && (!draggedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || draggedPawn.Downed || draggedPawn.Dead) )
                        {
                            Messages.Message("IncapableStatusForRole".Translate(draggedPawn.LabelShortCap), MessageTypeDefOf.RejectInput);
                        }
                        else
                        {
                            assignedSeats.Add(draggedPawn, new Pair<VehiclePawn, VehicleHandler>(vehicle, handler));
                        }
                    }
                }
                var removalList = new List<Pawn>();
                foreach(KeyValuePair<Pawn, Pair<VehiclePawn, VehicleHandler>> assignedKVP in assignedSeats.Where(r => r.Value.Second.role == handler.role))
                {
                    Widgets.Label(assignedPawnRect, assignedKVP.Key.LabelCap);
                    Widgets.ThingIcon(assignedPawnIconRect, assignedKVP.Key);
                    Rect removalButtonRect = new Rect(roleRect.x + roleRect.width - 100f, assignedPawnRect.y, 90f, 20f);
                    if(!assignedKVP.Value.First.GetComp<CompVehicle>().AllPawnsAboard.Contains(assignedKVP.Key) && Widgets.ButtonText(removalButtonRect, "RemoveFromRole".Translate()))
                    {
                        removalList.Add(assignedKVP.Key);
                    }
                    assignedRect.y += 30f;
                    assignedPawnRect.y += 30f;
                    assignedPawnIconRect.y += 30f;
                }
                foreach(Pawn p in removalList)
                {
                     assignedSeats.Remove(p);
                }
                if (!slotsAvailable)
                {
                    assignedRect.y -= 15f;
                }
            }
            if(Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                draggedPawn = null;
            }
            
            
            Widgets.Label(pawnsRect, "Colonists".Translate());
            Rect colonistIconRect = new Rect(pawnsRect.x, pawnsRect.y + 30f, 30f, 30f);
            Rect colonistRect = new Rect(colonistIconRect.x + 30f, pawnsRect.y + 35f, pawnsRect.width, 30f);
            foreach(Pawn pawn in pawns.Select(p => p.AnyThing as Pawn).Where(a => !assignedSeats.ContainsKey(a)))
            {
                Rect entryButtonRect = new Rect(colonistIconRect.x + colonistRect.width - 100f, colonistRect.y, 90f, 20f);
                if(Mouse.IsOver(colonistRect) && !Mouse.IsOver(entryButtonRect))
                {
                    if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        draggedPawn = pawn;
                        draggedItemPosOffset = Event.current.mousePosition - colonistRect.position;
                        draggedIconPosOffset = Event.current.mousePosition - colonistIconRect.position;
				        Event.current.Use();
				        SoundDefOf.Click.PlayOneShotOnCamera(null);
                    }
                }

                if(draggedPawn == pawn)
                {
                    Rect draggedRect = new Rect(Event.current.mousePosition.x - draggedItemPosOffset.x, Event.current.mousePosition.y - draggedItemPosOffset.y, colonistRect.width, colonistRect.height);
                    Rect draggedIconRect = new Rect(Event.current.mousePosition.x - draggedIconPosOffset.x, Event.current.mousePosition.y - draggedIconPosOffset.y, colonistIconRect.width, colonistIconRect.height);

                    Widgets.Label(draggedRect, pawn.LabelCap);
                    Widgets.ThingIcon(draggedIconRect, pawn);
                }
                else
                {
                    if(Widgets.ButtonText(entryButtonRect, "AddToRole".Translate()))
                    {
                        VehicleHandler firstHandler = vehicle.GetComp<CompVehicle>().handlers.FirstOrDefault(x => assignedSeats.Where(r => r.Value.Second.role == x.role).Select(p => p.Key).Count() < x.role.slots);
                        if(firstHandler != null)
                            assignedSeats.Add(pawn, new Pair<VehiclePawn, VehicleHandler>(vehicle, firstHandler));
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
            if(Widgets.ButtonText(buttonRect, "Assign".Translate()))
            {
                if(!FinalizeSeats(out string failReason))
                {
                    Messages.Message("AssignFailure".Translate(vehicle.LabelCap, failReason), MessageTypeDefOf.RejectInput);
                    return;
                }
                Close(true);
            }
            buttonRect.x += ButtonWidth;
            if(Widgets.ButtonText(buttonRect, "CancelAssigning".Translate()))
            {
                Close(true);
            }
            buttonRect.x += ButtonWidth;
            if(Widgets.ButtonText(buttonRect, "ClearSeats".Translate()))
            {
                SoundDefOf.Click.PlayOneShotOnCamera(null);
                assignedSeats.Clear();
            }
        }

        private bool FinalizeSeats(out string failReason)
        {
            failReason = string.Empty;
            foreach(VehicleHandler handler in vehicle.GetComp<CompVehicle>().handlers.Where(x => x.role.RequiredForCaravan))
            {
                if (assignedSeats.Where(r => r.Value.Second.role == handler.role).Select(k => k.Key).Count() >= handler.role.slotsToOperate)
                    continue;
                failReason = "CantAssignVehicle".Translate(vehicle.LabelCap);
                return false;
            }
            try
            {
                trad.ForceTo(1);
                foreach(Pawn assignedPawn in assignedSeats.Keys)
                {
                    pawns.FirstOrDefault(p => (p.AnyThing as Pawn) == assignedPawn)?.ForceTo(1);
                }

                foreach(KeyValuePair<Pawn, Pair<VehiclePawn, VehicleHandler>> seating in assignedSeats)
                {
                    if (HelperMethods.assignedSeats.ContainsKey(seating.Key))
                        HelperMethods.assignedSeats.Remove(seating.Key);
                    HelperMethods.assignedSeats.Add(seating.Key, seating.Value);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Failed to finalize assigning vehicle seats. Message: {ex.Message} ST: {ex.StackTrace}");
                failReason = ex.Message;
                return false;
            }
            return true;
        }
        
        private void DrawVehicleTex()
        {
            Rect displayRect = new Rect(vehicle.GetComp<CompVehicle>().Props.displayUICoord.x, vehicle.GetComp<CompVehicle>().Props.displayUICoord.y, vehicle.GetComp<CompVehicle>().Props.displayUISize.x, vehicle.GetComp<CompVehicle>().Props.displayUISize.y);
            GUI.DrawTexture(displayRect, vehicleTex);

            if (vehicle.GetComp<CompCannons>() != null)
            {
                foreach (CannonHandler cannon in vehicle.GetComp<CompCannons>().Cannons.OrderBy(x => x.drawLayer))
                {
                    PawnKindLifeStage biggestStage = vehicle.kindDef.lifeStages.MaxBy(x => x.bodyGraphicData?.drawSize ?? Vector2.zero);
                    float baseWidth = (vehicle.GetComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonDrawSize.x;
                    float baseHeight = (vehicle.GetComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonDrawSize.y;

                    float xBase = displayRect.x + (displayRect.width / 2) - (baseWidth / 2) - ((vehicle.GetComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.baseCannonRenderLocation.x);
                    float yBase = displayRect.y + (displayRect.height / 2) - (baseHeight / 2) - ((vehicle.GetComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.baseCannonRenderLocation.y);

                    Rect baseCannonDrawnRect = new Rect(xBase, yBase, baseWidth, baseHeight);
                    GUI.DrawTexture(baseCannonDrawnRect, cannon.CannonBaseTexture);

                    float cannonWidth = (vehicle.GetComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonTurretDrawSize.x;
                    float cannonHeight = (vehicle.GetComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonTurretDrawSize.y;

                    float xCannon = displayRect.x + (displayRect.width / 2) - (cannonWidth / 2) - ((vehicle.GetComp<CompVehicle>().Props.displayUISize.x / biggestStage.bodyGraphicData.drawSize.x) * cannon.cannonRenderLocation.x);
                    float yCannon = displayRect.y + (displayRect.height / 2) - (cannonHeight / 2) - ((vehicle.GetComp<CompVehicle>().Props.displayUISize.y / biggestStage.bodyGraphicData.drawSize.y) * cannon.cannonRenderLocation.y);

                    Rect cannonDrawnRect = new Rect(xCannon, yCannon, cannonWidth, cannonHeight);
                    GUI.DrawTexture(cannonDrawnRect, cannon.CannonTexture);

                    if (RimShipMod.mod.settings.debugDrawCannonGrid)
                    {
                        Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.width);
                        Widgets.DrawLineHorizontal(cannonDrawnRect.x, cannonDrawnRect.y + cannonDrawnRect.height, cannonDrawnRect.width);
                        Widgets.DrawLineVertical(cannonDrawnRect.x, cannonDrawnRect.y, cannonDrawnRect.height);
                        Widgets.DrawLineVertical(cannonDrawnRect.x + cannonDrawnRect.width, cannonDrawnRect.y, cannonDrawnRect.height);
                    }
                }
            }
        }

        private const float ButtonWidth = 120f;
        private const float ButtonHeight = 30f;

        private Vector2 pawnsScrollPosition;
        private Vector2 assignedScrollPosition;

        private Dictionary<Pawn, Pair<VehiclePawn, VehicleHandler>> assignedSeats;

        private Pawn draggedPawn;
        private Vector2 draggedItemPosOffset;
        private Vector2 draggedIconPosOffset;

        private List<TransferableOneWay> pawns;
        private VehiclePawn vehicle;
        private TransferableOneWay trad;
        private Texture2D vehicleTex;
    }
}
