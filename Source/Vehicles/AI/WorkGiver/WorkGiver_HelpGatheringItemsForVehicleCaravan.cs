using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class WorkGiver_HelpGatheringItemsForVehicleCaravan : WorkGiver
{
	public override Job NonScanJob(Pawn pawn)
	{
		foreach (Lord lord in pawn.Map.lordManager.lords)
		{
			if (lord.LordJob is not LordJob_FormAndSendVehicles { GatherItemsNow: true })
				continue;

			Thing thing = GatherItemsForVehicleCaravanUtility.FindThingToHaul(pawn, lord);
			if (thing != null && AnyReachableCarrierOrColonist(pawn, lord))
			{
				Job job = JobMaker.MakeJob(JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle, thing);
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