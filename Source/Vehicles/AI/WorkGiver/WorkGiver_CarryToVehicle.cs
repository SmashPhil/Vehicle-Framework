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

		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadVehicle);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t is VehiclePawn vehicle && !vehicle.cargoToLoad.NullOrEmpty() && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				Thing thing = FindThingToPack(vehicle, pawn);
				if (thing != null)
				{
					int countLeft = CountLeftForItem(vehicle, pawn, thing);
					int jobCount = Mathf.Min(thing.stackCount, countLeft);
					Log.Message($"Found: {thing} to pack. CountLeft={countLeft} JobCount={jobCount}");
					if (jobCount > 0)
					{
						Job job = JobMaker.MakeJob(JobDefOf_Vehicles.LoadVehicle, thing, t);
						job.count = jobCount;
						return job;
					}
				}
				else
				{
					Log.Message($"Nothing to pack");
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
				int countLeftToTransfer = CountLeftToPack(vehicle, pawn, transferableOneWay);
				if (countLeftToTransfer > 0)
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

			Thing result = ClosestHaulable(pawn, ThingRequestGroup.Pawn);
			result ??= ClosestHaulable(pawn, ThingRequestGroup.HaulableEver);
			Log.Message($"Result = {result}");
			neededItems.Clear();
			return result;
		}

		private static Thing ClosestHaulable(Pawn pawn, ThingRequestGroup thingRequestGroup)
		{
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(thingRequestGroup), PathEndMode.Touch, TraverseParms.For(pawn),
				validator: (Thing thing) => neededItems.Contains(thing) && pawn.CanReserve(thing));
		}

		public static int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			{
				return 0;
			}
			return Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthersForPacking(vehicle, pawn, transferable), 0);
		}

		private static int CountLeftForItem(VehiclePawn vehicle, Pawn pawn, Thing thing)
		{
			TransferableOneWay transferable = JobDriver_LoadVehicle.GetTransferable(vehicle, thing);
			if (transferable == null)
			{
				return 0;
			}
			return CountLeftToPack(vehicle, pawn, transferable);
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
