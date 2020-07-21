using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Vehicles.Build;
using Vehicles.Defs;
using Vehicles.Lords;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;


namespace Vehicles
{
    public enum VehicleMovementStatus { Offline, Online }

    public class CompVehicle : ThingComp
    {
        public List<Jobs.Bill_BoardShip> bills = new List<Jobs.Bill_BoardShip>();

        public bool currentlyFishing = false;
        public bool draftStatusChanged = false;
        public bool beached = false;
        public bool showAllItemsOnMap = false;
        private float bearingAngle;
        private int turnSign;
        public float turnSpeed;

        private List<IntVec3> currentTravelCells;
        public List<TransferableOneWay> cargoToLoad;

        private bool outOfFoodNotified = false;

        public List<VehicleHandler> handlers = new List<VehicleHandler>();
        public VehicleMovementStatus movementStatus = VehicleMovementStatus.Online;

        public NavigationCategory navigationCategory = NavigationCategory.Opportunistic;

        public bool warnNoFuel;
        public List<IntVec3> OffsetIndices => CellRect.CenteredOn(Pawn.Position, Pawn.def.Size.x, Pawn.def.Size.z).ToList();

        public bool CanMove => (Props.vehicleMovementPermissions > VehiclePermissions.DriverNeeded || MovementHandlerAvailable) && movementStatus == VehicleMovementStatus.Online;

        public VehiclePawn Pawn => parent as VehiclePawn;
        public CompProperties_Vehicle Props => (CompProperties_Vehicle)this.props;

        private float cargoCapacity;
        private float armorPoints;
        private float moveSpeedModifier;

        public float ActualMoveSpeed => this.Pawn.GetStatValue(StatDefOf.MoveSpeed, true) + MoveSpeedModifier;

        public float ArmorPoints
        {
            get
            {
                return armorPoints;
            }
            set
            {
                if (value < 0)
                    armorPoints = 0f;
                armorPoints = value;
            }
        }

        public float CargoCapacity
        {
            get
            {
                return cargoCapacity;
            }
            set
            {
                if (value < 0)
                    cargoCapacity = 0f;
                cargoCapacity = value;
            }
        }

        public float MoveSpeedModifier
        {
            get
            {
                return moveSpeedModifier;
            }
            set
            {
                if (value < 0)
                    moveSpeedModifier = 0;
                moveSpeedModifier = value;
            }
        }


        public bool MovementHandlerAvailable
        {
            get
            {
                foreach (VehicleHandler handler in this.handlers)
                {
                    if (handler.role.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Movement) && handler.handlers.Count < handler.role.slotsToOperate)
                    {
                        return false;
                    }
                }
                if (Pawn.TryGetComp<CompRefuelable>() != null && Pawn.GetComp<CompRefuelable>().Fuel <= 0f)
                    return false;
                return true;
            }
        }

