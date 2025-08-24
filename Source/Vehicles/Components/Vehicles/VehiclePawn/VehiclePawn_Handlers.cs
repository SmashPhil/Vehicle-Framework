using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public partial class VehiclePawn
{
	// Assigned seat for to boarding role
	private List<AssignedSeat> boardingAssignments = [];

	// TODO 1.7 - Chance access modifier
	public List<VehicleRoleHandler> handlers = [];

	/* ----- Caches for VehicleHandlers ----- */

	public List<VehicleRoleHandler> Handlers => handlers;

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

	// TODO 1.7 - Rename to 'HasEnoughOperators'
	[Obsolete("Use HasEnoughOperators instead. Will be removed in 1.7")]
	public bool CanMoveWithOperators => HasEnoughOperators;

	/// <summary>
	/// Vehicle handler requirements are satisfied
	/// </summary>
	public bool HasEnoughOperators
	{
		get
		{
			if ((MovementPermissions & VehiclePermissions.Autonomous) != 0)
				return true;
			if (VehicleMod.settings.debug.debugDraftAnyVehicle)
				return true;

			foreach (VehicleRoleHandler handler in handlers)
			{
				if ((handler.role.HandlingTypes & HandlingType.Movement) != 0 && !handler.RoleFulfilled)
					return false;
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
			// TODO - can be cached
			List<Pawn> pawnsOnShip = [];
			if (handlers is { Count: > 0 })
			{
				foreach (VehicleRoleHandler handler in handlers)
				{
					foreach (Pawn pawn in handler.thingOwner)
					{
						if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
							pawnsOnShip.Add(pawn);
					}
				}
			}
			return pawnsOnShip;
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
		return handlers.Where(handler => (handler.role.HandlingTypes & handlingTypeFlag) == handlingTypeFlag);
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

	[Pure] // TODO 1.7 - Remove, pawn is required for permissions check
	[Obsolete("Use overload with pawn for permissions check.")]
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
			if ((handler.role.HandlingTypes & handlingTypeFlag) == handlingTypeFlag &&
				handler.AreSlotsAvailableAndReservable)
				return handler;
		}
		return null;
	}

	[Pure]
	public VehicleRoleHandler GetNextAvailableHandler(Pawn pawn, HandlingType handlingTypeFlag)
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

			if (handler.CanOperateRole(pawn) && (handler.role.HandlingTypes & handlingTypeFlag) == handlingTypeFlag &&
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
		if (pawn.ShouldAlwaysTransferToVehiclesCargo())
		{
			AddOrTransfer(pawn);
			return true;
		}

		if (handlers.NullOrEmpty())
			return false;

		foreach (VehicleRoleHandler handler in handlers)
		{
			if (handler.role.HandlingTypes != HandlingType.None && !handler.CanOperateRole(pawn))
				continue;

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

		// Vehicle saves and ticks boarded pawns, no need for world pawns
		if (pawn.IsWorldPawn())
			Find.WorldPawns.RemovePawn(pawn);

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
			EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();

		// NOTE - VehicleCaravans need to recache the pawn lists, this is especially crucial for ticking
		// behavior like caravan needs. This MUST occur after the PawnEntered event so the vehicle manifest
		// or AllPawnsListForReading is updated beforehand.
		if (this.GetVehicleCaravan() is { } caravan)
			caravan.RecacheVehicles();

		return result;
	}

	public bool RemovePawn(Pawn pawn)
	{
		foreach (VehicleRoleHandler handler in handlers)
		{
			if (TryRemovePawn(pawn, handler))
				return true;
		}
		return inventory.innerContainer.Remove(pawn);
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
		Assert.IsTrue(pawn.InVehicle());
		// In Caravan
		if (this.GetVehicleCaravan() is { } caravan)
		{
			RemovePawn(pawn);
			caravan.AddPawn(pawn, true);
			Find.WorldPawns.PassToWorld(pawn);
			return;
		}

		Assert.IsTrue(Spawned,
			$"Trying to disembark pawn from unspawned vehicle that is not in a caravan. {pawn} will be lost forever.");

		if (RemovePawn(pawn))
		{
			this.SpawnPawnNearVehicle(pawn);
			EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
		}
	}

	/// <summary>
	/// Disembark all pawns from the vehicle's inventory. Certain pawns are loaded into cargo rather than
	/// taking up valuable seats inside the vehicle that colonists would otherwise occupy.
	/// </summary>
	public void DisembarkAllFromInventory()
	{
		if (this.GetVehicleCaravan() is { } caravan)
		{
			for (int i = inventory.innerContainer.Count - 1; i >= 0; i--)
			{
				if (inventory.innerContainer[i] is Pawn pawn)
				{
					inventory.innerContainer.RemoveAt(i);
					caravan.AddPawn(pawn, true);
					Find.WorldPawns.PassToWorld(pawn);
					EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
				}
			}
		}
		else if (Spawned)
		{
			using (new EventDisabler<VehicleEventDef>(EventRegistry[VehicleEventDefOf.PawnExited]))
			{
				for (int i = inventory.innerContainer.Count - 1; i >= 0; i--)
				{
					if (inventory.innerContainer[i] is Pawn pawn)
						DisembarkPawn(pawn);
				}
			}
			EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
		}
		else
		{
			// Invalid operation but better to send the pawns to world and let the game decide how to
			// handle them
			Trace.Fail("Disembarking from vehicle when it is not spawned or in a caravan.");
			for (int i = inventory.innerContainer.Count - 1; i >= 0; i--)
			{
				if (inventory.innerContainer[i] is Pawn pawn)
				{
					inventory.innerContainer.RemoveAt(i);
					Find.WorldPawns.PassToWorld(pawn);
					EventRegistry[VehicleEventDefOf.PawnRemoved].ExecuteEvents();
				}
			}
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
					Pawn pawn = handler.thingOwner[i];
					handler.thingOwner.Remove(pawn);
					caravan.AddPawn(pawn, true);
					Find.WorldPawns.PassToWorld(pawn);
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
			Trace.Fail("Disembarking from vehicle when it is not spawned or in a caravan.");
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
}