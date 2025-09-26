using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_LoadVehicle : JobDriverLoadVehicleBase
{
	private static readonly HashSet<Thing> NeededThings = [];

	protected virtual List<TransferableOneWay> ThingsToLoad => Vehicle.cargoToLoad;

	protected virtual string ListerTag => ReservationType.LoadVehicle;

	protected VehiclePawn Vehicle => Carrier as VehiclePawn;

	protected TransferableOneWay Transferable
	{
		get
		{
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(ToHaul, ThingsToLoad,
				TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay is null)
			{
				Trace.Fail("Could not find any matching transferable.");
				return null;
			}
			return transferableOneWay;
		}
	}

	protected override bool HasDuplicateOpportunity(Thing thing)
	{
		return Transferable.things.Contains(thing);
	}

	protected override void OnThingAddedToInventory(Thing thing)
	{
		TransferableOneWay transferable = Transferable;
		transferable.AdjustTo(Mathf.Max(transferable.CountToTransfer - thing.stackCount, 0));
		if (transferable.CountToTransfer <= 0)
		{
			Vehicle.cargoToLoad.Remove(transferable);
		}
	}

	// TODO 1.6.2091 - stub to prevent breaking VehicleMapFramework patch.
	[UsedImplicitly, Obsolete("Deprecated. Call ShouldFailJob instead.", error: true)]
	protected bool FailJob()
	{
		return ShouldFailJob();
	}

	protected override bool ShouldFailJob()
	{
		return !MapComponentCache<VehicleReservationManager>.GetComponent(Map)
		 .VehicleListed(Vehicle, ListerTag);
	}

	protected override int CountLeftToTransfer()
	{
		return CountLeftToPack(Vehicle, pawn, Transferable);
	}

	protected override Thing FindThingToHaul()
	{
		return FindThingToPack(Vehicle, pawn, ThingsToLoad);
	}

	protected override Toil StartedCarryingThing()
	{
		Toil toil = ToilMaker.MakeToil();
		toil.initAction = AddToTransferable;
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		toil.atomicWithPrevious = true;
		return toil;
	}

	private void AddToTransferable()
	{
		TransferableOneWay transferable = Transferable;
		if (!transferable.things.Contains(pawn.carryTracker.CarriedThing))
		{
			transferable.things.Add(pawn.carryTracker.CarriedThing);
		}
	}

	protected override bool IsUsableCarrier(Pawn carrier, bool allowColonists = true)
	{
		if (carrier.DestroyedOrNull() || !carrier.Spawned)
			return false;
		if (carrier.Faction != pawn.Faction)
			return false;
		if (carrier.IsBurning())
			return false;
		if (MassUtility.IsOverEncumbered(carrier))
			return false;

		return carrier is not VehiclePawn { movementStatus: VehicleMovementStatus.Offline };
	}

	public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn,
		[CanBeNull] List<TransferableOneWay> transferables)
	{
		if (transferables.NullOrEmpty())
			return null;

		using ClearOnDispose<Thing> cod = new(NeededThings);
		foreach (TransferableOneWay transferableOneWay in transferables)
		{
			int countLeftToTransfer = CountLeftToPack(vehicle, pawn, transferableOneWay);
			if (countLeftToTransfer <= 0)
				continue;

			foreach (Thing thing in transferableOneWay.things)
			{
				NeededThings.Add(thing);
			}
		}
		if (NeededThings.Count == 0)
			return null;

		return Search.FindNearestThing(pawn, HasThing);

		static bool HasThing(Thing thing)
		{
			return NeededThings.Contains(thing);
		}
	}

	public static int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
	{
		if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			return 0;

		int hauledByOthers =
			Search.TransferableCountHauledByOthersForPacking(vehicle, pawn, transferable.AnyThing,
				transferable.things.Contains);
		int remaining = transferable.CountToTransfer - hauledByOthers;
		return Mathf.Clamp(remaining, 0, int.MaxValue);
	}
}