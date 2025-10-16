using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

public class JobDriver_PrepareVehicleCaravan_GatheringItems : JobDriverLoadVehicleBase
{
	private List<TransferableOneWay> ThingsToLoad =>
		GatherItemsForVehicleCaravanUtility.GetCaravanTransferables(job.lord);

	private TransferableOneWay Transferable
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

	protected override void OnThingAddedToInventory(Thing thing)
	{
		TransferableOneWay transferable = TransferableUtility.TransferableMatchingDesperate(thing, ThingsToLoad,
			TransferAsOneMode.PodsOrCaravanPacking);
		transferable.AdjustTo(Mathf.Max(transferable.CountToTransfer - thing.stackCount, 0));
	}

	protected override bool HasDuplicateOpportunity(Thing thing)
	{
		return Transferable.things.Contains(thing);
	}

	protected override bool ShouldFailJob()
	{
		return !Map.lordManager.lords.Contains(job.lord);
	}

	protected override int CountLeftToTransfer()
	{
		return JobDriver_LoadVehicle.CountLeftToPack(pawn, job.def, Transferable, job.lord);
	}

	protected override Thing FindThingToHaul()
	{
		return JobDriver_LoadVehicle.FindThingToPack(pawn, job.def, ThingsToLoad, job.lord);
	}

	protected override bool IsUsableCarrier(Pawn carrier, bool allowColonists = true)
	{
		return GatherItemsForVehicleCaravanUtility.IsUsableCarrier(carrier, pawn, allowColonists: allowColonists);
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
}