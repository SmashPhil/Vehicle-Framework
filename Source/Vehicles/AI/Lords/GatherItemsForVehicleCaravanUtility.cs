using System.Collections.Generic;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public static class GatherItemsForVehicleCaravanUtility
{
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
}