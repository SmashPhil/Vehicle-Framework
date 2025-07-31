using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public sealed class LordJob_FormAndSendVehicles : LordJob_FormAndSendCaravan
{
  private static readonly AccessTools.FieldRef<LordJob_FormAndSendCaravan, bool>
    CaravanSentFieldRef;

  private static (LordToil toil, string memo) prevState;

  public List<Pawn> prisoners = [];
  public List<VehiclePawn> vehicles = [];
  public List<Pawn> pawns = [];

  private Dictionary<Pawn, AssignedSeat> vehicleAssigned = [];

  private IntVec3 meetingPoint;
  private IntVec3 exitPoint;
  private PlanetTile startingTile;
  private PlanetTile destinationTile;
  private LordToil gatherAnimals;
  private LordToil gatherAnimalsPause;
  private LordToil gatherItems;
  private LordToil gatherItemsPause;
  private LordToil gatherSlaves;
  private LordToil gatherSlavesPause;
  private LordToil gatherDownedPawns;
  private LordToil gatherDownedPawnsPause;
  private LordToil tieAnimals;
  private LordToil tieAnimalsPause;
  private LordToil boardVehicle;
  private LordToil boardVehiclePause;
  private LordToil leave;
  private LordToil leavePause;

  // xml deserialization
  private List<Pawn> tmpPawnAssignments = [];
  private List<AssignedSeat> tmpVehicleHandlerAssignments = [];

  static LordJob_FormAndSendVehicles()
  {
    CaravanSentFieldRef =
      AccessTools.FieldRefAccess<bool>(typeof(LordJob_FormAndSendCaravan), "caravanSent");
  }

  /// <summary>
  /// Strictly for Xml Deserialization which requires a public default constructor.
  /// </summary>
  public LordJob_FormAndSendVehicles()
  {
  }

  public LordJob_FormAndSendVehicles(
    List<VehiclePawn> vehicles, List<Pawn> pawns,
    List<TransferableOneWay> transferables,
    IntVec3 meetingPoint, IntVec3 exitPoint, PlanetTile startingTile, PlanetTile destinationTile)
  {
    this.vehicles = vehicles;
    this.transferables = transferables;
    downedPawns = [];
    foreach (Pawn pawn in pawns)
    {
      if (pawn.Downed)
      {
        downedPawns.Add(pawn);
      }
      else if (!pawn.IsColonist && !pawn.RaceProps.Animal)
      {
        prisoners.Add(pawn);
      }
      else
      {
        this.pawns.Add(pawn);
      }
    }

    this.meetingPoint = meetingPoint;
    this.exitPoint = exitPoint;
    this.startingTile = startingTile;
    this.destinationTile = destinationTile;

    RequireAllSeated = this.vehicles.Exists(vehicle => vehicle.IsBoat());

    vehicleAssigned =
      new Dictionary<Pawn, AssignedSeat>(CaravanHelper.assignedSeats.AllAssignments);
  }

  public bool CaravanSent
  {
    get { return CaravanSentFieldRef.Invoke(this); }
    set { CaravanSentFieldRef.Invoke(this) = value; }
  }

  private (LordToil source, LordToil pause) GatherAnimals => (gatherAnimals, gatherAnimalsPause);
  private (LordToil source, LordToil pause) GatherItems => (gatherItems, gatherItemsPause);
  private (LordToil source, LordToil pause) GatherSlaves => (gatherSlaves, gatherSlavesPause);

  private (LordToil source, LordToil pause) GatherDowned =>
    (gatherDownedPawns, gatherDownedPawnsPause);

  private (LordToil source, LordToil pause) TieAnimals => (tieAnimals, tieAnimalsPause);
  private (LordToil source, LordToil pause) Board => (boardVehicle, boardVehiclePause);
  private (LordToil source, LordToil pause) Leave => (leave, leavePause);

  public bool RequireAllSeated { get; private set; }

  public VehiclePawn LeadVehicle { get; private set; }

  public bool GatherItemsNow
  {
    get { return lord.CurLordToil == gatherItems; }
  }

  public override bool NeverInRestraints
  {
    get { return true; }
  }

  public override bool AddFleeToil
  {
    get { return false; }
  }

  public void ForceCaravanLeave()
  {
    lord.GotoToil(Board.source);
  }

  public AssignedSeat GetVehicleAssigned(Pawn pawn)
  {
    return vehicleAssigned.TryGetValue(pawn);
  }

  public bool SeatAssigned(VehiclePawn vehicle, VehicleRoleHandler handler)
  {
    foreach (AssignedSeat assignment in vehicleAssigned.Values)
    {
      if (assignment.Vehicle == vehicle && assignment.handler == handler)
        return true;
    }
    return false;
  }

  public bool AssignSeat(Pawn pawn, VehiclePawn vehicle, VehicleRoleHandler handler)
  {
    return vehicleAssigned.TryAdd(pawn, new AssignedSeat(pawn, handler));
  }

  private void AssignRemainingPawns()
  {
    if (!RequireAllSeated)
      return;

    foreach (Pawn pawn in pawns)
    {
      if (vehicleAssigned.ContainsKey(pawn))
        continue;

      foreach (VehiclePawn vehicle in vehicles)
      {
        if (vehicle.SeatsAvailable <= 0)
          continue;

        vehicleAssigned[pawn] = new AssignedSeat(pawn, vehicle.GetAnyAvailableHandler());
      }
    }
  }

  /// <summary>
  /// Assigns pawns to a vehicle until there are no more pawns to fill or vehicle has no more free seats.
  /// </summary>
  /// <param name="vehicle">The vehicle to assign pawns to</param>
  /// <returns>True if assignment succeeds; otherwise, false</returns>
  private bool AssignSeats(VehiclePawn vehicle)
  {
    int countToAssign = vehicle.PawnCountToOperateLeft -
      vehicleAssigned.Values.CountWhere(seat => seat.Vehicle == vehicle);

    for (int i = 0; i < pawns.Count && i < countToAssign; i++, countToAssign++)
    {
      Pawn pawn = pawns[i];
      if (vehicleAssigned.ContainsKey(pawn))
        continue;

      VehicleRoleHandler handler = vehicle.GetNextAvailableHandler(pawn, HandlingType.Movement);
      Assert.IsNotNull(handler);
      vehicleAssigned.Add(pawn, new AssignedSeat(pawn, handler));
    }
    return true;
  }

  private void ResolveSeatingAssignments()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      int countToAssign = vehicle.PawnCountToOperateLeft -
        vehicleAssigned.Values.CountWhere(seat => seat.Vehicle == vehicle);
      if (countToAssign > 0 && !AssignSeats(vehicle))
      {
        Messages.Message("VehicleCaravanCanceled".Translate(), MessageTypeDefOf.NeutralEvent);
        CaravanFormingUtility.StopFormingCaravan(lord);
        return;
      }
    }
    AssignRemainingPawns();
  }

  private Transition PauseTransition(LordToil from, LordToil to)
  {
    Transition transition = new(from, to);
    transition.AddPreAction(new TransitionAction_Message(
      "MessageCaravanFormationPaused".Translate(), MessageTypeDefOf.NegativeEvent,
      () => lord.ownedPawns.FirstOrDefault(pawn => pawn.InMentalState)));
    transition.AddTrigger(new Trigger_MentalState());
    transition.AddPostAction(new TransitionAction_EndAllJobs());
    return transition;
  }

  private Transition UnpauseTransition(LordToil from, LordToil to)
  {
    Transition transition = new(from, to);
    transition.AddPreAction(new TransitionAction_Message(
      "MessageCaravanFormationUnpaused".Translate(), MessageTypeDefOf.SilentInput));
    transition.AddTrigger(new Trigger_NoMentalState());
    transition.AddPostAction(new TransitionAction_EndAllJobs());
    return transition;
  }

  private void DetermineLeadVehicle()
  {
    LeadVehicle = vehicles.MaxBy(vehicle => vehicle.VehicleDef.Size.Magnitude);
    if (LeadVehicle == null)
    {
      Messages.Message("VehicleCaravanCanceled".Translate(), MessageTypeDefOf.NeutralEvent);
      CaravanFormingUtility.StopFormingCaravan(lord);
    }
  }

  public override void Notify_PawnAdded(Pawn pawn)
  {
    base.Notify_PawnAdded(pawn);
    if (pawn is VehiclePawn vehicle)
    {
      VehicleReachabilityUtility.ClearCacheFor(vehicle);
    }
    else
    {
      ReachabilityUtility.ClearCacheFor(pawn);
    }
    DetermineLeadVehicle();
  }

  public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
  {
    if (pawn is VehiclePawn vehicle)
    {
      VehicleReachabilityUtility.ClearCacheFor(vehicle);
    }
    else
    {
      ReachabilityUtility.ClearCacheFor(pawn);
    }
    if (!CaravanSent)
    {
      if (condition == PawnLostCondition.Incapped ||
        condition == PawnLostCondition.Killed && pawn.Downed)
      {
        downedPawns.Add(pawn);
      }
      VehicleCaravanFormingUtility.RemovePawnFromVehicleCaravan(pawn, lord, condition, false);
      lord.ReceiveMemo(MemoTrigger.RemovedPawn);
    }
    DetermineLeadVehicle();
  }

  public override bool CanOpenAnyDoor(Pawn p)
  {
    return true;
  }

  public override void LordJobTick()
  {
    if (VehicleMod.settings.debug.debugDrawLordMeetingPoint &&
      Find.TickManager.TicksGame % 10 == 0)
    {
      if (lord.CurLordToil is IDebugLordMeetingPoint debugLordMeetingPoint)
      {
        lord.Map.debugDrawer.FlashCell(debugLordMeetingPoint.MeetingPoint, colorPct: 0.95f,
          duration: 10);
        lord.Map.debugDrawer.FlashLine(debugLordMeetingPoint.MeetingPoint, LeadVehicle.Position,
          duration: 10, color: SimpleColor.Magenta);
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
      Messages.Message("VF_CaravanTerminatedNoVehicles".Translate(),
        MessageTypeDefOf.NegativeEvent);
    }
  }

  public override string GetReport(Pawn pawn)
  {
    return "LordReportFormingCaravan".Translate();
  }

  private void SendCaravan()
  {
    CaravanSent = true;
    CaravanHelper.ExitMapAndCreateVehicleCaravan(lord.ownedPawns.Concat(downedPawns.Where(pawn =>
        JobGiver_PrepareCaravan_GatherDownedPawns.IsDownedPawnNearExitPoint(pawn, exitPoint))),
      lord.faction, Map.Tile, startingTile, destinationTile);
  }

  public override StateGraph CreateGraph()
  {
    StateGraph stateGraph = new();

    ResolveSeatingAssignments();

    gatherAnimals = new LordToil_PrepareCaravan_GatherAnimalsForVehicles(meetingPoint);
    gatherAnimalsPause = new LordToil_PrepareCaravan_Pause();
    gatherItems = new LordToil_PrepareCaravan_GatherCargo(meetingPoint);
    gatherItemsPause = new LordToil_PrepareCaravan_Pause();
    gatherSlaves = new LordToil_PrepareCaravan_GatherSlavesVehicle(meetingPoint);
    gatherSlavesPause = new LordToil_PrepareCaravan_Pause();
    gatherDownedPawns = new LordToil_PrepareCaravan_GatherDownedPawnsVehicle(meetingPoint);
    gatherDownedPawnsPause = new LordToil_PrepareCaravan_Pause();
    tieAnimals = new LordToil_PrepareCaravan_TieAnimalsToVehicle(meetingPoint);
    tieAnimalsPause = new LordToil_PrepareCaravan_Pause();
    boardVehicle = new LordToil_PrepareCaravan_BoardVehicles(exitPoint);
    boardVehiclePause = new LordToil_PrepareCaravan_Pause();
    leave = new LordToil_PrepareCaravan_LeaveWithVehicles(exitPoint);
    leavePause = new LordToil_PrepareCaravan_Pause();

    AddToStateGraph(stateGraph, GatherAnimals, MemoTrigger.AnimalsGathered,
      postActions: [new TransitionAction_EndAllJobs()]);
    //AddToStateGraph(stateGraph, TieAnimals, MemoTrigger.AnimalsTied, postActions: new TransitionAction[] { new TransitionAction_EndAllJobs() });
    AddToStateGraph(stateGraph, GatherItems, MemoTrigger.ItemsGathered,
      postActions: [new TransitionAction_EndAllJobs()]);
    AddToStateGraph(stateGraph, GatherDowned, MemoTrigger.DownedPawnsGathered);
    //AddToStateGraph(stateGraph, GatherSlaves, MemoTrigger.SlavesGathered);
    AddToStateGraph(stateGraph, Board, MemoTrigger.PawnsOnboard,
      preActions: [new TransitionAction_EndAllJobs()],
      postActions: [new TransitionAction_EndAllJobs()]);
    AddToStateGraph(stateGraph, Leave);

    LordToil_End lordToilEnd = new();
    stateGraph.AddToil(lordToilEnd);

    Transition leaveTransition = new(Leave.source, lordToilEnd);
    leaveTransition.AddTrigger(new Trigger_Memo(MemoTrigger.ExitMap));
    leaveTransition.AddPreAction(new TransitionAction_Custom(SendCaravan));

    stateGraph.AddTransition(leaveTransition);

    return stateGraph;
  }

  public void AddToStateGraph(StateGraph stateGraph, (LordToil source, LordToil pause) toil,
    string memo = null, TransitionAction[] preActions = null,
    TransitionAction[] postActions = null)
  {
    stateGraph.AddToil(toil.source);
    stateGraph.AddToil(toil.pause);

    if (prevState.toil != null)
    {
      Transition transition = new(prevState.toil, toil.source);
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

    Scribe_Collections.Look(ref vehicleAssigned, nameof(vehicleAssigned), LookMode.Reference,
      LookMode.Deep, ref tmpPawnAssignments, ref tmpVehicleHandlerAssignments);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
      RequireAllSeated = vehicles.Exists(vehicle => vehicle.IsBoat());
  }
}