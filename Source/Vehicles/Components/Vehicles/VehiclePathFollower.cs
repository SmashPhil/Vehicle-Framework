using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Burst;
using SmashTools.Performance;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public struct PathOrderData
{
  public int3 destination;
  public bool exitMapOnArrival;
  public Rot8 endRotation;
}

[PublicAPI]
public sealed class VehiclePathFollower : IExposable, IDisposable
{
  public const int MaxMoveTicks = 450;
  public const float SnowReductionFromWalking = 0.001f;
  public const int ClamorCellsInterval = 12;
  public const int MinCostWalk = 50;
  public const int MinCostAmble = 60;

  public const int MinCheckAheadNodes = 1;
  public const int MaxCheckAheadNodes = 5;
  public const int TicksWhileWaiting = 10;

  public const int CheckAheadNodesForCollisions = 3;
  public const int MaxCheckAheadNodesForCollisions = 8;

  private static readonly HashSet<IntVec3> CollisionCells = [];

  public readonly VehiclePawn vehicle;

  // Stats during transition
  private AccelerationController controller;

  private List<IntVec3> bumperCells;

  // TODO 1.7 - Fix access modifiers
  public IntVec3 nextCell;
  private IntVec3 lastCell;
  public IntVec3 lastPathedTargetPosition;
  private LocalTargetInfo destination;

  // TODO 1.7 - Fix access modifiers and opt for getter for Tween class
  public float nextCellCostLeft;
  public float nextCellCostTotal = 1f;

  private int waitTicks;

  private PathEndMode peMode;
  private Rot8 endRot = Rot8.Invalid;
  private bool shouldStopClipping;
  private PathingStatus status;

  private Queue<PathOrderData> pathQueue = [];
  private PathReceipt receipt;
  public Path curPath;

  public VehiclePathFollower(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    controller = new AccelerationController(this);

    bumperCells = [];
    // If vehicle is not NxN, it may clip buildings at destination.
    shouldStopClipping = vehicle.VehicleDef.size.x != vehicle.VehicleDef.size.z;

    // N cells away from vehicle's front
    LookAheadStartingIndex = Mathf.CeilToInt(vehicle.VehicleDef.Size.z / 2f);
    LookAheadDistance = MinCheckAheadNodes + LookAheadStartingIndex;
    CollisionsLookAheadStartingIndex = Mathf.CeilToInt(vehicle.VehicleDef.Size.z / 2f);
    CollisionsLookAheadDistance = CheckAheadNodesForCollisions + CollisionsLookAheadStartingIndex;
  }

  private int LookAheadDistance { get; set; }

  private int LookAheadStartingIndex { get; set; }

  private int CollisionsLookAheadDistance { get; set; }

  private int CollisionsLookAheadStartingIndex { get; set; }

  private VehiclePathingSystem PathingSystem { get; set; }

  private VehiclePathingSystem.VehiclePathData PathData { get; set; }

  // TODO 1.7 - Remove
  public LocalTargetInfo Destination => destination;

  internal float PathCostLeft { get; private set; }

  private bool Stopping => status == PathingStatus.Stopping;

  public bool Moving => status is PathingStatus.Moving or PathingStatus.Stopping;

  public bool Waiting => waitTicks > 0;

  public PathRequestStatus RequestStatus
  {
    get
    {
      if (pathQueue.Count > 0)
        return PathRequestStatus.Calculating;

      return field;
    }
    internal set
    {
      if (pathQueue.Count > 0 && value != PathRequestStatus.Calculating)
      {
        receipt?.Dispose();
        receipt = null;
        pathQueue.Clear();
      }
      field = value;
    }
  }

  // TODO - For Follow job, will need implementation when escorting is added
  public IntVec3 LastPassableCellInPath
  {
    get
    {
      if (!Moving || curPath == null)
      {
        return IntVec3.Invalid;
      }

      if (!Destination.Cell.Impassable(vehicle.Map))
      {
        return Destination.Cell;
      }

      foreach (IntVec3 cell in curPath.Nodes)
      {
        if (!cell.Impassable(vehicle.Map))
          return cell;
      }
      return !vehicle.Position.Impassable(vehicle.Map) ? vehicle.Position : IntVec3.Invalid;
    }
  }

