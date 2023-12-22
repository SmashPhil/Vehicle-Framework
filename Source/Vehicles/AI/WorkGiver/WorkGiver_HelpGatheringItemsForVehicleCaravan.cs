using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
	public class WorkGiver_HelpGatheringItemsForVehicleCaravan : WorkGiver
	{
		public override Job NonScanJob(Pawn pawn)
		{
			List<Lord> lords = pawn.Map.lordManager.lords;
			for (int i = 0; i < lords.Count; i++)
			{
				LordJob_FormAndSendVehicles lordJob_FormAndSendCaravan = lords[i].LordJob as LordJob_FormAndSendVehicles;
				if (lordJob_FormAndSendCaravan != null && lordJob_FormAndSendCaravan.GatherItemsNow)
				{
					Thing thing = GatherItemsForVehicleCaravanUtility.FindThingToHaul(pawn, lords[i]);
					if (thing != null && AnyReachableCarrierOrColonist(pawn, lords[i]))
					{
						Job job = JobMaker.MakeJob(JobDefOf_Vehicles.PrepareCaravan_GatheringVehicle, thing);
						job.lord = lords[i];
						return job;
					}
				}
			}
			return null;
		}

		private bool AnyReachableCarrierOrColonist(Pawn forPawn, Lord lord)
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				if (lord.ownedPawns[i] is VehiclePawn vehicle && JobDriver_PrepareVehicleCaravan_GatheringItems.IsUsableCarrier(vehicle, forPawn) && !lord.ownedPawns[i].IsForbidden(forPawn) && forPawn.CanReach(lord.ownedPawns[i], PathEndMode.Touch, Danger.Deadly))
				{
					return true;
				}
			}
			return false;
		}
	}
}
