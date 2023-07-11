using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_PackVehicle : WorkGiver_Scanner
	{
		public static HashSet<Thing> neededItems = new HashSet<Thing>();

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadVehicle);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t is VehiclePawn vehicle && vehicle.cargoToLoad.NotNullAndAny() && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				Thing thing = FindThingToPack(vehicle, pawn);
				if (thing != null)
				{
					Job job = JobMaker.MakeJob(JobDefOf_Vehicles.CarryItemToVehicle, thing, t);
					job.count = Mathf.Min(thing.stackCount, CountLeftToTransferItem(vehicle, pawn, thing));
					return job;
				}
			}
			return null;
		}

		public static Thing FindThingToPack(VehiclePawn vehicle, Pawn p)
		{
			List<TransferableOneWay> transferables = vehicle.cargoToLoad;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (CountLeftToTransferPack(vehicle, p, transferableOneWay) > 0)
				{
					for (int j = 0; j < transferableOneWay.things.Count; j++)
					{
						neededItems.Add(transferableOneWay.things[j]);
					}
				}
			}
			if (!neededItems.Any())
			{
				return null;
			}
			Thing result = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, 
				TraverseMode.ByPawn, false), 9999f, (Thing x) => neededItems.Contains(x) && !x.IsForbidden(p) && p.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
			neededItems.Clear();
			return result;
		}

		public static int CountLeftToTransferPack(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			{
				return 0;
			}
			return Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthersForPacking(vehicle, pawn, transferable), 0);
		}

		private static int CountLeftToTransferItem(VehiclePawn vehicle, Pawn pawn, Thing thing)
		{
			var item = vehicle.cargoToLoad.FirstOrDefault(t => t.AnyThing.def == thing.def);
			return Mathf.Max(Mathf.Min(thing.stackCount, item.CountToTransfer - TransferableCountHauledByOthersForPacking(vehicle, pawn, item)), 0);
		}

		private static int TransferableCountHauledByOthersForPacking(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			List<Pawn> allPawnsSpawned = vehicle.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
			int num = 0;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				Pawn pawn2 = allPawnsSpawned[i];
				if (pawn2 != pawn && pawn2.CurJob != null && pawn2.CurJob.def == JobDefOf_Vehicles.CarryItemToVehicle)
				{
					if (pawn2.jobs.curDriver is JobDriver_GiveToVehicle driver)
					{
						Thing toHaul = driver.Item;
						if (toHaul != null && transferable.things.Contains(toHaul) || TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
						{
							num += toHaul.stackCount;
						}
					}
				}
			}
			return num;
		}
	}
}
