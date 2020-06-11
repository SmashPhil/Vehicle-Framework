using System;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles.Lords
{
    public class LordJob_FormAndSendVehicles : LordJob
    {
        public LordJob_FormAndSendVehicles()
        {
        }

        public LordJob_FormAndSendVehicles(List<TransferableOneWay> transferables, List<Pawn> vehicles, List<Pawn> pawns, List<Pawn> downedPawns, List<Pawn> prisoners, IntVec3 meetingPoint, IntVec3 exitPoint,
            int startingTile, int destinationTile)
        {
            this.transferables = transferables;
            this.vehicles = vehicles;
            this.downedPawns = downedPawns;
            this.meetingPoint = meetingPoint;
            this.exitPoint = exitPoint;
            this.startingTile = startingTile;
            this.destinationTile = destinationTile;
            this.pawns = pawns;
            this.prisoners = prisoners;
            forceCaravan = false;
        }

        public bool ForceCaravanLeave
        {
            get
            {
                return forceCaravan;
            }
            set
            {
                forceCaravan = value;
            }
        }

        public Pawn GetShipAssigned(Pawn p)
        {
            return this.vehicleAssigned.TryGetValue(p);
        }

        public void AssignSeats()
        {
            if(vehicleAssigned != null && vehicleAssigned.Count > 0 /*&& this.shipAssigned.Keys.Count == (this.sailors.Count + this.prisoners.Count)*/)
                return;
            List<Pawn> sailorsTmp = pawns;
            List<Pawn> prisonersTmp = prisoners;
            int numPreassign = 0;
            foreach(Pawn p in vehicles)
            {
                numPreassign = p.GetComp<CompVehicle>().PawnCountToOperate - p.GetComp<CompVehicle>().AllCrewAboard.Count;
                for(int i = 0; i < numPreassign; i++)
                {
                    vehicleAssigned.Add(sailorsTmp.Pop(), p);
                }
            }
            if(sailorsTmp.Count > 0)
            {
                int i = 0;
                int j = 0;
                while(sailorsTmp.Count > 0)
                {
                    Pawn p = pawns[i];
                    vehicleAssigned.Add(sailorsTmp.Pop(), p);
                    i = (i+2) > pawns.Count ? 0 : ++i;
                }
                while(prisonersTmp.Count > 0)
                {
                    Pawn p = pawns[j];
                    vehicleAssigned.Add(prisonersTmp.Pop(), p);
                    j = (j + 2) > pawns.Count ? 0 : ++j;
                }
            }
        }

        public bool GatherItemsNow
        {
            get
            {
                return this.lord.CurLordToil == this.gatherItems;
            }
        }

        public override bool NeverInRestraints
        {
            get
            {
                return true;
            }
        }

        public override bool AddFleeToil
        {
            get
            {
                return false;
            }
        }

        public string Status
        {
            get
            {
                LordToil curLordToil = this.lord.CurLordToil;
                if(curLordToil == this.gatherAnimals)
                {
                    return "FormingCaravanStatus_GatheringAnimals".Translate();
                }
                if(curLordToil == this.gatherAnimals_pause)
                {
                    return "FormingCaravanStatus_GatherAnimals_Pause".Translate();
                }
                if (curLordToil == this.gatherItems)
                {
                    return "FormingCaravanStatus_GatheringItems_Ship".Translate();
                }
                if (curLordToil == this.gatherItems_pause)
                {
                    return "FormingCaravanStatus_GatheringItems_Ship_Pause".Translate();
                }
                if (curLordToil == this.gatherSlaves)
                {
                    return "FormingCaravanStatus_GatheringSlaves_Ship".Translate();
                }
                if (curLordToil == this.gatherSlaves_pause)
                {
                    return "FormingCaravanStatus_GatheringSlaves_Ship_Pause".Translate();
                }
                if (curLordToil == this.gatherDownedPawns)
                {
                    return "FormingCaravanStatus_GatheringDownedPawns_Ship".Translate();
                }
                if (curLordToil == this.gatherDownedPawns_pause)
                {
                    return "FormingCaravanStatus_GatheringDownedPawns_Ship_Pause".Translate();
                }
                if(curLordToil == this.boardVehicle)
                {
                    return "FormingCaravanStatus_BoardShip".Translate();
                }
                if (curLordToil == this.boardVehicle_pause)
                {
                    return "FormingCaravanStatus_BoardShip_Pause".Translate();
                }
                if (curLordToil == this.leave)
                {
                    return "FormingCaravanStatus_Leaving_Ship".Translate();
                }
                if (curLordToil == this.leave_pause)
                {
                    return "FormingCaravanStatus_Leaving_Ship_Pause".Translate();
                }
                return "FormingCaravanStatus_Waiting".Translate();
            }
        }

        private Transition PauseTransition(LordToil from, LordToil to)
        {
            Transition transition = new Transition(from, to, false, true);
            transition.AddPreAction(new TransitionAction_Message("MessageCaravanFormationPaused".Translate(), MessageTypeDefOf.NegativeEvent, () => this.lord.ownedPawns.FirstOrDefault((Pawn x) => x.InMentalState), null, 1f));
            transition.AddTrigger(new Trigger_MentalState());
            transition.AddPostAction(new TransitionAction_EndAllJobs());
            return transition;
        }

        private Transition UnpauseTransition(LordToil from, LordToil to)
        {
            Transition transition = new Transition(from, to, false, true);
            transition.AddPreAction(new TransitionAction_Message("MessageCaravanFormationUnpaused".Translate(), MessageTypeDefOf.SilentInput, null, 1f));
            transition.AddTrigger(new Trigger_NoMentalState());
            transition.AddPostAction(new TransitionAction_EndAllJobs());
            return transition;
        }

        public override void Notify_PawnAdded(Pawn p)
        {
            base.Notify_PawnAdded(p);
            ReachabilityUtility.ClearCacheFor(p);
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            base.Notify_PawnLost(p, condition);
            ReachabilityUtility.ClearCacheFor(p);
            if (!this.caravanSent)
            {
                if (condition == PawnLostCondition.IncappedOrKilled && p.Downed)
                {
                    this.downedPawns.Add(p);
                }
                CaravanFormingUtility.RemovePawnFromCaravan(p, this.lord, false);
            }
        }

        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override void LordJobTick()
        {
            base.LordJobTick();
            for(int i = this.downedPawns.Count - 1; i >= 0; i--)
            {
                if (this.downedPawns[i].Destroyed)
                {
                    this.downedPawns.RemoveAt(i);
                }
                else if (!this.downedPawns[i].Downed)
                {
                    this.lord.AddPawn(this.downedPawns[i]);
                    this.downedPawns.RemoveAt(i);
                }
            }
            if(!lord.ownedPawns.Any(x => HelperMethods.IsVehicle(x)))
            {
                lord.lordManager.RemoveLord(lord);
                Messages.Message("BoatCaravanTerminatedNoBoats".Translate(), MessageTypeDefOf.NegativeEvent);
            }
        }

        public override string GetReport(Pawn pawn)
        {
            return "LordReportFormingCaravan".Translate();
        }

        private void SendCaravan()
        {
            this.caravanSent = true;
            CaravanFormingUtility.FormAndCreateCaravan(this.lord.ownedPawns.Concat(
                from x in this.downedPawns
                    where JobGiver_PrepareCaravan_GatherDownedPawns.IsDownedPawnNearExitPoint(x, this.exitPoint)
                    select x), this.lord.faction, base.Map.Tile, this.startingTile, this.destinationTile);
        }
        
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            gatherAnimals = new LordToil_PrepareCaravan_GatherAnimals(meetingPoint);
            stateGraph.AddToil(gatherAnimals);
            gatherAnimals_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(gatherAnimals_pause);
            gatherItems = new LordToil_PrepareCaravan_GatherShip(meetingPoint);
            stateGraph.AddToil(gatherItems);
            gatherItems_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(gatherItems_pause);
            gatherSlaves = new LordToil_PrepareCaravan_GatherSlavesShip(meetingPoint);
            stateGraph.AddToil(gatherSlaves);
            gatherSlaves_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(gatherSlaves_pause);
            gatherDownedPawns = new LordToil_PrepareCaravan_GatherDownedPawnsShip(meetingPoint, exitPoint);
            stateGraph.AddToil(gatherDownedPawns);
            gatherDownedPawns_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(gatherDownedPawns_pause);
            AssignSeats();
            boardVehicle = new LordToil_PrepareCaravan_BoardVehicles(exitPoint);
            stateGraph.AddToil(boardVehicle);
            boardVehicle_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(boardVehicle_pause);
            LordToil_PrepareCaravan_Wait lordToil_PrepareCaravan_Wait = new LordToil_PrepareCaravan_Wait(meetingPoint);
            stateGraph.AddToil(lordToil_PrepareCaravan_Wait);
            LordToil_PrepareCaravan_Pause lordToil_PrepareCaravan_Pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(lordToil_PrepareCaravan_Pause);
            leave = new LordToil_PrepareCaravan_LeaveShip(exitPoint);
            stateGraph.AddToil(leave);
            leave_pause = new LordToil_PrepareCaravan_Pause();
            stateGraph.AddToil(leave_pause);
            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            Transition transition = new Transition(gatherAnimals, gatherItems, false, true);
            transition.AddTrigger(new Trigger_Memo("AllAnimalsGathered"));
            stateGraph.AddTransition(transition, false);
            Transition transition2 = new Transition(gatherItems, gatherDownedPawns, false, true);
            transition2.AddTrigger(new Trigger_Memo("AllItemsGathered"));
            transition2.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transition2, false);
            Transition transition3 = new Transition(gatherDownedPawns, gatherSlaves, false, true);
            transition3.AddTrigger(new Trigger_Memo("AllDownedPawnsGathered"));
            stateGraph.AddTransition(transition3, false);
            Transition transition4 = new Transition(gatherSlaves, lordToil_PrepareCaravan_Wait, false, true);
            transition4.AddTrigger(new Trigger_Memo("AllSlavesGathered"));
            transition4.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transition4, false);
            Transition transition5 = new Transition(lordToil_PrepareCaravan_Wait, boardVehicle, false, true);
            transition5.AddTrigger(new Trigger_NoPawnsVeryTiredAndSleeping(0f));
            transition5.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(transition5, false);
            Transition transitionB = new Transition(this.boardVehicle, leave, false, true);
            transitionB.AddTrigger(new Trigger_Memo("AllPawnsOnboard"));
            transitionB.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transitionB, false);
            Transition transition6 = new Transition(this.leave, lordToil_End, false, true);
            transition6.AddTrigger(new Trigger_Memo("ReadyToExitMap"));
            transition6.AddPreAction(new TransitionAction_Custom(new Action(SendCaravan)));
            stateGraph.AddTransition(transition6, false);
            Transition transition7 = PauseTransition(gatherAnimals, gatherAnimals_pause);
            stateGraph.AddTransition(transition7, false);
            Transition transition8 = UnpauseTransition(gatherAnimals_pause, gatherAnimals);
            stateGraph.AddTransition(transition8, false);
            Transition transition9 = PauseTransition(gatherItems, gatherItems_pause);
            stateGraph.AddTransition(transition9, false);
            Transition transition10 = UnpauseTransition(gatherItems_pause, gatherItems);
            stateGraph.AddTransition(transition10, false);
            Transition transition11 = PauseTransition(gatherDownedPawns, gatherDownedPawns_pause);
            stateGraph.AddTransition(transition11, false);
            Transition transition12 = UnpauseTransition(gatherDownedPawns_pause, gatherDownedPawns);
            stateGraph.AddTransition(transition12, false);
            Transition transition13 = PauseTransition(gatherSlaves, gatherSlaves_pause);
            stateGraph.AddTransition(transition13, false);
            Transition transition14 = UnpauseTransition(gatherSlaves_pause, gatherSlaves);
            stateGraph.AddTransition(transition14, false);
            Transition transition15 = PauseTransition(boardVehicle, boardVehicle_pause);
            stateGraph.AddTransition(transition15, false);
            Transition transition16 = UnpauseTransition(boardVehicle_pause, boardVehicle);
            stateGraph.AddTransition(transition16, false);
            Transition transition17 = PauseTransition(leave, leave_pause);
            stateGraph.AddTransition(transition17, false);
            Transition transition18 = UnpauseTransition(leave_pause, leave);
            stateGraph.AddTransition(transition18, false);
            Transition transition19 = PauseTransition(lordToil_PrepareCaravan_Wait, lordToil_PrepareCaravan_Pause);
            stateGraph.AddTransition(transition19, false);
            Transition transition20 = UnpauseTransition(lordToil_PrepareCaravan_Pause, lordToil_PrepareCaravan_Wait);
            stateGraph.AddTransition(transition20, false);
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref transferables, "transferables", LookMode.Deep);
            Scribe_Collections.Look(ref downedPawns, "downedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref prisoners, "prisoners", LookMode.Reference);
            Scribe_Collections.Look(ref vehicles, "vehicles", LookMode.Reference);
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref meetingPoint, "meetingPoint", default, false);
            Scribe_Values.Look(ref exitPoint, "exitPoint", default, false);
            Scribe_Values.Look(ref startingTile, "startingTile", 0, false);
            Scribe_Values.Look(ref destinationTile, "destinationTile", 0, false);
            Scribe_Collections.Look(ref vehicleAssigned, "vehicleAssigned", LookMode.Reference, LookMode.Reference);
        }

        public List<TransferableOneWay> transferables = new List<TransferableOneWay>();

        public List<Pawn> downedPawns = new List<Pawn>();

        public List<Pawn> prisoners = new List<Pawn>();

        public List<Pawn> vehicles = new List<Pawn>();

        public List<Pawn> pawns = new List<Pawn>();

        protected Dictionary<Pawn, Pawn> vehicleAssigned = new Dictionary<Pawn, Pawn>();

        protected IntVec3 meetingPoint;

        protected IntVec3 exitPoint;

        protected int startingTile;

        protected int destinationTile;

        protected bool caravanSent;

        protected LordToil gatherAnimals;

        protected LordToil gatherAnimals_pause;

        protected LordToil gatherItems;

        protected LordToil gatherItems_pause;

        protected LordToil gatherSlaves;

        protected LordToil gatherSlaves_pause;

        protected LordToil gatherDownedPawns;

        protected LordToil gatherDownedPawns_pause;

        protected LordToil boardVehicle;

        protected LordToil boardVehicle_pause;

        protected LordToil leave;

        protected LordToil leave_pause;

        public const float CustomWakeThreshold = 0.5f;

        protected bool forceCaravan;
    }
}