  public void RecalculatePermissions()
  {
    if (Moving && (!vehicle.CanMoveFinal || !vehicle.Drafted))
    {
      PatherFailed();
    }
  }

  private void RecacheComponents()
  {
    PathingSystem = vehicle.Spawned ? vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>() : null;
    PathData = PathingSystem?[vehicle.VehicleDef];
  }

  public void PostGenerationSetup()
  {
    vehicle.AddEvent(VehicleEventDefOf.Spawned, RecacheComponents);
    vehicle.AddEvent(VehicleEventDefOf.Despawned, RecacheComponents);
    controller.RegisterEvents();
  }

  internal void PostLoad()
  {
    controller.RegisterEvents();
  }

  public void SetEndRotation(Rot8 rot)
  {
    endRot = rot;
  }

  void IExposable.ExposeData()
  {
    Scribe_Deep.Look(ref controller, nameof(controller), ctorArgs: this);
    Scribe_Values.Look(ref status, nameof(status));

    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      // TODO 1.7 - REMOVE
      bool moving = false;
      Scribe_Values.Look(ref moving, nameof(moving));
      if (moving) status = PathingStatus.Moving;
    }

    Scribe_Values.Look(ref nextCell, nameof(nextCell));
    Scribe_Values.Look(ref nextCellCostLeft, nameof(nextCellCostLeft));
    Scribe_Values.Look(ref nextCellCostTotal, nameof(nextCellCostTotal));
    Scribe_Values.Look(ref peMode, nameof(peMode));

