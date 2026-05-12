using System;
using System.Collections.Generic;
using CoreLib.Performance;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[PublicAPI]
public class JobDriver_LoadVehicle : JobDriverLoadVehicleBase
{
	private static readonly ObjectPool<ThingSet> SetPool = new(5);
	private static readonly ObjectPool<TransferableSearch> SearchPool = new(5);

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

	// TODO 1.6.2091 - stub to prevent breaking VehicleMapFramework patch.
	[UsedImplicitly, Obsolete("Deprecated. Call ShouldFailJob instead.", error: true)]
	protected bool FailJob()
	{
		return ShouldFailJob();
	}

	protected override bool ShouldFailJob()
	{
		if (MassUtility.IsOverEncumbered(Vehicle))
			return true;

		return !MapComponentCache<VehicleReservationManager>.GetComponent(Map)
		 .VehicleListed(Vehicle, ListerTag);
	}

	protected override int CountLeftToTransfer()
	{
		return CountLeftToPack(pawn, job.def, Transferable);
	}

	protected override Thing FindThingToHaul()
	{
		return FindThingToPack(pawn, job.def, ThingsToLoad);
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

	/// <summary>
	/// Search for item to pack given the list of required items in <paramref name="transferables"/>.
	/// </summary>
	public static Thing FindThingToPack(Pawn pawn, JobDef jobDef, List<TransferableOneWay> transferables,
		Lord lord = null)
	{
		if (transferables.NullOrEmpty())
			return null;

		Assert.IsNotNull(jobDef);
		using ObjectPool<ThingSet>.Scope ap = SetPool.GetTemporary(out ThingSet thingSet);
		foreach (TransferableOneWay transferableOneWay in transferables)
		{
			int countLeftToTransfer = CountLeftToPack(pawn, jobDef, transferableOneWay, lord);
			if (countLeftToTransfer <= 0)
				continue;

			foreach (Thing thing in transferableOneWay.things)
			{
				thingSet.Add(thing);
			}
		}
		if (thingSet.Count == 0)
			return null;

		return Search.FindNearestThing(pawn, thingSet.IsValid);
	}

	public static int CountLeftToPack(Pawn pawn, JobDef jobDef, TransferableOneWay transferable, Lord lord = null)
	{
		if (transferable is not { HasAnyThing: true, CountToTransfer: > 0 })
			return 0;

		using ObjectPool<TransferableSearch>.Scope ap = SearchPool.GetTemporary(out TransferableSearch transSearch);
		transSearch.Init(jobDef, transferable, lord);
		int countBeingPacked = Search.CountAlreadyBeingPacked(pawn, transSearch);
		int remaining = transferable.CountToTransfer - countBeingPacked;
		return Mathf.Max(remaining, 0);
	}

	private sealed class ThingSet : IPoolable
	{
		private readonly HashSet<Thing> neededThings = [];

		public int Count => neededThings.Count;

		bool IPoolable.InPool { get; set; }

    public void Add(Thing thing)
		{
			neededThings.Add(thing);
		}

		public bool IsValid(Thing thing)
		{
			return neededThings.Contains(thing);
		}

		void IPoolable.Reset()
		{
			neededThings.Clear();
		}
	}
}