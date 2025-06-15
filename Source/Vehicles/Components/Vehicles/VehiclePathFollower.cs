using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class VehiclePathFollower : IExposable
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
  protected VehiclePawn vehicle;

  private List<IntVec3> bumperCells;

  private bool moving;

  public IntVec3 nextCell;
  private IntVec3 lastCell;
  public IntVec3 lastPathedTargetPosition;
  private LocalTargetInfo destination;

  public float nextCellCostLeft;
  public float nextCellCostTotal = 1f;

  private int cellsUntilClamor;

  private int lastMovedTick = -999999;
  private int waitTicks;

  public VehiclePath curPath;

  private PathEndMode peMode;
  private Rot8 endRot = Rot8.Invalid;

  private CancellationTokenSource pathCancellationTokenSource = new();
  private bool shouldStopClipping;

  private static readonly HashSet<IntVec3> collisionCells = [];

  public VehiclePathFollower(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    bumperCells = [];
    // If vehicle is not NxN, it may clip buildings at destination.
    shouldStopClipping = vehicle.VehicleDef.size.x != vehicle.VehicleDef.size.z;

    // N cells away from vehicle's front
    LookAheadStartingIndex = Mathf.CeilToInt(vehicle.VehicleDef.Size.z / 2f);
    LookAheadDistance = MinCheckAheadNodes + LookAheadStartingIndex;
    CollisionsLookAheadStartingIndex = Mathf.CeilToInt(vehicle.VehicleDef.Size.z / 2f);
    CollisionsLookAheadDistance = CheckAheadNodesForCollisions + CollisionsLookAheadStartingIndex;
  }

  public int LookAheadDistance { get; private set; }

  public int LookAheadStartingIndex { get; private set; }

  public int CollisionsLookAheadDistance { get; private set; }

  public int CollisionsLookAheadStartingIndex { get; private set; }

  public PathRequestStatus RequestStatus { get; internal set; }

  public LocalTargetInfo Destination => destination;

  public bool Moving => moving;

  public bool Waiting => waitTicks > 0;

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

  public void SetEndRotation(Rot8 rot)
  {
    endRot = rot;
  }

  public void ExposeData()
  {
    Scribe_Values.Look(ref moving, nameof(moving));
    Scribe_Values.Look(ref nextCell, nameof(nextCell));
    Scribe_Values.Look(ref nextCellCostLeft, nameof(nextCellCostLeft));
    Scribe_Values.Look(ref nextCellCostTotal, nameof(nextCellCostTotal));
    Scribe_Values.Look(ref peMode, nameof(peMode));
    Scribe_Values.Look(ref cellsUntilClamor, nameof(cellsUntilClamor));
    Scribe_Values.Look(ref lastMovedTick, nameof(lastMovedTick), -999999);

    if (moving)
    {
      Scribe_TargetInfo.Look(ref destination, nameof(destination));
    }

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      vehicle.animator?.SetBool(PropertyIds.Moving, moving);
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

    if (!ignoreReachability &&
      !vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>()[vehicle.VehicleDef]
       .VehicleReachability.CanReachVehicle(vehicle.Position, dest, peMode,
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

    curPath?.Dispose();
    curPath = null;
    moving = true;
    vehicle.animator?.SetBool(PropertyIds.Moving, moving);
    vehicle.EventRegistry[VehicleEventDefOf.MoveStart].ExecuteEvents();
  }

  public void StopDead()
  {
    if (!vehicle.Spawned)
      return;

    if (curPath != null)
    {
      vehicle.EventRegistry[VehicleEventDefOf.MoveStop].ExecuteEvents();
      curPath.Dispose();
    }

    curPath = null;
    moving = false;
    vehicle.animator?.SetBool(PropertyIds.Moving, moving);
    nextCell = vehicle.Position;
  }

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

    if (VehicleMod.settings.debug.debugDrawBumpers)
    {
      GenDraw.DrawFieldEdges(bumperCells);
    }

    // Transition between cells
    lastMovedTick = Find.TickManager.TicksGame;
    if (nextCellCostLeft > 0f)
    {
      float costsToPayTick = CostToPayThisTick();
      nextCellCostLeft -= costsToPayTick;
      return;
    }

    // Attempt setup for next cell transition
    if (moving)
    {
      TryEnterNextPathCell();
    }
  }

  public void TryResumePathingAfterLoading()
  {
    if (moving)
    {
      // Paths resumed post-load can be assumed to already be reachable. RegionGrid at this point will
      // be suspended anyways so it is not possible to do a reachability check.
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
    if (curPath != null && (vehicle.Faction == Faction.OfPlayer || DebugViewSettings.drawPaths)
      && Find.Selector.IsSelected(vehicle))
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
    if (vehicle.jobs.curJob != null)
    {
      vehicle.jobs.curDriver.Notify_PatherArrived();
    }
  }

  public void PatherFailed()
  {
    if (RequestStatus == PathRequestStatus.Calculating)
      pathCancellationTokenSource.Cancel();

    StopDead();
    SetEndRotation(Rot8.Invalid);
    vehicle.jobs?.curDriver?.Notify_PatherFailed();
    RequestStatus = PathRequestStatus.None;
  }

  public void EngageBrakes()
  {
    vehicle.EventRegistry[VehicleEventDefOf.Braking].ExecuteEvents();
    PatherFailed();
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
      vehicle.CalculateAngle();
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
        RequestNewPath();
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
    vehicle.CalculateAngle();

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
      vehicle.LocationRestrictedBySize(nextCell, vehicle.FullRotation))
    {
      PatherFailed();
      return;
    }

    float num = CostToMoveIntoCell(vehicle, vehicle.Position, nextCell);
    nextCellCostTotal = num;
    nextCellCostLeft = num;

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

  private static void LocomotionTicks(VehiclePawn vehicle, IntVec3 from, IntVec3 to,
    ref float tickCost)
  {
    Pawn locomotionUrgencySameAs = vehicle.jobs.curDriver.locomotionUrgencySameAs;
    if (locomotionUrgencySameAs is VehiclePawn locomotionVehicle &&
      locomotionUrgencySameAs != vehicle && locomotionUrgencySameAs.Spawned)
    {
      float tickCostOtherVehicle = CostToMoveIntoCell(locomotionVehicle, from, to);
      tickCost =
        Mathf.Max(tickCost, tickCostOtherVehicle); //Slow down to match other vehicle's speed
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

  public static float CostToMoveIntoCell(VehiclePawn vehicle, IntVec3 from, IntVec3 to)
  {
    float tickCost = MoveTicksAt(vehicle, from, to);
    tickCost += vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>()[vehicle.VehicleDef]
     .VehiclePathGrid.PerceivedPathCostAt(to);
    // At minimum should take ~7.5 seconds per cell, any slower vehicle should be disabled
    tickCost = Mathf.Min(tickCost, MaxMoveTicks);
    if (vehicle.CurJob != null)
    {
      LocomotionTicks(vehicle, from, to, ref tickCost);
    }

    return Mathf.Max(tickCost, 1f);
  }

  private float CostToPayThisTick()
  {
    return Mathf.Max(1, nextCellCostTotal / MaxMoveTicks);
  }

  private VehiclePath FindPath(CancellationToken token)
  {
    lastPathedTargetPosition = destination.Cell;
    VehiclePath pawnPath = vehicle.Map.GetCachedMapComponent<VehiclePathingSystem>()[vehicle.VehicleDef]
     .VehiclePathFinder
     .FindPath(vehicle.Position, destination, vehicle, token, peMode: peMode);
    return pawnPath;
  }

  /// <summary>
  /// Calculates and assigns new path to <see cref="curPath"/>
  /// </summary>
  internal void GeneratePath(CancellationToken token)
  {
    VehiclePath pawnPath = FindPath(token);
    if (pawnPath is null || !pawnPath.Found)
    {
      PatherFailed();
      Messages.Message("VF_NoPathForVehicle".Translate(), MessageTypeDefOf.RejectInput, false);
      return;
    }
    if (curPath is not null)
    {
      VehiclePath oldPath = curPath;
      if (UnityData.IsInMainThread)
        oldPath.Dispose();
      else
        UnityThread.ExecuteOnMainThread(oldPath.Dispose);
    }
    curPath = pawnPath;
    RequestStatus = PathRequestStatus.None;
  }

  private void RequestNewPath()
  {
    if (pathCancellationTokenSource is null or { IsCancellationRequested: true })
      pathCancellationTokenSource = new CancellationTokenSource();

    RequestStatus = PathRequestStatus.Calculating;
    AsyncPathFindAction asyncAction = AsyncPool<AsyncPathFindAction>.Get();
    asyncAction.Set(vehicle, pathCancellationTokenSource.Token);
    TaskManager.RunAsync(asyncAction);
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

    if (curPath.UsedHeuristics && curPath.NodesConsumedCount >= 75)
    {
      return PathRequest.NeedNew;
    }

    if (lastPathedTargetPosition != destination.Cell)
    {
      float length = (vehicle.Position - destination.Cell).LengthHorizontalSquared;
      float minLengthForRecalc = length switch
      {
        > 900 => 10,
        > 289 => 5,
        > 100 => 3,
        > 49  => 2,
        _     => 0.5f
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

      // Should two vehicles be pathing into eachother directly, first to stop will be given a
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

    collisionCells.Clear();
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
        if (!cell.InBounds(vehicle.Map) || !collisionCells.Add(cell)) continue;

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

    collisionCells.Clear();
  }

  public enum PathRequest
  {
    None,
    Wait,
    Fail,
    NeedNew
  }

  public enum PathRequestStatus
  {
    None,
    Calculating,
    Failed,
  }
}