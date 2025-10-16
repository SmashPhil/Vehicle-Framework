using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class WorkGiver_HelpGatheringItemsForVehicleCaravan : WorkGiver
{
	private static JobDef JobDef => JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle;

	public override Job NonScanJob(Pawn pawn)
	{
		foreach (Lord lord in pawn.Map.lordManager.lords)
		{
			if (lord.LordJob is not LordJob_FormAndSendVehicles { GatherItemsNow: true })
				continue;

			List<TransferableOneWay> transferables = GatherItemsForVehicleCaravanUtility.GetCaravanTransferables(lord);
			Thing thing = JobDriver_LoadVehicle.FindThingToPack(pawn, JobDef, transferables, lord);
			if (thing != null && AnyReachableCarrierOrColonist(pawn, lord))
			{
				Job job = JobMaker.MakeJob(JobDef, thing);
				job.lord = lord;
				return job;
			}
		}
		return null;
	}

	private static bool AnyReachableCarrierOrColonist(Pawn forPawn, Lord lord)
	{
		foreach (Pawn pawn in lord.ownedPawns)
		{
			if (pawn is not VehiclePawn vehicle || vehicle.IsForbidden(forPawn))
				continue;
			if (!GatherItemsForVehicleCaravanUtility.IsUsableCarrier(vehicle, forPawn))
				continue;

			if (forPawn.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly))
				return true;
		}
		return false;
	}
}