using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;


namespace Vehicles;

public class JobGiver_PrepareVehicleCaravan_GatheringItems : ThinkNode_JobGiver
{
	private static JobDef JobDef => JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle;

	protected override Job TryGiveJob(Pawn pawn)
	{
		if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			return null;

		Lord lord = pawn.GetLord();
		List<TransferableOneWay> transferables = GatherItemsForVehicleCaravanUtility.GetCaravanTransferables(lord);
		Thing thing = JobDriver_LoadVehicle.FindThingToPack(pawn, JobDef, transferables, lord);
		if (thing is null)
			return null;

		return new Job(JobDef, thing)
		{
			lord = lord
		};
	}
}