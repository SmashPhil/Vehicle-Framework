using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public struct VehicleTaskData
{
  public required VehiclePawn vehicle;
  public required IVehicleTask task;
  public required IntVec3 destination;
  public required IntVec3 meetingSpot;
}

public sealed class LordJob_TravelWithVehicle : LordJob, IVehicleAssigner, IVehicleTaskLord
{
  private Pawn pawn;
  private VehiclePawn vehicle;
  private IntVec3 destination;
  private IntVec3 meetingSpot;
  private IVehicleTask task;

  private IntVec3 origin;
  private Rot8 rotation;
  

  [UsedWithReflection]
  public LordJob_TravelWithVehicle()
  {
  }

  public LordJob_TravelWithVehicle(Pawn pawn, in VehicleTaskData data)
  {
    this.pawn = pawn;
    vehicle = data.vehicle;
    destination = data.destination;
    task = data.task;
    meetingSpot = data.meetingSpot;
    origin = vehicle.Position;
    rotation = vehicle.FullRotation;
  }

  public override bool ShouldExistWithoutPawns => false;

  public override bool IsCaravanSendable => true;

  public IntVec3 MeetingSpot => meetingSpot;

  public IntVec3 Destination => destination;

  IVehicleTask IVehicleTaskLord.Task => task;

  private bool NoVehicle()
  {
    return vehicle is null || vehicle.lord?.LordJob != this;
  }

  private bool NoLord()
  {
    return pawn.lord?.LordJob != this || NoVehicle();
  }

  private bool ColonistDrafted()
  {
    return pawn.Drafted;
  }

  private bool ColonistNotInVehicle()
  {
    return pawn.GetVehicle() != vehicle;
  }

  public override void Cleanup()
  {
    vehicle.DisembarkAll();
    // Removing vehicle will immediately fail on all toils, so this is equivalent to 
    //lord.RemovePawn(vehicle);
  }

  public override StateGraph CreateGraph()
  {
    StateGraph graph = new();

    // Gather around vehicle
    LordToil_GatherAtVehicle gatherAtVehicle = new(meetingSpot);
    gatherAtVehicle.AddFailCondition(NoVehicle);
    gatherAtVehicle.AddFailCondition(ColonistDrafted);
    graph.AddToil(gatherAtVehicle);
    graph.StartingToil = gatherAtVehicle;

    // Goto vehicle
    LordToil_BoardVehicle boardVehicle = new(vehicle);
    boardVehicle.AddFailCondition(NoLord);
    boardVehicle.AddFailCondition(ColonistDrafted);
    graph.AddToil(boardVehicle);

    Transition toBoardToil = new(gatherAtVehicle, boardVehicle);
    toBoardToil.AddTrigger(new Trigger_Memo(MemoTrigger.PawnsOnboard));
    graph.AddTransition(toBoardToil);

    // Vehicle travels to destination
    LordToil_TravelToDestination travelToDest = new(vehicle, destination);
    travelToDest.AddFailCondition(NoLord);
    travelToDest.AddFailCondition(ColonistNotInVehicle);
    graph.AddToil(travelToDest);
    
    Transition toTravelToil = new(boardVehicle, travelToDest);
    toTravelToil.AddTrigger(new Trigger_Memo(MemoTrigger.PawnsOnboard));
    graph.AddTransition(toTravelToil);

    // Execute task
    LordToil_WorkTask workTask = new(task);
    workTask.AddFailCondition(NoLord);
    graph.AddToil(workTask);

    Transition toTaskToil = new(travelToDest, workTask);
    toTaskToil.AddTrigger(new Trigger_Memo(MemoTrigger.ArrivedAtDestination));
    graph.AddTransition(toTaskToil);

    // cleanup with task
    LordToil_BoardVehicle returnToVehicle = new(vehicle);
    returnToVehicle.AddFailCondition(NoLord);
    returnToVehicle.AddFailCondition(ColonistDrafted);
    graph.AddToil(returnToVehicle);

    Transition toReturnToil = new(workTask, returnToVehicle);
    toReturnToil.AddTrigger(new Trigger_Memo(MemoTrigger.TaskComplete));
    graph.AddTransition(toReturnToil);

    // travel back to origin
    LordToil_ParkVehicle parkVehicle = new(vehicle, origin);
    parkVehicle.AddFailCondition(NoLord);
    parkVehicle.AddFailCondition(ColonistNotInVehicle);
    graph.AddToil(parkVehicle);

    Transition toOriginToil = new(returnToVehicle, parkVehicle);
    toOriginToil.AddTrigger(new Trigger_Memo(MemoTrigger.PawnsOnboard));
    graph.AddTransition(toOriginToil);

    LordToil_End endLord = new();
    graph.AddToil(endLord);

    Transition toEndToil = new(parkVehicle, endLord);
    toEndToil.AddTrigger(new Trigger_Memo(MemoTrigger.ArrivedAtDestination));
    graph.AddTransition(toEndToil);

    return graph;
  }

  public override void ExposeData()
  {
    Scribe_References.Look(ref pawn, nameof(pawn));
    Scribe_References.Look(ref vehicle, nameof(vehicle));
    Scribe_Values.Look(ref destination, nameof(destination));
    Scribe_Values.Look(ref meetingSpot, nameof(meetingSpot));
    Scribe_Deep.Look(ref task, nameof(task));

    Scribe_Values.Look(ref origin, nameof(origin));
  }

  public AssignedSeat GetAssignedSeat(Pawn requester)
  {
    Assert.AreEqual(pawn, requester);
    return new AssignedSeat(requester, vehicle.GetNextAvailableHandler(requester, HandlingType.Movement));
  }
}