        public int PawnCountToOperate
        {
            get
            {
                int pawnCount = 0;
                foreach (VehicleRole r in Props.roles)
                {
                    if (r.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Movement))
                        pawnCount += r.slotsToOperate;
                }
                return pawnCount;
            }
        }

        public int PawnCountToOperateLeft
        {
            get
            {
                int pawnsMounted = 0;
                foreach(VehicleHandler handler in handlers.Where(h => h.role.handlingTypes.Contains(HandlingTypeFlags.Movement)))
                {
                    pawnsMounted += handler.handlers.Count;
                }
                return PawnCountToOperate - pawnsMounted;
            }
        }

        public List<Pawn> AllPawnsAboard
        {
            get
            {
                List<Pawn> pawnsOnShip = new List<Pawn>();
                if (!(handlers is null) && handlers.Count > 0)
                {
                    foreach (VehicleHandler handler in handlers)
                    {
                        if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
                    }
                }

                return pawnsOnShip;
            }
        }

        public List<Pawn> AllCrewAboard
        {
            get
            {
                List<Pawn> crewOnShip = new List<Pawn>();
                if (!(handlers is null))
                {
                    foreach (VehicleHandler handler in handlers)
                    {
                        if (handler.role.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Movement))
                        {
                            crewOnShip.AddRange(handler.handlers);
                        }
                    }
                }
                return crewOnShip;
            }
        }

        public List<Pawn> AllCannonCrew
        {
            get
            {
                List<Pawn> weaponCrewOnShip = new List<Pawn>();
                foreach(VehicleHandler handler in handlers)
                {
                    if (handler.role.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Cannon))
                    {
                        weaponCrewOnShip.AddRange(handler.handlers);
                    }
                }
                return weaponCrewOnShip;
            }
        }

        public List<Pawn> Passengers
        {
            get
            {
                List<Pawn> passengersOnShip = new List<Pawn>();
                if(!(handlers is null))
                {
                    foreach(VehicleHandler handler in handlers)
                    {
                        if(handler.role.handlingTypes.NullOrEmpty())
                        {
                            passengersOnShip.AddRange(handler.handlers);
                        }
                    }
                }
                return passengersOnShip;
            }
        }

        public List<Pawn> AllCapablePawns
        {
            get
            {
                List<Pawn> pawnsOnShip = new List<Pawn>();
                if(!(handlers is null) && handlers.Count > 0)
                {
                    foreach (VehicleHandler handler in handlers)
                    {
                        if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
                    }
                }
                pawnsOnShip = pawnsOnShip.Where(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))?.ToList();
                return pawnsOnShip ?? new List<Pawn>() { };
            }
        }

        public int SeatsAvailable
        {
            get
            {
                int x = 0;
                foreach(VehicleHandler handler in handlers)
                {
                    x += handler.role.slots - handler.handlers.Count;
                }
                return x;
            }
        }

        public int TotalSeats
        {
            get
            {
                int x = 0;
                foreach(VehicleHandler handler in handlers)
                {
                    x += handler.role.slots;
                }
                return x;
            }
        }

        public void AddHandlers(List<VehicleHandler> handlerList)
        {
            if (handlerList is null)
                return;
            foreach(VehicleHandler handler in handlerList)
            {
                var handlerPermanent = new VehicleHandler(this.Pawn, handler.role);
                if(handler.currentlyReserving is null) handler.currentlyReserving = new List<Pawn>();
                handlers.Add(handlerPermanent);
            }
        }

        public int AverageSkillOfCapablePawns(SkillDef skill)
        {
            int value = 0;
            foreach(Pawn p in AllCapablePawns)
                value += p.skills.GetSkill(skill).Level;
            value /= AllCapablePawns.Count;
            return value;
        }

        public List<VehicleHandler> GetAllHandlersMatch(HandlingTypeFlags handlingTypeFlag, string cannonKey = "")
        {
            return handlers.FindAll(x => x.role.handlingTypes.AnyNullified(h => h == handlingTypeFlag) && (handlingTypeFlag != HandlingTypeFlags.Cannon || (!x.role.cannonIds.NullOrEmpty() && x.role.cannonIds.Contains(cannonKey))));
        }

        public VehicleHandler GetHandlersMatch(Pawn pawn)
        {
            return handlers.FirstOrDefault(x => x.handlers.Contains(pawn));
        }

        public VehicleHandler NextAvailableHandler(HandlingTypeFlags flag = HandlingTypeFlags.Null)
        {
            foreach(VehicleHandler handler in flag == HandlingTypeFlags.Null ? handlers : handlers.Where(h => h.role.handlingTypes.Contains(flag)))
            {
                if(handler.AreSlotsAvailable)
                {
                    return handler;
                }
            }
            return null;
        }

        public VehicleHandler ReservedHandler(Pawn p)
        {
            foreach(VehicleHandler handler in handlers)
            {
                if(handler.currentlyReserving.Contains(p))
                {
                    return handler;
                }
            }
            return null;
        }

        public void Rename()
        {
            if(this.Props.nameable)
            {
                Find.WindowStack.Add(new Dialog_GiveShipName(this.Pawn));
            }
        }
        
        public void Recolor()
        {
            Log.Message("COLOR");
        }

        public void EnqueueCellImmediate(IntVec3 cell)
        {
            currentTravelCells.Clear();
            currentTravelCells.Add(cell);
        }

        public void EnqueueCell(IntVec3 cell)
        {
            currentTravelCells.Add(cell);
        }

        public float BearingAngle
        {
            get
            {
                if (!Props.diagonalRotation)
                    throw new NotSupportedException($"Attempting to get relative angle when smooth pathing has been disabled for {Pawn.LabelShort} with DiagonalRotation: {this.Props.diagonalRotation}. It should not reach here.");
                return bearingAngle;
            }
            set
            {
                bearingAngle = value;
                if (bearingAngle < -180)
                    bearingAngle = 180;
                else if (bearingAngle > 180)
                    bearingAngle = -180;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(!this.Pawn.Dead)
            {
                if(!cargoToLoad.NullOrEmpty())
                {
                    if (!cargoToLoad.AnyNullified(x => x.AnyThing != null && x.CountToTransfer > 0 && !this.Pawn.inventory.innerContainer.Contains(x.AnyThing)))
                    {
                        cargoToLoad.Clear();
                    }
                    else
                    {
                        Command_Action cancelLoad = new Command_Action();
                        cancelLoad.defaultLabel = "DesignatorCancel".Translate();
                        cancelLoad.icon = TexCommandVehicles.CancelPackCargoIcon;
                        cancelLoad.action = delegate ()
                        {
                            cargoToLoad.Clear();
                        };
                        yield return cancelLoad;
                    }
                }
                else
                {
                    Command_Action loadShip = new Command_Action();
                    loadShip.defaultLabel = "LoadShip".Translate();
                    loadShip.icon = TexCommandVehicles.PackCargoIcon;
                    loadShip.action = delegate ()
                    {
                        Find.WindowStack.Add(new Dialog_LoadCargo(Pawn));
                    };
                    yield return loadShip;
                }

                if (!this.Pawn.Drafted)
                {
                    Command_Action unloadAll = new Command_Action();
                    unloadAll.defaultLabel = "Disembark".Translate();
                    unloadAll.icon = TexCommandVehicles.UnloadAll;
                    unloadAll.action = delegate ()
                    {
                        DisembarkAll();
                        this.Pawn.drafter.Drafted = false;
                    };
                    unloadAll.hotKey = KeyBindingDefOf.Misc2;
                    yield return unloadAll;
                
                    foreach (VehicleHandler handler in handlers)
                    {
                        for (int i = 0; i < handler.handlers.Count; i++)
                        {
                            Pawn currentPawn = handler.handlers.InnerListForReading[i];
                            Command_Action unload = new Command_Action();
                            unload.defaultLabel = "Unload " + currentPawn.LabelShort;
                            unload.icon = TexCommandVehicles.UnloadPassenger;
                            unload.action = delegate ()
                            {
                                DisembarkPawn(currentPawn);
                            };
                            yield return unload;
                        }
                    }
                    if(this.Props.fishing && FishingCompatibility.fishingActivated)
                    {
                        Command_Toggle fishing = new Command_Toggle
                        {
                            defaultLabel = "BoatFishing".Translate(),
                            defaultDesc = "BoatFishingDesc".Translate(),
                            icon = TexCommandVehicles.FishingIcon,
                            isActive = (() => this.currentlyFishing),
                            toggleAction = delegate ()
                            {
                                this.currentlyFishing = !this.currentlyFishing;
                            }
                        };
                        yield return fishing;
                    }
                }
                if(this.Pawn.GetLord()?.LordJob is LordJob_FormAndSendVehicles)
                {
                    Command_Action forceCaravanLeave = new Command_Action
                    {
                        defaultLabel = "ForceLeaveCaravan".Translate(),
                        defaultDesc = "ForceLeaveCaravanDesc".Translate(),
                        icon = TexCommandVehicles.CaravanIcon,
                        action = delegate ()
                        {
                            (this.Pawn.GetLord().LordJob as LordJob_FormAndSendVehicles).ForceCaravanLeave = true;
                            Messages.Message("ForceLeaveConfirmation".Translate(), MessageTypeDefOf.TaskCompletion);
                        }
                    };
                    yield return forceCaravanLeave;
                }
            }
            yield break;
        }

        public void MultiplePawnFloatMenuOptions(List<Pawn> pawns)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            FloatMenuOption opt1 = new FloatMenuOption("BoardShipGroup".Translate(this.Pawn.LabelShort), delegate ()
            {
                List<IntVec3> cells = this.Pawn.OccupiedRect().Cells.ToList();
                foreach (Pawn p in pawns)
                {
                    if(cells.Contains(p.Position))
                        continue;
                    Job job = new Job(JobDefOf_Ships.Board, this.parent);
                    p.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                    VehicleHandler handler = p.IsColonistPlayerControlled ? NextAvailableHandler() : handlers.FirstOrDefault(h => h.role.handlingTypes.NullOrEmpty());
                    GiveLoadJob(p, handler);
                    ReserveSeat(p, handler);
                }
            }, MenuOptionPriority.Default, null, null, 0f, null, null);
            FloatMenuOption opt2 = new FloatMenuOption("BoardShipGroupFail".Translate(this.Pawn.LabelShort), null, MenuOptionPriority.Default, null, null, 0f, null, null);
            opt2.Disabled = true;
            int r = 0;
            foreach(VehicleHandler h in this.handlers)
            {
                r += h.currentlyReserving.Count;
            }
            options.Add(pawns.Count + r > this.SeatsAvailable ? opt2 : opt1);
            
            FloatMenuMulti floatMenuMap = new FloatMenuMulti(options, pawns, this.Pawn, pawns[0].LabelCap, Verse.UI.MouseMapPosition())
            {
                givesColonistOrders = true
            };
            Find.WindowStack.Add(floatMenuMap);
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (!pawn.RaceProps.ToolUser)
            {
                yield break;
            }
            if (!pawn.CanReserveAndReach(this.parent, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, false))
            {
                yield break;
            }
            if(pawn is null)
                yield break;
            if (this.movementStatus is VehicleMovementStatus.Offline)
            {
                yield break;
            }
            
            foreach (VehicleHandler handler in handlers)
            {
                if(handler.AreSlotsAvailable)
                {
                    
                    FloatMenuOption opt = new FloatMenuOption("BoardShip".Translate(this.parent.LabelShort, handler.role.label, (handler.role.slots - (handler.handlers.Count + handler.currentlyReserving.Count)).ToString()), 
                    delegate ()
                    {
                        Job job = new Job(JobDefOf_Ships.Board, this.parent);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
                        GiveLoadJob(pawn, handler);
                        ReserveSeat(pawn, handler);
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                    yield return opt;
                }
            }
            if(this.Pawn.health.summaryHealth.SummaryHealthPercent < 0.99f)
            {
                yield return new FloatMenuOption("RepairShip".Translate(this.Pawn.LabelShort),
                delegate ()
                {
                    Job job = new Job(JobDefOf_Ships.RepairShip, this.parent);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
                }, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            yield break;
        }

        public void GiveLoadJob(Pawn pawn, VehicleHandler handler)
        {
            if (this.bills is null) this.bills = new List<Jobs.Bill_BoardShip>();

            if (!(bills is null) && bills.Count > 0)
            {
                Jobs.Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawn);
                if (!(bill is null))
                {
                    bill.handler = handler;
                    return;
                }
            }
            bills.Add(new Jobs.Bill_BoardShip(pawn, handler));
        }

        public void Notify_BoardedCaravan(Pawn pawnToBoard, ThingOwner handler)
        {
            if (!pawnToBoard.IsWorldPawn())
            {
                Log.Warning("Tried boarding Caravan with non-worldpawn");
            }

            if (pawnToBoard.holdingOwner != null)
            {
                pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, handler);
            }
            else
            {
                handler.TryAdd(pawnToBoard);
            }
        }

        public void Notify_Boarded(Pawn pawnToBoard)
        {
            if(bills != null && bills.Count > 0)
            {
                Jobs.Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
                if(bill != null)
                {
                    if(pawnToBoard.IsWorldPawn())
                    {
                        Log.Error("Tried boarding ship with world pawn.");
                        return;
                    }
                    if(pawnToBoard.Spawned)
                        pawnToBoard.DeSpawn(DestroyMode.Vanish);
                    if (bill.handler.handlers.TryAdd(pawnToBoard, true))
                    {
                        if(pawnToBoard != null)
                        {
                            if (bill.handler.currentlyReserving.Contains(pawnToBoard)) bill.handler.currentlyReserving.Remove(pawnToBoard);
                            Find.WorldPawns.PassToWorld(pawnToBoard, PawnDiscardDecideMode.Decide);
                        }
                    }
                    else if(pawnToBoard.holdingOwner != null)
                    {
                        pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
                    }
                    bills.Remove(bill);
                }
            }
        }

        public void DisembarkPawn(Pawn pawn)
        {
            if(!this.Pawn.Position.Standable(this.Pawn.Map))
            {
                Messages.Message("RejectDisembarkInvalidTile".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if(!pawn.Spawned)
            {
                GenSpawn.Spawn(pawn, this.Pawn.PositionHeld.RandomAdjacentCellCardinal(), this.Pawn.MapHeld);
                if (this.Pawn.GetLord() != null)
                {
                    this.Pawn.GetLord().AddPawn(pawn);
                }
            }
            RemovePawn(pawn);
            if(!AllPawnsAboard.AnyNullified() && outOfFoodNotified)
                outOfFoodNotified = false;
        }

        public void DisembarkAll()
        {
            if(!Pawn.Position.Standable(Pawn.Map))
            {
                Messages.Message("RejectDisembarkInvalidTile".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            var pawnsToDisembark = new List<Pawn>(AllPawnsAboard);
            if( !(pawnsToDisembark is null) && pawnsToDisembark.Count > 0)
            {
                if(Pawn.GetCaravan() != null && !Pawn.Spawned)
                {
                    List<VehicleHandler> handlerList = handlers;
                    for(int i = 0; i < handlerList.Count; i++)
                    {
                        VehicleHandler handler = handlerList[i];
                        handler.handlers.TryTransferAllToContainer(Pawn.GetCaravan().pawns, false);
                    }
                    return;
                }
                foreach(Pawn p in pawnsToDisembark)
                {
                    DisembarkPawn(p);
                }
            }
        }

        public void RemovePawn(Pawn pawn)
        {
            for (int i = 0; i < this.handlers.Count; i++)
            {
                VehicleHandler handler = this.handlers[i];
                if(handler.handlers.Remove(pawn)) return;
            }
        }

        public void DeadPawnReplace(Pawn pawn)
        {
            //NEEDS IMPLEMENTATION
            /*foreach(ShipHandler h in handlers)
            {
                if(h.handlers.InnerListForReading.Contains(pawn))
                {
                    
                }
            }*/
        }

        public void BeachShip()
        {
            this.movementStatus = VehicleMovementStatus.Offline;
            this.beached = true;
        }

        public void RemoveBeachedStatus()
        {
            this.movementStatus = VehicleMovementStatus.Online;
            this.beached = false;
        }

        private void TrySatisfyPawnNeeds()
        {
            if(this.Pawn.Spawned || this.Pawn.IsCaravanMember())
            {
                foreach (Pawn p in this.AllPawnsAboard)
                {
                    TrySatisfyPawnNeeds(p);
                }
            }
        }

        private void TrySatisfyPawnNeeds(Pawn pawn)
        {
            if(pawn.Dead) return;
            List<Need> allNeeds = pawn.needs.AllNeeds;
            int tile = this.Pawn.IsCaravanMember() ? this.Pawn.GetCaravan().Tile : this.Pawn.Map.Tile;

            for(int i = 0; i < allNeeds.Count; i++)
            {
                Need need = allNeeds[i];
                if(need is Need_Rest)
                {
                    if(CaravanNightRestUtility.RestingNowAt(tile) || GetHandlersMatch(pawn).role.handlingTypes.NullOrEmpty())
                    {
                        TrySatisfyRest(pawn, need as Need_Rest);
                    }
                }
                else if (need is Need_Food)
                {
                    if(!CaravanNightRestUtility.RestingNowAt(tile))
                        TrySatisfyFood(pawn, need as Need_Food);
                }
                else if (need is Need_Chemical)
                {
                    if(!CaravanNightRestUtility.RestingNowAt(tile))
                        TrySatisfyChemicalNeed(pawn, need as Need_Chemical);
                }
                else if (need is Need_Joy)
                {
                    if (!CaravanNightRestUtility.RestingNowAt(tile))
                        TrySatisfyJoyNeed(pawn, need as Need_Joy);
                }
                else if(need is Need_Comfort)
                {
                    need.CurLevel = 0.5f;
                }
                else if(need is Need_Outdoors)
                {
                    need.CurLevel = 0.25f;
                }
            }
        }

        private void TrySatisfyRest(Pawn pawn, Need_Rest rest)
        {
            Building_Bed bed = (Building_Bed)this.Pawn.inventory.innerContainer.InnerListForReading.Find(x => x is Building_Bed); // Reserve?
            float restValue = bed is null ? 0.75f : bed.GetStatValue(StatDefOf.BedRestEffectiveness, true);
            restValue *= pawn.GetStatValue(StatDefOf.RestRateMultiplier, true);
            if(restValue > 0)
                rest.CurLevel += 0.0057142857f * restValue;
        }

        private void TrySatisfyFood(Pawn pawn, Need_Food food)
        {
            if(food.CurCategory < HungerCategory.Hungry)
                return;

            if(TryGetBestFood(pawn, out Thing thing, out Pawn owner))
            {
                food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
                if(thing.Destroyed)
                {
                    owner.inventory.innerContainer.Remove(thing);
                    if(this.Pawn.IsCaravanMember())
                    {
                        this.Pawn.GetCaravan().RecacheImmobilizedNow();
						this.Pawn.GetCaravan().RecacheDaysWorthOfFood();
                    }
                }
                if(!outOfFoodNotified && !TryGetBestFood(pawn, out thing, out Pawn owner2))
                {
                    Messages.Message("ShipOutOfFood".Translate(this.Pawn.LabelShort), this.Pawn, MessageTypeDefOf.NegativeEvent, false);
                    outOfFoodNotified = true;
                }
            }
        }

        public bool TryGetBestFood(Pawn forPawn, out Thing food, out Pawn owner)
        {
            List<Thing> list = this.Pawn.inventory.innerContainer.InnerListForReading;
            Thing thing = null;
            float num = 0f;
            foreach(Thing foodItem in list)
            {
                if(CanEatForNutrition(foodItem, forPawn))
                {
                    float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem, forPawn);
                    if(thing is null || foodScore > num)
                    {
                        thing = foodItem;
                        num = foodScore;
                    }
                }
            }
            if(this.Pawn.IsCaravanMember())
            {
                foreach(Thing foodItem2 in CaravanInventoryUtility.AllInventoryItems(this.Pawn.GetCaravan()))
                {
                    if(CanEatForNutrition(foodItem2, forPawn))
                    {
                        float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem2, forPawn);
                        if(thing is null || foodScore > num)
                        {
                            thing = foodItem2;
                            num = foodScore;
                        }
                    }
                }
            }
            
            if(thing != null)
            {
                food = thing;
                owner = this.Pawn.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.Pawn.GetCaravan(), thing) : this.Pawn;
                return true;
            }
            food = null;
            owner = null;
            return false;
        }

        private void TrySatisfyChemicalNeed(Pawn pawn, Need_Chemical chemical)
        {
            if (chemical.CurCategory >= DrugDesireCategory.Satisfied)
                return;

            if(TryGetDrugToSatisfyNeed(pawn, chemical, out Thing drug, out Pawn owner))
                this.IngestDrug(pawn, drug, owner);
        }

        public void IngestDrug(Pawn pawn, Thing drug, Pawn owner)
        {
            float num = drug.Ingested(pawn, 0f);
            Need_Food food = pawn.needs.food;
            if(food != null)
            {
                food.CurLevel += num;
            }
            if(drug.Destroyed)
            {
                owner.inventory.innerContainer.Remove(drug);
            }
        }
        public bool TryGetDrugToSatisfyNeed(Pawn forPawn, Need_Chemical chemical, out Thing drug, out Pawn owner)
        {
            Hediff_Addiction addictionHediff = chemical.AddictionHediff;
            if(addictionHediff is null)
            {
                drug = null;
                owner = null;
                return false;
            }
            List<Thing> list = this.Pawn.inventory.innerContainer.InnerListForReading;

            Thing thing = null;
            foreach(Thing t in list)
            {
                if(t.IngestibleNow && t.def.IsDrug)
                {
                    CompDrug compDrug = t.TryGetComp<CompDrug>();
                    if(compDrug != null && compDrug.Props.chemical != null)
                    {
                        if(compDrug.Props.chemical.addictionHediff == addictionHediff.def)
                        {
                            if(forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
                            {
                                thing = t;
                                break;
                            }
                        }
                    }
                }
            }
            if(this.Pawn.IsCaravanMember())
            {
                foreach(Thing t in CaravanInventoryUtility.AllInventoryItems(this.Pawn.GetCaravan()))
                {
                    if(t.IngestibleNow && t.def.IsDrug)
                    {
                        CompDrug compDrug = t.TryGetComp<CompDrug>();
                        if(compDrug != null && compDrug.Props.chemical != null)
                        {
                            if(compDrug.Props.chemical.addictionHediff == addictionHediff.def)
                            {
                                if(forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
                                {
                                    thing = t;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if(thing != null)
            {
                drug = thing;
                owner = this.Pawn.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.Pawn.GetCaravan(), thing) : this.Pawn;
                return true;
            }
            drug = null;
            owner = null;
            return false;
        }

        public static bool CanEatForNutrition(Thing item, Pawn forPawn)
        {
            return item.IngestibleNow && item.def.IsNutritionGivingIngestible && forPawn.WillEat(item, null) && item.def.ingestible.preferability > FoodPreferability.NeverForNutrition &&
                (!item.def.IsDrug || !forPawn.IsTeetotaler()) && (!forPawn.RaceProps.Humanlike || forPawn.needs.food.CurCategory >= HungerCategory.Starving || item.def.ingestible.preferability >
                FoodPreferability.DesperateOnlyForHumanlikes);
        }

        private void TrySatisfyJoyNeed(Pawn pawn, Need_Joy joy)
        {
            if(pawn.IsHashIntervalTick(1250))
            {
                float num = Pawn.vPather.MovingNow ? 4E-05f : 4E-3f; //Incorporate 'shifts'
                if (num <= 0f)
                    return;
                num *= 1250f;
                List<JoyKindDef> tmpJoyList = GetAvailableJoyKindsFor(pawn);
                JoyKindDef joyKind;
                if (!tmpJoyList.TryRandomElementByWeight((JoyKindDef x) => 1f - Mathf.Clamp01(pawn.needs.joy.tolerances[x]), out joyKind))
                    return;
                joy.GainJoy(num, joyKind);
                tmpJoyList.Clear();
            }
        }

        public List<JoyKindDef> GetAvailableJoyKindsFor(Pawn p)
        {
            List<JoyKindDef> outJoyKinds = new List<JoyKindDef>();
            if (!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Meditative))
                outJoyKinds.Add(JoyKindDefOf.Meditative);
            if(!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Social))
            {
                int num = 0;
                foreach(Pawn targetpawn in this.AllPawnsAboard)
                {
                    if(!targetpawn.Downed && targetpawn.RaceProps.Humanlike && !targetpawn.InMentalState)
                    {
                        num++;
                    }
                }
                if (num >= 2)
                    outJoyKinds.Add(JoyKindDefOf.Social);
            }
            return outJoyKinds;
        }

        public void ReserveSeat(Pawn p, VehicleHandler handler)
        {
            if(p is null || !p.Spawned) return;
            foreach(VehicleHandler h in handlers)
            {
                if(h != handler && h.currentlyReserving.Contains(p))
                {
                    h.currentlyReserving.Remove(p);
                }
            }
            if(!handler.currentlyReserving.Contains(p))
                handler.currentlyReserving.Add(p);
        }

        public bool ResolveSeating()
        {
            //if(AllCapablePawns.Count >= PawnCountToOperate)
            //{
            //    for(int r = 0; r < 100; r++)
            //    {
            //        for (int i = 0; i < handlers.Count; i++)
            //        {
            //            VehicleHandler handler = handlers[i];
            //            if (handler.currentlyReserving.Count > 0)
            //                return false;
            //        }
            //        for (int i = 0; i < handlers.Count; i++)
            //        {
            //            VehicleHandler handler = handlers[i];
            //            VehicleHandler passengerHandler = handlers.FirstOrDefault(h => h.role.handlingTypes.NullOrEmpty());
            //            if (handler.handlers.Count > handler.role.slots)
            //            {
            //                int j = 0;
            //                while(handler.handlers.Count > handler.role.slots)
            //                {
            //                    Pawn p = handler.handlers.InnerListForReading[j];
            //                    handler.handlers.TryTransferToContainer(p, passengerHandler.handlers, false);
            //                    j++;
            //                }
            //            }
            //            if (handler.role.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Movement) && handler.handlers.Count < handler.role.slotsToOperate)
            //            {
            //                if (passengerHandler.handlers.Count <= 0)
            //                {
            //                    VehicleHandler emergencyHandler = handlers.Find(x => x.role.handlingTypes.AnyNullified(h => h < HandlingTypeFlags.Movement) && x.handlers.Count > 0); //Can Optimize
            //                    Pawn transferPawnE = emergencyHandler?.handlers.InnerListForReading.Find(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && x.RaceProps.Humanlike);
            //                    if(transferPawnE is null)
            //                        continue;
            //                    emergencyHandler?.handlers.TryTransferToContainer(transferPawnE, handler.handlers, false);
            //                    continue;
            //                }
            //                Pawn transferingPawn = passengerHandler.handlers.InnerListForReading.Find(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && x.RaceProps.Humanlike);
            //                if(transferingPawn is null)
            //                    continue;
            //                passengerHandler.handlers.TryTransferToContainer(transferingPawn, handler.handlers, false);
            //            }
            //            if (handler.role.handlingTypes.AnyNullified(h => h == HandlingTypeFlags.Cannon) && handler.handlers.Count < handler.role.slotsToOperate && this.CanMove)
            //            {
            //                if(passengerHandler.handlers.Count <= 0)
            //                {
            //                    VehicleHandler emergencyHandler = this.handlers.Find(x => x.role.handlingTypes.AnyNullified(h => h < HandlingTypeFlags.Cannon) && x.handlers.Count > 0); //Can Optimize
            //                    Pawn transferPawnE = emergencyHandler?.handlers.InnerListForReading.Find(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && x.RaceProps.Humanlike);
            //                    if(transferPawnE is null)
            //                        continue;
            //                    emergencyHandler?.handlers.TryTransferToContainer(transferPawnE, handler.handlers, false);
            //                    continue;
            //                }
            //                Pawn transferingPawn = passengerHandler.handlers.InnerListForReading.Find(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && x.RaceProps.Humanlike);
            //                if(transferingPawn is null)
            //                    continue;
            //                passengerHandler.handlers.TryTransferToContainer(transferingPawn, handler.handlers, false);
            //            }
            //        }
            //    }
            //}
            return CanMove;
        }

        public void CheckTurnSign(float? turnAngle = null)
        {
            if(turnAngle is null)
                turnAngle = (float)Pawn.DrawPos.Angle180RotationTracker(currentTravelCells.First().ToVector3Shifted());
            turnSign = HelperMethods.NearestTurnSign(BearingAngle, turnAngle.Value);
        }

        public void SmoothPatherTick()
        {
            if(!VehicleMod.mod.settings.debugDisableSmoothPathing && currentTravelCells.AnyNullified())
            {
                float angle = (float)Pawn.DrawPos.Angle180RotationTracker(currentTravelCells.First().ToVector3Shifted());

                if (Pawn.SmoothPos.ToIntVec3() != Pawn.Position)
                    Pawn.Position = Pawn.SmoothPos.ToIntVec3();
                if (!Pawn.Drafted)
                {
                    currentTravelCells.Clear();
                    return;
                }

                Vector3 test = SPTrig.ForwardStep(BearingAngle, ActualMoveSpeed / 60);
                Pawn.SmoothPos += test;

                if(Prefs.DevMode)
                {
                    GenDraw.DrawLineBetween(currentTravelCells.First().ToVector3Shifted(), Pawn.DrawPos, SimpleColor.Red);
                }

                if (Pawn.Position == currentTravelCells.First())
                    currentTravelCells.Pop();

                if(BearingAngle != angle)
                {
                    BearingAngle += turnSign * turnSpeed;
                    if (Math.Abs(BearingAngle - angle) < (0.5))
                        BearingAngle = angle;
                }
            }
        }

        //public override void CompTickRare()
        //{
        //    base.CompTickRare();
        //    if(!VehicleMod.mod.settings.debugDisableSmoothPathing && currentTravelCells.AnyNullified())
        //    {
        //        float angle = (float)Pawn.DrawPos.Angle180RotationTracker(currentTravelCells.First().ToVector3Shifted());
        //        CheckTurnSign(angle);
        //    }
        //    if (VehicleMod.mod.settings.debugDisableSmoothPathing)
        //        Pawn.SmoothPos = Pawn.DrawPos;
        //}

        public override void CompTick()
        {
            base.CompTick();
            //SmoothPatherTick();
            if (Pawn.IsHashIntervalTick(150))
                TrySatisfyPawnNeeds();

            foreach (VehicleHandler handler in handlers)
            {
                handler.ReservationHandler();
            }
        }

        private void InitializeVehicle()
        {
            if (handlers != null && handlers.Count > 0)
                return;
            if (currentTravelCells is null)
                currentTravelCells = new List<IntVec3>();
            if (cargoToLoad is null)
                cargoToLoad = new List<TransferableOneWay>();

            navigationCategory = Props.defaultNavigation;

            foreach(VehicleHandler handler in handlers)
            {
                if(handler.currentlyReserving is null) handler.currentlyReserving = new List<Pawn>();
            }
            if (!(Props.roles is null) && Props.roles.Count > 0)
            {
                foreach(VehicleRole role in Props.roles)
                {
                    handlers.Add(new VehicleHandler(Pawn, role));
                }
            }
        }

        private void InitializeStats()
        {
            armorPoints = Props.armor;
            cargoCapacity = Props.cargoCapacity;
            moveSpeedModifier = 0f;
            turnSpeed = Props.turnSpeed;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                InitializeVehicle();
                InitializeStats();
                Pawn.ageTracker.AgeBiologicalTicks = 0;
                Pawn.ageTracker.AgeChronologicalTicks = 0;
                Pawn.ageTracker.BirthAbsTicks = 0;
                Pawn.health.Reset();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref movementStatus, "movingStatus", VehicleMovementStatus.Online);
            Scribe_Values.Look(ref navigationCategory, "navigationCategory", NavigationCategory.Opportunistic);
            Scribe_Values.Look(ref currentlyFishing, "currentlyFishing", false);
            Scribe_Values.Look(ref bearingAngle, "bearingAngle");
            Scribe_Values.Look(ref turnSign, "turnSign");
            Scribe_Values.Look(ref turnSpeed, "turnSpeed");
            Scribe_Values.Look(ref showAllItemsOnMap, "showAllItemsOnMap");

            Scribe_Collections.Look(ref currentTravelCells, "currentTravelCells");
            Scribe_Collections.Look(ref cargoToLoad, "cargoToLoad");

            Scribe_Values.Look(ref armorPoints, "armorPoints");
            Scribe_Values.Look(ref cargoCapacity, "cargoCapacity");
            Scribe_Values.Look(ref moveSpeedModifier, "moveSpeed");

            Scribe_Collections.Look(ref handlers, "handlers", LookMode.Deep);
            Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);
        }
    }
}