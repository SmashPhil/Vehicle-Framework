using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public class JobDriver_PrepareVehicleCaravan_GatheringItems : JobDriverLoadVehicleBase
{
	private List<TransferableOneWay> ThingsToLoad =>
		GatherItemsForVehicleCaravanUtility.GetCaravanTransferables(job.lord);

	protected override bool ShouldGatherItems => !pawn.IsFormingVehicleCaravan() || base.ShouldGatherItems;

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
		Transferable.AdjustTo(Mathf.Max(Transferable.CountToTransfer - thing.stackCount, 0));
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
		return GatherItemsForVehicleCaravanUtility.CountLeftToTransfer(pawn, Transferable, job.lord);
	}

	protected override Thing FindThingToHaul()
	{
		return GatherItemsForVehicleCaravanUtility.FindThingToHaul(pawn, pawn.GetLord());
	}

	protected override bool IsUsableCarrier(Pawn carrier, bool allowColonists = true)
	{
		return GatherItemsForVehicleCaravanUtility.IsUsableCarrier(carrier, pawn, allowColonists: allowColonists);
	}
}