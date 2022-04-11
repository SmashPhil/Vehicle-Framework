using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles.Lords
{
	public class LordJob_FormAndSendVehicles : LordJob
	{
		public const float CustomWakeThreshold = 0.5f;

		public List<TransferableOneWay> transferables = new List<TransferableOneWay>();
		public List<Pawn> downedPawns = new List<Pawn>();
		public List<Pawn> prisoners = new List<Pawn>();
		public List<VehiclePawn> vehicles = new List<VehiclePawn>();
		public List<Pawn> pawns = new List<Pawn>();
		protected Dictionary<Pawn, (VehiclePawn vehicle, VehicleHandler handler)> vehicleAssigned = new Dictionary<Pawn, (VehiclePawn, VehicleHandler)>();
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
		protected bool forceCaravan;
		protected bool requireAllSeated;

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
			vehicleAssigned = new Dictionary<Pawn, (VehiclePawn, VehicleHandler)>(CaravanHelper.assignedSeats);
			forceCaravan = false;
		}

		public VehiclePawn LeadVehicle
		{
			get
			{
				return vehicles.First(x => x is VehiclePawn && x.RaceProps.baseBodySize == vehicles.Max(y => y.RaceProps.baseBodySize));
			}
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
					return "FormingCaravanStatus_GatheringItems_Ship".Translate();
				}
				if (curLordToil == gatherItems_pause)
				{
					return "FormingCaravanStatus_GatheringItems_Ship_Pause".Translate();
				}
				if (curLordToil == gatherSlaves)
				{
					return "FormingCaravanStatus_GatheringSlaves_Ship".Translate();
				}
				if (curLordToil == gatherSlaves_pause)
				{
					return "FormingCaravanStatus_GatheringSlaves_Ship_Pause".Translate();
				}
				if (curLordToil == gatherDownedPawns)
				{
					return "FormingCaravanStatus_GatheringDownedPawns_Ship".Translate();
				}
				if (curLordToil == gatherDownedPawns_pause)
				{
					return "FormingCaravanStatus_GatheringDownedPawns_Ship_Pause".Translate();
				}
				if (curLordToil == boardVehicle)
				{
					return "FormingCaravanStatus_BoardShip".Translate();
				}
				if (curLordToil == boardVehicle_pause)
				{
					return "FormingCaravanStatus_BoardShip_Pause".Translate();
				}
				if (curLordToil == leave)
				{
					return "FormingCaravanStatus_Leaving_Ship".Translate();
				}
				if (curLordToil == leave_pause)
				{
					return "FormingCaravanStatus_Leaving_Ship_Pause".Translate();
				}
				return "FormingCaravanStatus_Waiting".Translate();
			}
		}

		public (VehiclePawn vehicle, VehicleHandler handler) GetVehicleAssigned(Pawn p)
		{
			if (vehicleAssigned.ContainsKey(p))
			{
				return vehicleAssigned[p];
			}
			return (vehicles.FirstOrDefault(), null);
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
				
				vehicleAssigned.Add(nextToAssign, (vehicle, vehicle.NextAvailableHandler(h => h == HandlingTypeFlags.Movement)));

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
			Transition transition = new Transition(from, to, false, true);
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
			ReachabilityUtility.ClearCacheFor(p);
		}

		public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
		{
			base.Notify_PawnLost(p, condition);
			ReachabilityUtility.ClearCacheFor(p);
			if (!caravanSent)
			{
				if (condition == PawnLostCondition.IncappedOrKilled && p.Downed)
				{
					downedPawns.Add(p);
				}
				CaravanFormingUtility.RemovePawnFromCaravan(p, lord, false);
			}
		}

		public override bool CanOpenAnyDoor(Pawn p)
		{
			return true;
		}

		public override void LordJobTick()
		{
			base.LordJobTick();
			for(int i = downedPawns.Count - 1; i >= 0; i--)
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
			if(!lord.ownedPawns.NotNullAndAny(x => x is VehiclePawn))
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

			gatherAnimals = new LordToil_PrepareCaravan_GatherAnimals(meetingPoint);
			stateGraph.AddToil(gatherAnimals);
			gatherAnimals_pause = new LordToil_PrepareCaravan_Pause();
			stateGraph.AddToil(gatherAnimals_pause);

			gatherItems = new LordToil_PrepareCaravan_GatherCargo(meetingPoint);
			stateGraph.AddToil(gatherItems);
			gatherItems_pause = new LordToil_PrepareCaravan_Pause();
			stateGraph.AddToil(gatherItems_pause);

			//DISABLED BECAUSE VANILLA NO LONGER GATHERS SLAVES (Maybe I'll add unique mechanics in the future, idk we'll see)
			//gatherSlaves = new LordToil_PrepareCaravan_GatherSlavesVehicle(meetingPoint);
			//stateGraph.AddToil(gatherSlaves);
			//gatherSlaves_pause = new LordToil_PrepareCaravan_Pause();
			//stateGraph.AddToil(gatherSlaves_pause);

			//gatherDownedPawns = new LordToil_PrepareCaravan_GatherDownedPawnsVehicle(meetingPoint, exitPoint);
			//stateGraph.AddToil(gatherDownedPawns);
			//gatherDownedPawns_pause = new LordToil_PrepareCaravan_Pause();
			//stateGraph.AddToil(gatherDownedPawns_pause);

			ResolveSeatingAssignments();

			boardVehicle = new LordToil_PrepareCaravan_BoardVehicles(exitPoint);
			stateGraph.AddToil(boardVehicle);
			boardVehicle_pause = new LordToil_PrepareCaravan_Pause();
			stateGraph.AddToil(boardVehicle_pause);

			leave = new LordToil_PrepareCaravan_LeaveWithVehicles(exitPoint);
			stateGraph.AddToil(leave);
			leave_pause = new LordToil_PrepareCaravan_Pause();
			stateGraph.AddToil(leave_pause);
			LordToil_End lordToil_End = new LordToil_End();
			stateGraph.AddToil(lordToil_End);

			Transition transition1 = new Transition(gatherAnimals, gatherItems);
			transition1.AddTrigger(new Trigger_Memo("AllItemsGathered"));
			transition1.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transition1, false);

			Transition transition2 = new Transition(gatherItems, gatherDownedPawns);
			transition2.AddTrigger(new Trigger_Memo("AllItemsGathered"));
			transition2.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transition2, false);

			Transition transition3 = new Transition(gatherDownedPawns, boardVehicle); //gatherSlaves
			transition3.AddTrigger(new Trigger_Memo("AllDownedPawnsGathered"));
			stateGraph.AddTransition(transition3, false);

			Transition transition4 = new Transition(gatherSlaves, boardVehicle);
			transition4.AddTrigger(new Trigger_Memo("AllSlavesGathered"));
			transition4.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transition4, false);

			Transition transitionB = new Transition(boardVehicle, leave);
			transitionB.AddTrigger(new Trigger_Memo("AllPawnsOnboard"));
			transitionB.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transitionB, false);

			Transition transition6 = new Transition(leave, lordToil_End);
			transition6.AddTrigger(new Trigger_Memo("ReadyToExitMap"));
			transition6.AddPreAction(new TransitionAction_Custom(SendCaravan));
			stateGraph.AddTransition(transition6, false);

			Transition transition9 = PauseTransition(gatherItems, gatherItems_pause);
			stateGraph.AddTransition(transition9, false);

			Transition transition10 = UnpauseTransition(gatherItems_pause, gatherItems);
			stateGraph.AddTransition(transition10, false);

			Transition transition11 = PauseTransition(gatherDownedPawns, gatherDownedPawns_pause);
			stateGraph.AddTransition(transition11, false);

			Transition transition12 = UnpauseTransition(gatherDownedPawns_pause, gatherDownedPawns);
			stateGraph.AddTransition(transition12, false);

			//Transition transition13 = PauseTransition(gatherSlaves, gatherSlaves_pause);
			//stateGraph.AddTransition(transition13, false);

			//Transition transition14 = UnpauseTransition(gatherSlaves_pause, gatherSlaves);
			//stateGraph.AddTransition(transition14, false);

			Transition transition15 = PauseTransition(boardVehicle, boardVehicle_pause);
			stateGraph.AddTransition(transition15, false);

			Transition transition16 = UnpauseTransition(boardVehicle_pause, boardVehicle);
			stateGraph.AddTransition(transition16, false);

			Transition transition17 = PauseTransition(leave, leave_pause);
			stateGraph.AddTransition(transition17, false);

			Transition transition18 = UnpauseTransition(leave_pause, leave);
			stateGraph.AddTransition(transition18, false);

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
	}
}
