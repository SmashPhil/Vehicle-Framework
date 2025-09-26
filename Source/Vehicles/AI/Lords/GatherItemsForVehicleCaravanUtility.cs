using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public static class GatherItemsForVehicleCaravanUtility
{
	private static readonly HashSet<Thing> NeededItems = [];

	public static List<TransferableOneWay> GetCaravanTransferables(Lord lord)
	{
		var caravanLordJob = lord.LordJob as LordJob_FormAndSendVehicles;
		Assert.IsNotNull(caravanLordJob,
			$"{nameof(JobDriver_PrepareCaravan_GatherItems)} can only be used with {nameof(LordJob_FormAndSendVehicles)} as a duty assignment.");
		return caravanLordJob.transferables;
	}

	public static bool IsUsableCarrier(Pawn carrier, Pawn forPawn, bool allowColonists = true)
	{
		if (carrier is VehiclePawn vehicle)
		{
			return vehicle.IsFormingVehicleCaravan() && (!vehicle.DestroyedOrNull() && vehicle.Spawned) &&
				vehicle.Faction == forPawn.Faction
				&& !vehicle.IsBurning() && vehicle.movementStatus != VehicleMovementStatus.Offline
				&& !MassUtility.IsOverEncumbered(vehicle);
		}
		return !CaravanHelper.assignedSeats.IsAssigned(carrier) &&
			JobDriver_PrepareCaravan_GatherItems.IsUsableCarrier(carrier, forPawn, allowColonists: allowColonists);
	}

	public static Thing FindThingToHaul(Pawn pawn, Lord lord)
	{
		using ClearOnDispose<Thing> cod = new(NeededItems);
		List<TransferableOneWay> transferables = GetCaravanTransferables(lord);
		foreach (TransferableOneWay transferable in transferables)
		{
			if (CountLeftToTransfer(pawn, transferable, lord) <= 0)
				continue;

			foreach (Thing thing in transferable.things)
			{
				NeededItems.Add(thing);
			}
		}

		if (NeededItems.Count == 0)
			return null;

		Thing result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
			ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(pawn),
			validator: thing => NeededItems.Contains(thing) && pawn.CanReserve(thing));
		return result;
	}

	public static int CountLeftToTransfer(Pawn pawn, TransferableOneWay transferable, Lord lord)
	{
		if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
		{
			return 0;
		}
		int x = Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthers(pawn, transferable, lord), 0);
		return x;
	}

	private static int TransferableCountHauledByOthers(Pawn pawn, TransferableOneWay transferable, Lord lord)
	{
		if (!transferable.HasAnyThing)
		{
			Log.Warning("Can't determine transferable count hauled by others because transferable has 0 things.");
			return 0;
		}

		int count = 0;
		foreach (Pawn spawnedPawn in lord.Map.mapPawns.AllPawnsSpawned)
		{
			if (spawnedPawn == pawn)
				continue;
			if (spawnedPawn.CurJob == null || spawnedPawn.CurJob.def != JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle)
				continue;
			if (spawnedPawn.CurJob.lord != lord)
				continue;

			var driver = (JobDriver_PrepareVehicleCaravan_GatheringItems)spawnedPawn.jobs.curDriver;
			Thing toHaul = driver.ToHaul;
			if (transferable.things.Contains(toHaul) ||
				TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
			{
				count += toHaul.stackCount;
			}
		}
		return count;
	}
}