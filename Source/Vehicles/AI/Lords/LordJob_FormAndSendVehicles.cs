using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class LordJob_FormAndSendVehicles : LordJob
	{
		public const float CustomWakeThreshold = 0.5f;

		private static (LordToil toil, string memo) prevState;

		public List<TransferableOneWay> transferables = new List<TransferableOneWay>();
		public List<Pawn> downedPawns = new List<Pawn>();
		public List<Pawn> prisoners = new List<Pawn>();
		public List<VehiclePawn> vehicles = new List<VehiclePawn>();
		public List<Pawn> pawns = new List<Pawn>();
		protected Dictionary<Pawn, AssignedSeat> vehicleAssigned = new Dictionary<Pawn, AssignedSeat>();

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
		protected LordToil tieAnimals;
		protected LordToil tieAnimals_pause;
		protected LordToil boardVehicle;
		protected LordToil boardVehicle_pause;
		protected LordToil leave;
		protected LordToil leave_pause;
		protected bool requireAllSeated;

		private List<Pawn> tmpPawnAssignments = new List<Pawn>();
		private List<AssignedSeat> tmpVehicleHandlerAssignments = new List<AssignedSeat>();

		public LordJob_FormAndSendVehicles()
		{
		}

		public LordJob_FormAndSendVehicles(List<TransferableOneWay> transferables, List<VehiclePawn> vehicles, List<Pawn> pawns, List<Pawn> downedPawns, List<Pawn> prisoners, IntVec3 meetingPoint, IntVec3 exitPoint,
			int startingTile, int destinationTile, bool requireAllSeated = false)
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
			this.requireAllSeated = requireAllSeated;
			vehicleAssigned = new Dictionary<Pawn, AssignedSeat>(CaravanHelper.assignedSeats);
		}

		public (LordToil source, LordToil pause) GatherAnimals => (gatherAnimals, gatherAnimals_pause);
		public (LordToil source, LordToil pause) GatherItems => (gatherItems, gatherItems_pause);
		public (LordToil source, LordToil pause) GatherSlaves => (gatherSlaves, gatherSlaves_pause);
		public (LordToil source, LordToil pause) GatherDowned => (gatherDownedPawns, gatherDownedPawns_pause);
		public (LordToil source, LordToil pause) TieAnimals => (tieAnimals, tieAnimals_pause);
		public (LordToil source, LordToil pause) Board => (boardVehicle, boardVehicle_pause);
		public (LordToil source, LordToil pause) Leave => (leave, leave_pause);

		public VehiclePawn LeadVehicle
		{
			get
			{
				return vehicles.First(x => x is VehiclePawn && x.RaceProps.baseBodySize == vehicles.Max(y => y.RaceProps.baseBodySize));
			}
		}

		public bool GatherItemsNow
		{
			get
			{
				return lord.CurLordToil == gatherItems;
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
				LordToil curLordToil = lord.CurLordToil;
				if (curLordToil == gatherAnimals)
				{
					return "FormingCaravanStatus_GatheringAnimals".Translate();
				}
				if (curLordToil == gatherAnimals_pause)
				{
					return "FormingCaravanStatus_GatherAnimals_Pause".Translate();
				}
				if (curLordToil == gatherItems)
				{
					return "FormingCaravanStatus_GatheringItems".Translate();
				}
				if (curLordToil == gatherItems_pause)
				{
					return "FormingCaravanStatus_GatheringItems_Pause".Translate();
				}
				if (curLordToil == gatherSlaves)
				{
					return "FormingCaravanStatus_GatheringSlaves_Vehicles".Translate();
				}
				if (curLordToil == gatherSlaves_pause)
				{
					return "FormingCaravanStatus_GatheringSlaves_Vehicles_Pause".Translate();
				}
				if (curLordToil == gatherDownedPawns)
				{
					return "FormingCaravanStatus_GatheringDownedPawns".Translate();
				}
				if (curLordToil == gatherDownedPawns_pause)
				{
					return "FormingCaravanStatus_GatheringDownedPawns_Pause".Translate();
				}
				if (curLordToil == tieAnimals)
				{
					return "FormingCaravanStatus_RopingAnimals".Translate();
				}
				if (curLordToil == tieAnimals_pause)
				{
					return "FormingCaravanStatus_RopingAnimals_Pause".Translate();
				}
				if (curLordToil == boardVehicle)
				{
					return "FormingCaravanStatus_BoardVehicles".Translate();
				}
				if (curLordToil == boardVehicle_pause)
				{
					return "FormingCaravanStatus_BoardVehicles_Pause".Translate();
				}
				if (curLordToil == leave)
				{
					return "FormingCaravanStatus_Leaving".Translate();
				}
				if (curLordToil == leave_pause)
				{
					return "FormingCaravanStatus_Leaving_Pause".Translate();
				}
				return "FormingCaravanStatus_Waiting".Translate();
			}
		}

		public void ForceCaravanLeave()
		{
			lord.GotoToil(Board.source);
		}

		public (VehiclePawn vehicle, VehicleHandler handler) GetVehicleAssigned(Pawn p)
		{
			if (vehicleAssigned.ContainsKey(p))
			{
				return vehicleAssigned[p];
			}
			return (null, null);
		}

		public bool SeatAssigned(VehiclePawn vehicle, VehicleHandler handler)
		{
			foreach (var assignment in vehicleAssigned.Values)
			{
				if (assignment.vehicle == vehicle && assignment.handler == handler)
				{
					return true;
				}
			}
			return false;
		}

		public bool AssignSeat(Pawn pawn, VehiclePawn vehicle, VehicleHandler handler)
		{
			return vehicleAssigned.TryAdd(pawn, (vehicle, handler));
		}

		public bool AssignRemainingPawns()
		{
			if (requireAllSeated)
			{
				foreach (Pawn pawn in pawns.Where(p => !vehicleAssigned.ContainsKey(p)))
				{
					VehiclePawn nextAvailableVehicle = vehicles.FirstOrDefault(v => v.SeatsAvailable > 0);
					if (nextAvailableVehicle is null)
					{
						return false;
					}
					vehicleAssigned.Add(pawn, (nextAvailableVehicle, nextAvailableVehicle.NextAvailableHandler()));
				}
			}
			else
			{
				int nextVehicleIndex = 0;
				foreach (Pawn pawn in pawns.Where(p => !vehicleAssigned.ContainsKey(p)))
				{
					VehiclePawn nextAvailableVehicle = vehicles[nextVehicleIndex];
					vehicleAssigned.Add(pawn, (nextAvailableVehicle, null));
					nextVehicleIndex++;
					if (nextVehicleIndex >= vehicles.Count)
					{
						nextVehicleIndex = 0;
					}
				}
			}
			return true;
		}

		public bool AssignSeats(VehiclePawn vehicle)
		{
			int iterations = 0;
			while (vehicleAssigned.Where(k => k.Value.vehicle == vehicle).Select(p => p.Key).Count() < vehicle.PawnCountToOperateLeft)
			{
				if (iterations > 200)
				{
					return false;
				}

				Pawn nextToAssign = pawns.FirstOrDefault(x => !vehicleAssigned.ContainsKey(x));
				if (nextToAssign is null)
				{
					//Cannot finalize caravan
					return false;
				}
				
				vehicleAssigned.Add(nextToAssign, (vehicle, vehicle.NextAvailableHandler(HandlingTypeFlags.Movement)));

				iterations++;
			}
			return true;
		}

		public void ResolveSeatingAssignments()
		{
			foreach (VehiclePawn vehicle in vehicles)
			{
				if (vehicleAssigned.Where(k => k.Value.vehicle == vehicle).Select(p => p.Key).Count() < vehicle.PawnCountToOperateLeft)
				{
					if (!AssignSeats(vehicle))
					{
						Messages.Message("VehicleCaravanCanceled".Translate(), MessageTypeDefOf.NeutralEvent);
						CaravanFormingUtility.StopFormingCaravan(lord);
						return;
					}
				}
			}

			AssignRemainingPawns();
		}

		private Transition PauseTransition(LordToil from, LordToil to)
		{
			Transition transition = new Transition(from, to);
			transition.AddPreAction(new TransitionAction_Message("MessageCaravanFormationPaused".Translate(), MessageTypeDefOf.NegativeEvent, () => lord.ownedPawns.FirstOrDefault((Pawn x) => x.InMentalState), null, 1f));
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
			if (p is VehiclePawn vehicle)
			{
				VehicleReachabilityUtility.ClearCacheFor(vehicle);
			}
			else
			{
				ReachabilityUtility.ClearCacheFor(p);
			}
		}

		public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
		{
			base.Notify_PawnLost(pawn, condition);
			if (pawn is VehiclePawn vehicle)
			{
				VehicleReachabilityUtility.ClearCacheFor(vehicle);
			}
			else
			{
				ReachabilityUtility.ClearCacheFor(pawn);
			}
			if (!caravanSent)
			{
				if (condition == PawnLostCondition.Incapped && pawn.Downed)
				{
					downedPawns.Add(pawn);
				}
				VehicleCaravanFormingUtility.RemovePawnFromVehicleCaravan(pawn, lord, condition, false);
				lord.ReceiveMemo(MemoTrigger.RemovedPawn);
			}
		}

		public override bool CanOpenAnyDoor(Pawn p)
		{
			return true;
		}

		public override void LordJobTick()
		{
			base.LordJobTick();
			if (VehicleMod.settings.debug.debugDrawLordMeetingPoint && Find.TickManager.TicksGame % 10 == 0)
			{
				if (lord.CurLordToil is IDebugLordMeetingPoint debugLordMeetingPoint)
				{
					lord.Map.debugDrawer.FlashCell(debugLordMeetingPoint.MeetingPoint, colorPct: 0.95f, duration: 10);
				}
			}

			for (int i = downedPawns.Count - 1; i >= 0; i--)
			{
				if (downedPawns[i].Destroyed)
				{
					downedPawns.RemoveAt(i);
				}
				else if (!downedPawns[i].Downed)
				{
					lord.AddPawn(downedPawns[i]);
					downedPawns.RemoveAt(i);
				}
			}
			if (!lord.ownedPawns.NotNullAndAny(x => x is VehiclePawn))
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
			caravanSent = true;
			CaravanHelper.ExitMapAndCreateVehicleCaravan(lord.ownedPawns.Concat(downedPawns.Where(pawn => 
				JobGiver_PrepareCaravan_GatherDownedPawns.IsDownedPawnNearExitPoint(pawn, exitPoint))), lord.faction, Map.Tile, startingTile, destinationTile);
		}

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();

			ResolveSeatingAssignments();

			gatherAnimals = new LordToil_PrepareCaravan_GatherAnimalsForVehicles(meetingPoint);
			gatherAnimals_pause = new LordToil_PrepareCaravan_Pause();
			gatherItems = new LordToil_PrepareCaravan_GatherCargo(meetingPoint);
			gatherItems_pause = new LordToil_PrepareCaravan_Pause();
			gatherSlaves = new LordToil_PrepareCaravan_GatherSlavesVehicle(meetingPoint);
			gatherSlaves_pause = new LordToil_PrepareCaravan_Pause();
			gatherDownedPawns = new LordToil_PrepareCaravan_GatherDownedPawnsVehicle(meetingPoint);
			gatherDownedPawns_pause = new LordToil_PrepareCaravan_Pause();
			tieAnimals = new LordToil_PrepareCaravan_TieAnimalsToVehicle(meetingPoint);
			tieAnimals_pause = new LordToil_PrepareCaravan_Pause();
			boardVehicle = new LordToil_PrepareCaravan_BoardVehicles(exitPoint);
			boardVehicle_pause = new LordToil_PrepareCaravan_Pause();
			leave = new LordToil_PrepareCaravan_LeaveWithVehicles(exitPoint);
			leave_pause = new LordToil_PrepareCaravan_Pause();

			AddToStateGraph(stateGraph, GatherAnimals, MemoTrigger.AnimalsGathered, postActions: new TransitionAction[] { new TransitionAction_EndAllJobs() });
			//AddToStateGraph(stateGraph, TieAnimals, MemoTrigger.AnimalsTied, postActions: new TransitionAction[] { new TransitionAction_EndAllJobs() });
			AddToStateGraph(stateGraph, GatherItems, MemoTrigger.ItemsGathered, postActions: new TransitionAction[] { new TransitionAction_EndAllJobs() });
			AddToStateGraph(stateGraph, GatherDowned, MemoTrigger.DownedPawnsGathered);
			//AddToStateGraph(stateGraph, GatherSlaves, MemoTrigger.SlavesGathered);
			AddToStateGraph(stateGraph, Board, MemoTrigger.PawnsOnboard, preActions: new TransitionAction[] { new TransitionAction_EndAllJobs() }, postActions: new TransitionAction[] { new TransitionAction_EndAllJobs() });
			AddToStateGraph(stateGraph, Leave);

			LordToil_End lordToil_End = new LordToil_End();
			stateGraph.AddToil(lordToil_End);

			Transition leaveTransition = new Transition(Leave.source, lordToil_End);
			leaveTransition.AddTrigger(new Trigger_Memo(MemoTrigger.ExitMap));
			leaveTransition.AddPreAction(new TransitionAction_Custom(SendCaravan));

			stateGraph.AddTransition(leaveTransition);

			return stateGraph;
		}

		public void AddToStateGraph(StateGraph stateGraph, (LordToil source, LordToil pause) toil, string memo = null, TransitionAction[] preActions = null, TransitionAction[] postActions = null)
		{
			stateGraph.AddToil(toil.source);
			stateGraph.AddToil(toil.pause);

			if (prevState.toil != null)
			{
				Transition transition = new Transition(prevState.toil, toil.source);
				if (!prevState.memo.NullOrEmpty())
				{
					transition.AddTrigger(new Trigger_Memo(prevState.memo));
				}
				if (!preActions.NullOrEmpty())
				{
					foreach (TransitionAction action in preActions)
					{
						transition.AddPreAction(action);
					}
				}
				if (!postActions.NullOrEmpty())
				{
					foreach (TransitionAction action in postActions)
					{
						transition.AddPostAction(action);
					}
				}
				stateGraph.AddTransition(transition);
				Transition pauseTransition = PauseTransition(toil.source, toil.pause);
				Transition unpauseTransition = UnpauseTransition(toil.pause, toil.source);
				stateGraph.AddTransition(pauseTransition);
				stateGraph.AddTransition(unpauseTransition);
			}
			prevState = (toil.source, memo);
		}

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref transferables, nameof(transferables), LookMode.Deep);
			Scribe_Collections.Look(ref downedPawns, nameof(downedPawns), LookMode.Reference);
			Scribe_Collections.Look(ref prisoners, nameof(prisoners), LookMode.Reference);
			Scribe_Collections.Look(ref vehicles, nameof(vehicles), LookMode.Reference);
			Scribe_Collections.Look(ref pawns, nameof(pawns), LookMode.Reference);
			Scribe_Values.Look(ref meetingPoint, nameof(meetingPoint));
			Scribe_Values.Look(ref exitPoint, nameof(exitPoint));
			Scribe_Values.Look(ref startingTile, nameof(startingTile));
			Scribe_Values.Look(ref destinationTile, nameof(destinationTile));
			Scribe_Collections.Look(ref vehicleAssigned, nameof(vehicleAssigned), LookMode.Reference, LookMode.Deep, ref tmpPawnAssignments, ref tmpVehicleHandlerAssignments);
		}
	}

	public class AssignedSeat : IExposable
	{
		public VehiclePawn vehicle;
		public VehicleHandler handler;

		public AssignedSeat()
		{
		}

		public AssignedSeat(VehiclePawn vehicle, VehicleHandler handler)
		{
			this.vehicle = vehicle;
			this.handler = handler;
		}

		public static implicit operator ValueTuple<VehiclePawn, VehicleHandler>(AssignedSeat assignedSeat)
		{
			return (assignedSeat.vehicle, assignedSeat.handler);
		}

		public static implicit operator AssignedSeat(ValueTuple<VehiclePawn, VehicleHandler> tuple)
		{
			return new AssignedSeat(tuple.Item1, tuple.Item2);
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle));
			Scribe_References.Look(ref handler, nameof(handler));
		}
	}
}
