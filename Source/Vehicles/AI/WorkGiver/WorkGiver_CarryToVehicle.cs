using System;
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
		private static HashSet<Thing> neededThings = new HashSet<Thing>();

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public virtual string ReservationName => ReservationType.LoadVehicle;

		public virtual JobDef JobDef => JobDefOf_Vehicles.LoadVehicle;

		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

		public abstract ThingOwner<Thing> ThingOwner(VehiclePawn vehicle);

		public virtual List<TransferableOneWay> Transferables(VehiclePawn vehicle)
		{
			return null;
		}

		public virtual IEnumerable<ThingDefCount> ThingDefs(VehiclePawn vehicle)
		{
			return null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationName);
		}

		public virtual bool JobAvailable(VehiclePawn vehicle)
		{
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			VehiclePawn vehicle = t as VehiclePawn;
			if (vehicle == null)
			{
				return null;
			}
			if (pawn.Faction != t.Faction)
			{
				return null;
			}
			if (!JobAvailable(vehicle))
			{
				return null;
			}
			if ((!Transferables(vehicle).NullOrEmpty() || ThingDefs(vehicle).NotNullAndAny()) && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				Thing thing = FindThingToPack(vehicle, pawn);
				if (thing != null)
				{
					int countLeft = CountLeftForItem(vehicle, pawn, thing);
					int jobCount = Mathf.Min(thing.stackCount, countLeft);
					if (jobCount > 0)
					{
						Job job = JobMaker.MakeJob(JobDef, thing, t);
						job.count = jobCount;
						return job;
					}
				}
			}
			return null;
		}

		public virtual Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn)
		{
			Thing result = null;
			List<TransferableOneWay> transferables = Transferables(vehicle);
			if (!transferables.NullOrEmpty())
			{
				for (int i = 0; i < transferables.Count; i++)
				{
					TransferableOneWay transferableOneWay = transferables[i];
					int countLeftToTransfer = CountLeftToPack(vehicle, pawn, transferableOneWay);
					if (countLeftToTransfer > 0)
					{
						for (int j = 0; j < transferableOneWay.things.Count; j++)
						{
							neededThings.Add(transferableOneWay.things[j]);
						}
					}
				}
				if (!neededThings.Any())
				{
					return null;
				}

				result = ClosestHaulable(pawn, ThingRequestGroup.Pawn, validator: ValidThing);
				result ??= ClosestHaulable(pawn, ThingRequestGroup.HaulableEver, validator: ValidThing);
				neededThings.Clear();
			}
			return result;

			bool ValidThing(Thing thing)
			{
				return neededThings.Contains(thing) && pawn.CanReserve(thing) && !thing.IsForbidden(pawn.Faction);
			}
		}

		protected Thing ClosestHaulable(Pawn pawn, ThingRequestGroup thingRequestGroup, Predicate<Thing> validator = null)
		{
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(thingRequestGroup), PathEndMode.Touch, TraverseParms.For(pawn), validator: validator);
		}

		private int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			{
				return 0;
			}
			return Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthersForPacking(vehicle, pawn, transferable), 0);
		}

		private int CountLeftForItem(VehiclePawn vehicle, Pawn pawn, Thing thing)
		{
			TransferableOneWay transferable = JobDriver_LoadVehicle.GetTransferable(Transferables(vehicle), vehicle, thing);
			if (transferable == null)
			{
				return 0;
			}
			return CountLeftToPack(vehicle, pawn, transferable);
		}

		private int TransferableCountHauledByOthersForPacking(VehiclePawn vehicle, Pawn pawn, TransferableOneWay transferable)
		{
			int mechCount = 0;
			if (ModsConfig.BiotechActive)
			{
				mechCount = HauledByOthers(pawn, transferable, vehicle.Map.mapPawns.SpawnedColonyMechs());
			}
			int slaveCount = 0;
			if (ModsConfig.IdeologyActive)
			{
				slaveCount = HauledByOthers(pawn, transferable, vehicle.Map.mapPawns.SlavesOfColonySpawned);
			}
			return mechCount + slaveCount + HauledByOthers(pawn, transferable, vehicle.Map.mapPawns.FreeColonistsSpawned);
		}

		private int HauledByOthers(Pawn pawn, TransferableOneWay transferable, List<Pawn> pawns)
		{
			int count = 0;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn target = pawns[i];
				count += CountFromJob(pawn, target, transferable, pawns);
			}
			return count;
		}

		protected virtual int CountFromJob(Pawn pawn, Pawn target, TransferableOneWay transferable, List<Pawn> pawns)
		{
			if (target != pawn && target.CurJob != null && (target.CurJob.def == JobDef || target.CurJob.def == JobDefOf_Vehicles.CarryItemToVehicle))
			{
				if (target.jobs.curDriver is JobDriver_LoadVehicle driver)
				{
					Thing toHaul = driver.Item;
					if (toHaul != null && transferable.things.Contains(toHaul) || TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
					{
						return toHaul.stackCount;
					}
				}
			}
			return 0;
		}
	}
}
