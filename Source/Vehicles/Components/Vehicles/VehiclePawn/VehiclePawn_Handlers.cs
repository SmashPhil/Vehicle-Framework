using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public partial class VehiclePawn
{
  // Assigned seat for to boarding role
  private List<AssignedSeat> boardingAssignments = [];
  public List<VehicleRoleHandler> handlers = [];

  /* ----- Caches for VehicleHandlers ----- */

  public List<VehicleRoleHandler> OccupiedHandlers { get; private set; } = [];

  public List<Pawn> AllPawnsAboard { get; private set; } = [];
  public List<Pawn> AllColonistsAboard { get; private set; } = [];

  public Dictionary<HandlingType, List<Pawn>> PawnsByHandlingType { get; private set; } = new()
  {
    [HandlingType.None] = [],
    [HandlingType.Movement] = [],
    [HandlingType.Turret] = [],
  };

  /* -------------------------------------- */

  public int PawnCountToOperate
  {
    get
    {
      int pawnCount = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        if (handler.role.HandlingTypes.HasFlag(HandlingType.Movement))
        {
          pawnCount += handler.role.SlotsToOperate;
        }
      }
      return pawnCount;
    }
  }

  public int PawnCountToOperateLeft
  {
    get { return PawnCountToOperate - PawnsByHandlingType[HandlingType.Movement].Count; }
  }

  #pragma warning disable 618
  public bool HasEnoughOperators => CanMoveWithOperators;

  // TODO 1.7 - Rename to 'HasEnoughOperators'
  /// <summary>
  /// Vehicle handler requirements are satisfied
  /// </summary>
  [Obsolete("Use CanMoveWithOperators instead. Will be removed in 1.7")]
  public bool CanMoveWithOperators
  {
    get
    {
      if (!MovementPermissions.HasFlag(VehiclePermissions.Autonomous))
      {
        foreach (VehicleRoleHandler handler in handlers)
        {
          if (handler.role.HandlingTypes.HasFlag(HandlingType.Movement) &&
            !handler.RoleFulfilled)
          {
            return false;
          }
        }
      }
      return true;
    }
  }
  #pragma warning restore 618

  public List<Pawn> Passengers => PawnsByHandlingType[HandlingType.None];

  public List<Pawn> AllCapablePawns
  {
    get
    {
      List<Pawn> pawnsOnShip = new List<Pawn>();
      if (!(handlers is null) && handlers.Count > 0)
      {
        foreach (VehicleRoleHandler handler in handlers)
        {
          if (!(handler.thingOwner is null) && handler.thingOwner.Count > 0)
            pawnsOnShip.AddRange(handler.thingOwner);
        }
      }

      pawnsOnShip = pawnsOnShip
       .Where(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))?.ToList();
      return pawnsOnShip ?? new List<Pawn>() { };
    }
  }

  public int SeatsAvailable
  {
    get
    {
      int x = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        x += handler.role.Slots - handler.thingOwner.Count;
      }

      return x;
    }
  }

  public int TotalSeats
  {
    get
    {
      int x = 0;
      foreach (VehicleRoleHandler handler in handlers)
      {
        x += handler.role.Slots;
      }

      return x;
    }
  }

  public void RecachePawnCount()
  {
    PawnsByHandlingType.ClearValueLists();
    OccupiedHandlers.Clear();
    AllPawnsAboard.Clear();
    AllColonistsAboard.Clear();

    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.thingOwner.Any)
      {
        OccupiedHandlers.Add(handler);
        foreach (Pawn pawn in handler.thingOwner)
        {
          AllPawnsAboard.Add(pawn);
          if (pawn.IsColonist)
            AllColonistsAboard.Add(pawn);

          if (handler.role.HandlingTypes == HandlingType.None)
          {
            PawnsByHandlingType[HandlingType.None].Add(pawn);
          }
          else
          {
            TryAddToCache(pawn, handler.role.HandlingTypes, HandlingType.Movement,
              PawnsByHandlingType);
            TryAddToCache(pawn, handler.role.HandlingTypes, HandlingType.Turret,
              PawnsByHandlingType);
          }
        }
      }
    }
    return;

    static void TryAddToCache(Pawn pawn, HandlingType value, HandlingType mask,
      Dictionary<HandlingType, List<Pawn>> cache)
    {
      if (value.HasFlag(mask))
        cache[mask].Add(pawn);
    }
  }

  public void AddRole(VehicleRole role)
  {
    role.ResolveReferences(VehicleDef);
    handlers.Add(new VehicleRoleHandler(this, role));
    handlers.Sort();
    ResetRenderStatus();
  }

  public void RemoveRole(VehicleRole role)
  {
    // Temporary measure to avoid the destruction of all pawns within the role being removed
    DisembarkAll();
    for (int i = handlers.Count - 1; i >= 0; i--)
    {
      VehicleRoleHandler handler = handlers[i];
      if (handler.role.key == role.key)
      {
        DrawTracker.RemoveRenderer(handler);
        handlers.RemoveAt(i);
      }
    }
  }

  public void RemoveRole(string roleKey)
  {
    // Temporary measure to avoid the destruction of all pawns within the role being removed
    DisembarkAll();
    for (int i = handlers.Count - 1; i >= 0; i--)
    {
      VehicleRoleHandler handler = handlers[i];
      if (handler.role.key == roleKey)
      {
        DrawTracker.RemoveRenderer(handler);
        handlers.RemoveAt(i);
      }
    }
  }

  [Pure]
  public VehicleRoleHandler GetHandler(string roleKey)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.role.key == roleKey)
      {
        return handler;
      }
    }
    return null;
  }

  [Pure]
  public IEnumerable<VehicleRoleHandler> GetHandlers(HandlingType handlingTypeFlag)
  {
    if (handlingTypeFlag == HandlingType.None)
      return handlers.Where(handler => handler.role.HandlingTypes == HandlingType.None);
    return handlers.Where(handler => handler.role.HandlingTypes.HasFlag(handlingTypeFlag));
  }

  [Pure]
  public VehicleRoleHandler GetAnyAvailableHandler()
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (handler.AreSlotsAvailableAndReservable)
        return handler;
    }
    return null;
  }

  [Pure]
  public VehicleRoleHandler GetNextAvailableHandler(HandlingType handlingTypeFlag)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      // None has an explicit check for no handling types, otherwise HasFlag would
      // always be true. Use GetAnyAvailableHandler if HandlingType does not matter.
      if (handlingTypeFlag == HandlingType.None)
      {
        if (handler.role.HandlingTypes == HandlingType.None ||
          handler.AreSlotsAvailableAndReservable)
          return handler;
        continue;
      }
      if (handler.role.HandlingTypes.HasFlag(handlingTypeFlag) &&
        handler.AreSlotsAvailableAndReservable)
        return handler;
    }
    return null;
  }

  [Pure]
  public VehicleRoleHandler GetHighestPriorityAvailableHandler()
  {
    foreach (VehicleRoleHandler handler in handlers.OrderBy(handler => handler))
    {
      if (handler.AreSlotsAvailableAndReservable)
        return handler;
    }
    return null;
  }

  public void GiveLoadJob(Pawn pawn, VehicleRoleHandler handler)
  {
    if (boardingAssignments.Count > 0)
    {
      AssignedSeat seat = boardingAssignments.FirstOrDefault(assignment => assignment.pawn == pawn);
      if (seat is not null)
      {
        seat.handler = handler;
        return;
      }
    }
    boardingAssignments.Add(new AssignedSeat(pawn, handler));
  }

  /// <summary>
  /// Pawn with bill has boarded vehicle.
  /// </summary>
  /// <remarks>For boarding vehicles outside of the job system, use <see cref="TryAddPawn(Pawn)"/></remarks>
  /// <returns>Pawn successfully boarded the vehicle</returns>
  public bool BoardPawn(Pawn pawn)
  {
    if (boardingAssignments.Count > 0)
    {
      AssignedSeat seat = boardingAssignments.FirstOrDefault(assignment => assignment.pawn == pawn);
      if (seat is not null)
      {
        if (pawn.IsWorldPawn())
        {
          Log.Error("Tried boarding vehicle with world pawn. Use Notify_BoardedCaravan instead.");
          return false;
        }

        if (!TryAddPawn(pawn, seat.handler))
        {
          return false;
        }
        boardingAssignments.Remove(seat);
        return true;
      }
    }
    return false;
  }

  public bool TryAddPawn(Pawn pawn)
  {
    if (handlers.NullOrEmpty())
      return false;

    foreach (VehicleRoleHandler handler in handlers)
    {
      if (TryAddPawn(pawn, handler))
        return true;
    }
    return false;
  }

  public bool TryAddPawn(Pawn pawn, VehicleRoleHandler handler)
  {
    // Pawn can be boarded pre-spawned for events such as raids, in this case the map will be null
    // and no reservation checks are needed.
    VehicleReservationManager reservationManager = null;
    if (Spawned)
    {
      reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
      if (!reservationManager.ReservedBy<VehicleRoleHandler, VehicleHandlerReservation>(this, pawn,
          handler) && !handler.AreSlotsAvailable)
      {
        // If pawn attempts to board vehicle role which is already full, stop immediately
        return false;
      }
    }

    Assert.IsTrue(handlers.Contains(handler));
    if (!handler.AreSlotsAvailable)
      return false;

    if (pawn.Spawned)
      pawn.DeSpawn();

    bool result = true;
    if (!handler.thingOwner.TryAddOrTransfer(pawn, canMergeWithExistingStacks: false) &&
      pawn.holdingOwner != null)
    {
      // If we can't add to handler and currently has other owner, transfer or else the pawn
      // may get lost forever.
      result = pawn.holdingOwner.TryTransferToContainer(pawn, handler.thingOwner);
    }
    reservationManager?.ReleaseAllClaimedBy(pawn);

    if (result)
      EventRegistry?[VehicleEventDefOf.PawnEntered].ExecuteEvents();

    // NOTE - VehicleCaravans need to recache the pawn lists, this is especially crucial for ticking
    // behavior like caravan needs. This MUST occur after the PawnEntered event so the vehicle manifest
    // or AllPawnsListForReading is updated beforehand.
    if (this.GetVehicleCaravan() is { } caravan)
      caravan.RecacheVehicles();

    return result;
  }

  public void RemovePawn(Pawn pawn)
  {
    foreach (VehicleRoleHandler handler in handlers)
    {
      if (TryRemovePawn(pawn, handler))
        break;
    }
  }

  public bool TryRemovePawn(Pawn pawn, VehicleRoleHandler handler)
  {
    if (handler.thingOwner.Remove(pawn))
    {
      EventRegistry[VehicleEventDefOf.PawnRemoved].ExecuteEvents();

      // Same as TryAddPawn and DisembarkPawn, we need to notify caravans that the pawn is being
      // moved around so it can update its pawn and vehicle lists.
      if (this.GetVehicleCaravan() is { } caravan)
        caravan.RecacheVehicles();

      if (Spawned)
        Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawn);
      return true;
    }
    return false;
  }

  public void DisembarkPawn(Pawn pawn)
  {
    Assert.IsTrue(pawn.ParentHolder is VehicleRoleHandler);
    // In Caravan
    if (this.GetVehicleCaravan() is { } caravan)
    {
      RemovePawn(pawn);
      caravan.AddPawn(pawn, true);
      Assert.IsFalse(pawn.IsWorldPawn());
      Find.WorldPawns.PassToWorld(pawn);
      return;
    }

    Assert.IsTrue(Spawned,
      $"Trying to disembark pawn from unspawned vehicle that is not in a caravan. {pawn} would be lost forever.");
    // On Map
    if (!pawn.Spawned)
    {
      CellRect occupiedRect = this.OccupiedRect().ExpandedBy(1);
      IntVec3 loc = Position;
      if (occupiedRect.EdgeCells
       .Where(cell => cell.InBounds(Map) && cell.Standable(Map) &&
          !cell.GetThingList(Map).NotNullAndAny(thing => thing is Pawn))
       .TryRandomElement(out IntVec3 newLoc))
      {
        loc = newLoc;
      }

      GenSpawn.Spawn(pawn, loc, MapHeld);
      if (!loc.Standable(Map))
      {
        pawn.pather.TryRecoverFromUnwalkablePosition(false);
      }

      if (lord is not null)
      {
        pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
        lord.AddPawn(pawn);
      }
    }

    RemovePawn(pawn);
    EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
    if (!AllPawnsAboard.NotNullAndAny() && outOfFoodNotified)
    {
      outOfFoodNotified = false;
    }
  }

  public void DisembarkAll()
  {
    if (this.GetVehicleCaravan() is { } caravan)
    {
      foreach (VehicleRoleHandler handler in handlers)
      {
        for (int i = handler.thingOwner.Count; --i >= 0;)
        {
          handler.thingOwner.TryTransferToContainer(handler.thingOwner[i], caravan.pawns,
            canMergeWithExistingStacks: false);
          EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
        }
      }
    }
    else if (Spawned)
    {
      using (new EventDisabler<VehicleEventDef>(EventRegistry[VehicleEventDefOf.PawnExited]))
      {
        for (int i = AllPawnsAboard.Count - 1; i >= 0; i--)
        {
          DisembarkPawn(AllPawnsAboard[i]);
        }
      }
      EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
      Assert.IsTrue(AllPawnsAboard.Count == 0);
    }
    else
    {
      // Invalid operation but better to send the pawns to world and let the game decide how to
      // handle them
      Log.Warning("Disembarking from vehicle when it is not spawned or in a caravan.");
      foreach (VehicleRoleHandler handler in handlers)
      {
        for (int i = handler.thingOwner.Count; --i >= 0;)
        {
          Pawn pawn = handler.thingOwner[i];
          TryRemovePawn(pawn, handler);
          Find.WorldPawns.PassToWorld(pawn);
        }
      }
    }
  }

  internal void TickHandlers()
  {
    // Only need to tick VehicleHandlers with pawns inside them
    foreach (VehicleRoleHandler handler in OccupiedHandlers)
    {
      handler.DoTick();
    }
  }
}