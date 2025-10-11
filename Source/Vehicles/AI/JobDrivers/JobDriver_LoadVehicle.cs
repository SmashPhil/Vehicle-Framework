using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

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
		if (MassUtility.IsOverEncumbered(Vehicle))
			return true;

		return !MapComponentCache<VehicleReservationManager>.GetComponent(Map)
		 .VehicleListed(Vehicle, ListerTag);
	}

	protected override int CountLeftToTransfer()
	{
		return CountLeftToPack(Vehicle, pawn, job.def, Transferable);
	}

	protected override Thing FindThingToHaul()
	{
		return FindThingToPack(Vehicle, pawn, job.def, ThingsToLoad);
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
	public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, JobDef jobDef,
		[CanBeNull] List<TransferableOneWay> transferables)
	{
		if (transferables.NullOrEmpty())
			return null;

		Assert.IsNotNull(jobDef);
		using ObjectPool<ThingSet>.Scope ap = SetPool.GetTemporary(out ThingSet thingSet);
		foreach (TransferableOneWay transferableOneWay in transferables)
		{
			int countLeftToTransfer = CountLeftToPack(vehicle, pawn, jobDef, transferableOneWay);
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

	public static int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, JobDef jobDef, TransferableOneWay transferable)
	{
		if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			return 0;

		using ObjectPool<TransferableSearch>.Scope ap = SearchPool.GetTemporary(out TransferableSearch transSearch);
		transSearch.Init(jobDef, transferable);
		int hauledByOthers =
			Search.CountHauledByOthersForPacking(vehicle, pawn, transferable.AnyThing, transSearch);
		int remaining = transferable.CountToTransfer - hauledByOthers;
		return Mathf.Clamp(remaining, 0, int.MaxValue);
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

	protected sealed class TransferableSearch : ISharedJobSearch, IPoolable
	{
		private JobDef jobDef;
		private TransferableOneWay transferable;

		bool IPoolable.InPool { get; set; }

		public void Init(JobDef jobDef, TransferableOneWay transferable)
		{
			this.jobDef = jobDef;
			this.transferable = transferable;
		}

		bool ISharedJobSearch.IsMatchingThing(Thing thing)
		{
			return transferable.things.Contains(thing);
		}

		bool ISharedJobSearch.ShouldConsiderPawn(Pawn otherPawn)
		{
			return otherPawn.CurJobDef == jobDef;
		}

		void IPoolable.Reset()
		{
			jobDef = null;
			transferable = null;
		}
	}
}