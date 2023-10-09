using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public abstract class WorkGiver_CarryToVehicle : WorkGiver_Scanner
	{
		protected static HashSet<Thing> neededItems = new HashSet<Thing>();

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
					int jobCount = Mathf.Min(thing.stackCount, CountLeftToTransferItem(vehicle, pawn, thing));
					if (jobCount > 0)
					{
						Job job = JobMaker.MakeJob(JobDefOf_Vehicles.LoadVehicle, thing, t);
						job.count = jobCount;
						return job;
					}
				}
			}
			return null;
		}

		public static Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn)
		{
			List<TransferableOneWay> transferables = vehicle.cargoToLoad;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (CountLeftToTransferPack(vehicle, pawn, transferableOneWay) > 0)
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
			Thing result = ClosestThingFor(pawn, ThingRequestGroup.Pawn);
			result ??= ClosestThingFor(pawn, ThingRequestGroup.HaulableEver);
			neededItems.Clear();
			return result;
		}

		private static Thing ClosestThingFor(Pawn pawn, ThingRequestGroup thingRequestGroup)
		{
			Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(thingRequestGroup), PathEndMode.Touch, TraverseParms.For(pawn),
				validator: (Thing thing) => neededItems.Contains(thing) && !thing.IsForbidden(pawn) && pawn.CanReserve(thing));
			return thing;
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
			TransferableOneWay cargo = vehicle.cargoToLoad.FirstOrDefault(t => t.AnyThing == thing);
			if (cargo == null)
			{
				return 0;
			}
			int countToTransfer = cargo.CountToTransfer - TransferableCountHauledByOthersForPacking(vehicle, pawn, cargo);
			int minCount = Mathf.Min(thing.stackCount, countToTransfer);
			Log.Message($"{pawn} transfering {thing} Count={minCount}  ToTransfer={cargo.CountToTransfer} ByOthers: {TransferableCountHauledByOthersForPacking(vehicle, pawn, cargo)}");
			return Mathf.Max(minCount, 0);
		}

		private static int TransferableCountHauledByOthersForPacking(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			List<Pawn> allPawnsSpawned = vehicle.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
			int num = 0;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				Pawn pawn2 = allPawnsSpawned[i];
				if (pawn2 != pawn && pawn2.CurJob != null && (pawn2.CurJob.def == JobDefOf_Vehicles.LoadVehicle || pawn2.CurJob.def == JobDefOf_Vehicles.CarryItemToVehicle))
				{
					if (pawn2.jobs.curDriver is JobDriver_LoadVehicle driver)
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