    if (Moving)
    {
      Scribe_TargetInfo.Look(ref destination, nameof(destination));
    }

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      controller ??= new AccelerationController(this);
      vehicle.animator?.SetBool(PropertyIds.Moving, Moving);
      LongEventHandler.ExecuteWhenFinished(RecacheComponents);
    }
  }

  public void StartPath(LocalTargetInfo dest, PathEndMode peMode, bool ignoreReachability = false)
  {
    if (!vehicle.Drafted)
    {
      PatherFailed();
      return;
    }

    dest = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicle.VehicleDef, vehicle.Map,
      dest.ToTargetInfo(vehicle.Map), ref peMode);

    if (dest is { HasThing: true, ThingDestroyed: true })
    {
      Log.Error(vehicle + " pathing to destroyed thing " + dest.Thing);
      PatherFailed();
      return;
    }

    // TODO - Add Building and Position Recoverable extras
    if (!vehicle.Position.Walkable(vehicle.VehicleDef, vehicle.Map) &&
      !TryRecoverFromUnwalkablePosition(error: true))
    {
      PatherFailed();
      return;
    }

    if (Moving && curPath != null && destination == dest && this.peMode == peMode)
    {
      PatherFailed();
      return;
    }

    if (!ignoreReachability && !PathData.VehicleReachability.CanReachVehicle(vehicle.Position, dest, peMode,
          TraverseParms.For(TraverseMode.ByPawn)))
    {
      PatherFailed();
      return;
    }

    this.peMode = peMode;
    destination = dest;

    PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation =
      vehicle.Map.pawnDestinationReservationManager.MostRecentReservationFor(vehicle);
    if (pawnDestinationReservation is not null &&
      ((Destination.HasThing && pawnDestinationReservation.target != Destination.Cell) ||
        (pawnDestinationReservation.job != vehicle.CurJob &&
          pawnDestinationReservation.target != Destination.Cell)))
    {
      vehicle.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(vehicle);
    }

    if (AtDestinationPosition())
    {
      PatherArrived();
      return;
    }

    curPath = null;
    status = PathingStatus.Moving;
    vehicle.animator?.SetBool(PropertyIds.Moving, Moving);
    vehicle.EventRegistry[VehicleEventDefOf.MoveStart].ExecuteEvents();
  }

  public void StopDead()
  {
    if (!vehicle.Spawned)
      return;

    if (curPath != null)
    {
      vehicle.EventRegistry[VehicleEventDefOf.MoveStop].ExecuteEvents();
    }

    curPath = null;
    receipt?.Dispose();
    receipt = null;
    status = PathingStatus.Idle;
    pathQueue.Clear();
    vehicle.animator?.SetBool(PropertyIds.Moving, Moving);
    nextCell = vehicle.Position;
    RequestStatus = PathRequestStatus.None;
    SetEndRotation(Rot8.Invalid);
    ResetToCurrentPosition();
  }

  [Profile]
  public void PatherTick()
  {
    if ((!vehicle.Drafted || !vehicle.CanMoveFinal) && curPath != null)
    {
      PatherFailed();
      return;
    }

    if (vehicle.stances.stunner.Stunned)
    {
      return; // TODO - apply deceleration and effects
    }

    if (receipt is { IsCompleted: true })
    {
      ProcessPath();
    }
    // NOTE - curPath can be null for a couple frames if multiple paths are queued up while the
    // game is paused, where the first initial path hasn't had a chance to be requested yet.
    if (pathQueue.Count > 0 && curPath != null && receipt == null)
    {
      ProcessQueue();
    }

    if (VehicleMod.settings.debug.debugDrawBumpers)
    {
      GenDraw.DrawFieldEdges(bumperCells);
    }

    if (nextCellCostLeft <= 0f)
    {
      if (Moving)
      {
        TryEnterNextPathCell();
      }
    }
    else
    {
      controller.Tick();
      float costsToPayTick = CostToPayThisTick();
      PathCostLeft -= Mathf.Min(costsToPayTick, nextCellCostLeft);
      nextCellCostLeft -= costsToPayTick;
    }

    if (Stopping && controller.MoveSpeed <= AccelerationController.MinSpeed)
    {
      PatherFailed();
    }
  }

  public void OrderMoveTo(in PathOrderData data)
  {
    if (vehicle.CurJobDef == JobDefOf.Goto && KeyBindingDefOf.QueueOrder.IsDownEvent)
    {
      pathQueue.Enqueue(data);
    }
    else
    {
      Job job = new(JobDefOf.Goto, new LocalTargetInfo(data.destination))
      {
        exitMapOnArrival = data.exitMapOnArrival
      };
      if (vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc))
      {
        vehicle.vehiclePather.SetEndRotation(data.endRotation);
        FleckMaker.Static(data.destination, vehicle.Map, FleckDefOf.FeedbackGoto);
      }
    }
  }

  public void TryResumePathingAfterLoading()
  {
    if (Moving)
    {
      // Paths resumed post-load can be assumed to already be reachable. RegionGrid at this point will
      // be suspended anyway so it is not possible to do a reachability check.
      StartPath(destination, peMode, ignoreReachability: true);
    }
  }

  // Breaking name convention here to mimic RimWorld since VehiclePawn::Notify_Teleported hides
  // non-virtual parent method.
  public void Notify_Teleported()
  {
    StopDead();
    ResetToCurrentPosition();
  }

  public void ResetToCurrentPosition()
  {
    nextCell = vehicle.Position;
    nextCellCostLeft = 0f;
    nextCellCostTotal = 1f;
  }

  private void CalculatePathCosts()
  {
    PathCostLeft = vehicle.vehiclePather.CostToMoveIntoCell(vehicle.Position, curPath.LastNode);
    IReadOnlyList<int3> nodes = curPath.Nodes;
    for (int i = nodes.Count - 1; i > 0; i--)
    {
      int3 from = nodes[i];
      int3 to = nodes[i - 1];
      PathCostLeft += vehicle.vehiclePather.CostToMoveIntoCell(from, to);
    }
  }

  public Building BuildingBlockingNextPathCell()
  {
    Building edifice = nextCell.GetEdifice(vehicle.Map);
    if (edifice != null && edifice.BlocksPawn(vehicle))
    {
      return edifice;
    }

    return null;
  }

  private bool AtDestinationPosition()
  {
    return vehicle.CanReachImmediateVehicle(destination, peMode);
  }

  public void PatherDraw()
  {
    if (curPath == null)
      return;

    if (vehicle.Faction != Faction.OfPlayer || !DebugViewSettings.drawPaths || Stopping)
      return;

    if (Find.Selector.IsSelected(vehicle))
    {
      curPath.DrawPath(vehicle);
    }
  }

  public bool TryRecoverFromUnwalkablePosition(bool error = true)
  {
    bool recovered = false;
    foreach (IntVec3 radialOffset in GenRadial.RadialPattern)
    {
      IntVec3 nearestAvailableCell = vehicle.Position + radialOffset;
      if (!vehicle.Drivable(nearestAvailableCell))
      {
        if (nearestAvailableCell == vehicle.Position)
          return true;

        if (error)
        {
          Log.Warning(
            $"{vehicle} on impassable cell {vehicle.Position}. Teleporting to {nearestAvailableCell}");
        }

        vehicle.Position = nearestAvailableCell;
        vehicle.Notify_Teleported();
        recovered = true;
        break;
      }
    }

    if (!recovered)
    {
      Log.Error(
        $"{vehicle} on impassable cell {vehicle.Position}. Cound not find nearby position to teleport to.");
    }
    return recovered;
  }

  private void PatherArrived()
  {
    if (endRot.IsValid)
    {
      vehicle.FullRotation = endRot;
    }

    StopDead();
    vehicle.jobs.curDriver?.Notify_PatherArrived();
  }

  public void PatherFailed()
  {
    StopDead();
    vehicle.jobs.curDriver?.Notify_PatherFailed();
  }

  public void EngageBrakes()
  {
    if (curPath == null)
    {
      PatherFailed();
      return;
    }
    vehicle.EventRegistry[VehicleEventDefOf.Braking].ExecuteEvents();
    controller.DecelerateNow(ThrottleSpeed.Urgent);
    status = PathingStatus.Stopping;

    if (VehicleMod.settings.main.useHandBrakes)
    {
      const string WheelTag = "Wheel";
      float damageAmount = controller.NodesToDecelerate * vehicle.VehicleDef.properties.brakeDamagePerNode;
      foreach (VehicleComponent component in vehicle.statHandler.components)
      {
        if (component.props.tags != null && component.props.tags.Contains(WheelTag))
        {
          component.TakeDamage(vehicle, new DamageInfo(DamageDefOf.Scratch, damageAmount), ignoreArmor: true);
        }
      }
    }
  }

  private void SetBumperCells()
  {
    Rot8 direction = Ext_Map.DirectionToCell(vehicle.Position, nextCell);
    if (!direction.IsValid)
    {
      direction = vehicle.FullRotation;
    }

    CellRect bumperRect = direction.IsDiagonal ?
      vehicle.MinRectShifted(new IntVec2(0, 2), direction) :
      vehicle.OccupiedRectShifted(new IntVec2(0, 2), direction);

    bumperCells = [.. bumperRect];
  }

  private void TryEnterNextPathCell()
  {
    if (waitTicks > 0)
    {
      waitTicks--;
      return;
    }

    if (RequestStatus == PathRequestStatus.Calculating)
      return;

    if (vehicle.beached)
    {
      vehicle.BeachShip();
      // VehiclePawn::ReclaimPosition is called from set_Position patch
      vehicle.Position = nextCell;
      PatherFailed();
      return;
    }

    // TODO - add snow tracks / depressions
    //if (vehicle.BodySize > 0.9f)
    //{
    //  vehicle.Map.snowGrid.AddDepth(vehicle.Position, -SnowReductionFromWalking); 
    //}

    PathRequest pathRequest = NeedNewPath();
    switch (pathRequest)
    {
      case PathRequest.None:
        break;
      case PathRequest.Fail:
        PatherFailed();
        return;
      case PathRequest.Wait:
        waitTicks = TicksWhileWaiting;
        return;
      case PathRequest.NeedNew:
        RequestNewPathBurst();
        break;
      default:
        throw new NotImplementedException("TryEnterNextPathCell.PathRequest");
    }

    // Wait for path to be calculated
    if (curPath == null)
      return;

    if (VehicleMod.settings.main.runOverPawns)
    {
      float costsToPayThisTick = CostToPayThisTick();
      float moveSpeed = 1 / (nextCellCostTotal / 60 / costsToPayThisTick);
      if (vehicle.FullRotation.IsDiagonal)
      {
        moveSpeed *= Ext_Math.Sqrt2;
      }

      WarnPawnsImpendingCollision();
      vehicle.CheckForCollisions(moveSpeed);
    }

    UpdateVehiclePosition();

    if (AtDestinationPosition())
    {
      PatherArrived();
      return;
    }

    SetupMoveIntoNextCell();
  }

  private void UpdateVehiclePosition()
  {
    if (vehicle.Position == nextCell) return;

    CellRect hitboxBeforeMoving = vehicle.OccupiedRect();
    lastCell = vehicle.Position;
    vehicle.Position = nextCell;

    foreach (IntVec3 cell in hitboxBeforeMoving.AllCellsNoRepeat(vehicle.OccupiedRect()))
    {
      vehicle.Map.pathing.RecalculatePerceivedPathCostAt(cell);
    }
  }

  private void SetupMoveIntoNextCell()
  {
    if (curPath.NodesLeft <= 1)
    {
      Log.Error(
        $"{vehicle} at {vehicle.Position} ran out of path nodes while pathing to {destination}.");
      PatherFailed();
      return;
    }

    nextCell = curPath.ConsumeNextNode();
    if (nextCell == vehicle.Position)
    {
      // Assigning new path mid-travel may start from lastCell, skip 1 to avoid 1 tick skip
      // that slows down the vehicle and realigns to horizontal rotation.
      nextCell = curPath.ConsumeNextNode();
    }
    vehicle.CalculateAngle();

    if (!vehicle.DrivableFast(nextCell))
    {
      Log.Error($"{vehicle} entering {nextCell} which is impassable.");
      PatherFailed();
      return;
    }

    Rot4 nextRot = Ext_Map.DirectionToCell(vehicle.Position, nextCell);
    if (nextRot.IsValid && vehicle.PawnOccupiedCells(nextCell, nextRot)
     .Any(cell => !cell.InBounds(vehicle.Map)))
    {
      PatherFailed(); //Emergency fail if vehicle tries to go out of bounds
      return;
    }

    //Check ahead and stop prematurely if vehicle won't fit at final destination
    if (shouldStopClipping && curPath.NodesLeft < LookAheadStartingIndex &&
      vehicle.LocationRestrictedBySize(vehicle.Map, nextCell, vehicle.FullRotation))
    {
      PatherFailed();
      return;
    }

    float cost = CostToMoveIntoCell(vehicle.Position, nextCell);
    nextCellCostTotal = cost;
    nextCellCostLeft = cost;

    SetBumperCells();
  }

  public static float MoveTicksAt(VehiclePawn vehicle, IntVec3 from, IntVec3 to)
  {
    float tickCost;
    if (to.x == from.x || to.z == from.z)
    {
      tickCost = vehicle.TicksPerMoveCardinal;
    }
    else
    {
      tickCost = vehicle.TicksPerMoveDiagonal;
    }

    return tickCost;
  }

  private void LocomotionTicks(IntVec3 from, IntVec3 to,
    ref float tickCost)
  {
    Pawn locomotionUrgencySameAs = vehicle.jobs.curDriver.locomotionUrgencySameAs;
    if (locomotionUrgencySameAs is VehiclePawn locomotionVehicle && locomotionUrgencySameAs != vehicle &&
        locomotionUrgencySameAs.Spawned)
    {
      // Slow down to match other vehicle's speed
      float tickCostOtherVehicle = locomotionVehicle.vehiclePather.CostToMoveIntoCell(from, to);
      tickCost = Mathf.Max(tickCost, tickCostOtherVehicle);
    }
    else
    {
      switch (vehicle.jobs.curJob.locomotionUrgency)
      {
        case LocomotionUrgency.Amble:
          tickCost *= 3;
          if (tickCost < MinCostAmble)
          {
            tickCost = MinCostAmble;
          }

          break;
        case LocomotionUrgency.Walk:
          tickCost *= 2;
          if (tickCost < MinCostWalk)
          {
            tickCost = MinCostWalk;
          }

          break;
        case LocomotionUrgency.Jog:
          break;
        case LocomotionUrgency.Sprint:
          tickCost = Mathf.RoundToInt(tickCost * 0.75f);
          break;
      }
    }
  }

  internal float CostToMoveIntoCell(IntVec3 from, IntVec3 to)
  {
    float tickCost = MoveTicksAt(vehicle, from, to);
    tickCost += PathData.VehiclePathGrid.PerceivedPathCostAt(to);
    // At minimum should take ~7.5 seconds per cell, any slower and the vehicle should be disabled
    tickCost = Mathf.Min(tickCost, MaxMoveTicks);
    if (vehicle.CurJob != null)
    {
      LocomotionTicks(from, to, ref tickCost);
    }
    return Mathf.Max(tickCost, 1f);
  }

  private float CostToPayThisTick()
  {
    if (nextCellCostTotal >= MaxMoveTicks)
      return nextCellCostTotal / MaxMoveTicks * controller.MoveSpeedPct;

    return controller.MoveSpeedPct;
  }

  private void ProcessPath()
  {
    Path path = receipt.ClaimPath();
    if (path == null)
    {
      Log.Error($"Unable to find path from {receipt.PathRequest.start} to {receipt.PathRequest.end}");
      receipt.Dispose();
      receipt = null;
      return;
    }
    if (curPath == null)
    {
      lastPathedTargetPosition = destination.Cell;
      curPath = path;
      CalculatePathCosts();
    }
    else
    {
      PathOrderData data = pathQueue.Dequeue();
      Assert.AreEqual(data.destination, path.LastNode);
      Assert.AreEqual(JobDefOf.Goto, vehicle.jobs.curJob.def);
      curPath.Combine(path);
      CalculatePathCosts();
      vehicle.jobs.curJob?.exitMapOnArrival = data.exitMapOnArrival;
      destination = new LocalTargetInfo(data.destination);
      lastPathedTargetPosition = destination.Cell;
    }
    controller.Accelerate(curPath, accelThrottle: ThrottleSpeed.Normal, decelThrottle: ThrottleSpeed.Fast);

    RequestStatus = curPath != null ? PathRequestStatus.None : PathRequestStatus.Failed;
    if (curPath == null)
    {
      PatherFailed();
    }
    else
    {
      receipt?.Dispose();
      receipt = null;
    }
  }

  private void ProcessQueue()
  {
    PathOrderData data = pathQueue.Peek();
    int3 start = curPath.LastNode;
    int rotation = curPath.NodesLeft >= 2
      ? Rot8.DirectionFromCells(curPath.Nodes[1], curPath.Nodes[0]).AsInt
      : vehicle.FullRotation.AsInt;
    receipt = RequestPathTo(start, data.destination, rotation);
  }

  private void RequestNewPathBurst()
  {
    receipt = RequestPathTo(vehicle.Position, destination.Cell, Rotation.ToOctant(vehicle.FullRotation.AsInt));
  }

  private PathReceipt RequestPathTo(IntVec3 start, IntVec3 end, int rotation)
  {
    return PathData.PathFinder.RequestPath(new SmashTools.Burst.PathRequest
    {
      start = start,
      end = end,
      rotation = rotation,
      smoothen = true
    });
  }

  private PathRequest NeedNewPath()
  {
    // Delay till not calculating path
    if (RequestStatus == PathRequestStatus.Calculating)
      return PathRequest.None;

    if (!destination.IsValid || curPath is null || !curPath.Found || curPath.NodesLeft == 0)
      return PathRequest.NeedNew;

    if (destination.HasThing && destination.Thing.Map != vehicle.Map)
      return PathRequest.NeedNew;

    foreach (IntVec3 cell in vehicle.VehicleRect(destination.Cell, Rot4.North,
      maxSizePossible: true))
    {
      VehiclePawn otherVehicle = PathingHelper.AnyVehicleBlockingPathAt(cell, vehicle);
      if (otherVehicle is null)
        continue;
      if (otherVehicle.vehiclePather.Moving || otherVehicle.vehiclePather.Waiting)
        continue;

      if (PathingHelper.TryFindNearestStandableCell(vehicle, destination.Cell, out IntVec3 result))
      {
        destination = result;
        return PathRequest.NeedNew;
      }
      return PathRequest.None;
    }

    if (vehicle.Position.InHorDistOf(curPath.LastNode, 15f) ||
      vehicle.Position.InHorDistOf(destination.Cell, 15f))
    {
      if (!VehicleReachabilityImmediate.CanReachImmediateVehicle(curPath.LastNode, destination,
        vehicle.Map, vehicle.VehicleDef, peMode))
      {
        return PathRequest.NeedNew;
      }
    }

    if (lastPathedTargetPosition != destination.Cell)
    {
      float length = (vehicle.Position - destination.Cell).LengthHorizontalSquared;
      float minLengthForRecalc = length switch
      {
        > 900 => 10,
        > 289 => 5,
        > 100 => 3,
        > 49 => 2,
        _ => 0.5f
      };
      if ((lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared >
        minLengthForRecalc * minLengthForRecalc)
      {
        return PathRequest.NeedNew;
      }
    }

    IntVec3 previous = IntVec3.Invalid;
    int nodeIndex = LookAheadStartingIndex;
    while (nodeIndex < LookAheadStartingIndex + MaxCheckAheadNodes &&
      nodeIndex < curPath.NodesLeft)
    {
      IntVec3 next = curPath.Peek(nodeIndex);
      Rot8 rot = Ext_Map.DirectionToCell(previous, next);
      if (!next.Walkable(vehicle.VehicleDef, vehicle.Map))
      {
        return PathRequest.NeedNew;
      }

      // Should two vehicles be pathing into each other directly, first to stop will be given a
      // Wait request while the other will request a new path
      CellRect vehicleRect = vehicle.VehicleRect(next, rot);
      foreach (IntVec3 cell in vehicleRect)
      {
        if (PathingHelper.AnyVehicleBlockingPathAt(cell, vehicle) is { } otherVehicle)
        {
          if (otherVehicle.vehiclePather.Moving && !otherVehicle.vehiclePather.Waiting)
          {
            return PathRequest.Wait;
          }

          return PathRequest.NeedNew;
        }
      }

      previous = next;
      nodeIndex++;
    }

    return PathRequest.None;
  }

  private void WarnPawnsImpendingCollision()
  {
    if (curPath == null)
      return;

    using ClearOnDispose<IntVec3> cod = new(CollisionCells);
    IntVec3 previous = IntVec3.Invalid;
    int nodeIndex = CollisionsLookAheadStartingIndex;
    while (nodeIndex < CollisionsLookAheadStartingIndex + MaxCheckAheadNodesForCollisions &&
      nodeIndex < curPath.NodesLeft)
    {
      IntVec3 next = curPath.Peek(nodeIndex);
      Rot8 rot = Ext_Map.DirectionToCell(previous, next);

      CellRect vehicleRect = vehicle.VehicleRect(next, rot).ExpandedBy(1);
      foreach (IntVec3 cell in vehicleRect)
      {
        if (!cell.InBounds(vehicle.Map) || !CollisionCells.Add(cell)) continue;

        List<Thing> thingList = cell.GetThingList(vehicle.Map);
        // Reverse iterate in case a thing or pawn is destroyed from being run over
        for (int i = thingList.Count - 1; i >= 0; i--)
        {
          Thing thing = thingList[i];
          if (thing is not Pawn pawn)
            continue;

          Room room = RegionAndRoomQuery.RoomAt(cell, vehicle.Map, RegionType.Set_Passable);
          Room pawnRoom = pawn.GetRoom(RegionType.Set_Passable);
          if (pawnRoom == null || pawnRoom.CellCount == 1 || (room == pawnRoom
            && GenSight.LineOfSight(vehicle.Position, pawn.Position, vehicle.Map)))
          {
            pawn.Notify_DangerousVehiclePath(vehicle);
          }
        }
      }

      previous = next;
      nodeIndex++;
    }
  }

  public void Dispose()
  {
    receipt?.Dispose();
  }

  internal enum PathingStatus
  {
    Idle,
    Moving,
    Stopping
  }

  // TODO 1.7 - update access modifier
  public enum PathRequest
  {
    None,
    Wait,
    Fail,
    NeedNew
  }

  // TODO 1.7 - update access modifier
  public enum PathRequestStatus
  {
    None,
    Calculating,
    Failed,
  }
}