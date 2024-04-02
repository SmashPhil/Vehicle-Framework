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
	public class WorkGiver_BringUpgradeMaterial : WorkGiver_CarryToVehicle
	{
		private static HashSet<ThingDef> neededThingDefs = new HashSet<ThingDef>();

		public override string ReservationName => ReservationType.LoadUpgradeMaterials;

		public override JobDef JobDef => JobDefOf_Vehicles.LoadUpgradeMaterials;

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

		public override bool JobAvailable(VehiclePawn vehicle)
		{
			return vehicle.CompUpgradeTree.CurrentlyUpgrading && !vehicle.CompUpgradeTree.StoredCostSatisfied;
		}

		public override ThingOwner<Thing> ThingOwner(VehiclePawn vehicle)
		{
			return vehicle.CompUpgradeTree.upgradeContainer;
		}

		public override IEnumerable<ThingDefCount> ThingDefs(VehiclePawn vehicle)
		{
			if (!vehicle.CompUpgradeTree.CurrentlyUpgrading || vehicle.CompUpgradeTree.NodeUnlocking.ingredients.NullOrEmpty())
			{
				yield break;
			}
			foreach (ThingDefCountClass thingDefCountClass in vehicle.CompUpgradeTree.NodeUnlocking.ingredients)
			{
				yield return new ThingDefCount(thingDefCountClass.thingDef, thingDefCountClass.count);
			}
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
			if (ThingDefs(vehicle).NotNullAndAny() && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				Thing thing = FindThingToPack(vehicle, pawn);
				if (thing != null)
				{
					int countLeft = CountLeftToPack(vehicle, pawn, new ThingDefCount(thing.def, thing.stackCount));
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

		public override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn)
		{
			Thing result = null;
			IEnumerable<ThingDefCount> thingDefs = ThingDefs(vehicle);
			if (thingDefs.NotNullAndAny())
			{
				foreach (ThingDefCount thingDefCount in thingDefs)
				{
					int countLeftToTransfer = CountLeftToPack(vehicle, pawn, thingDefCount);
					if (countLeftToTransfer > 0)
					{
						neededThingDefs.Add(thingDefCount.ThingDef);
					}
				}
				if (!neededThingDefs.Any())
				{
					return null;
				}

				result = ClosestHaulable(pawn, ThingRequestGroup.Pawn, validator: ValidThingDef);
				result ??= ClosestHaulable(pawn, ThingRequestGroup.HaulableEver, validator: ValidThingDef);
				neededThingDefs.Clear();
			}
			return result;

			bool ValidThingDef(Thing thing)
			{
				return neededThingDefs.Contains(thing.def) && pawn.CanReserve(thing) && !thing.IsForbidden(pawn.Faction);
			}
		}

		private int CountLeftToPack(VehiclePawn vehicle, Pawn pawn, ThingDefCount thingDefCount)
		{
			if (thingDefCount.Count <= 0 || thingDefCount.ThingDef == null)
			{
				return 0;
			}
			return Mathf.Max(thingDefCount.Count - TransferableCountHauledByOthersForPacking(vehicle, pawn, thingDefCount), 0);
		}

		private int TransferableCountHauledByOthersForPacking(VehiclePawn vehicle, Pawn pawn, ThingDefCount thingDefCount)
		{
			int mechCount = 0;
			if (ModsConfig.BiotechActive)
			{
				mechCount = HauledByOthers(pawn, thingDefCount, vehicle.Map.mapPawns.SpawnedColonyMechs());
			}
			int slaveCount = 0;
			if (ModsConfig.IdeologyActive)
			{
				slaveCount = HauledByOthers(pawn, thingDefCount, vehicle.Map.mapPawns.SlavesOfColonySpawned);
			}
			return mechCount + slaveCount + HauledByOthers(pawn, thingDefCount, vehicle.Map.mapPawns.FreeColonistsSpawned);
		}

		private int HauledByOthers(Pawn pawn, ThingDefCount thingDefCount, List<Pawn> pawns)
		{
			int count = 0;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn target = pawns[i];
				count += CountFromJob(pawn, target, thingDefCount, pawns);
			}
			return count;
		}

		protected virtual int CountFromJob(Pawn pawn, Pawn target, ThingDefCount thingDefCount, List<Pawn> pawns)
		{
			if (target != pawn && target.CurJob != null && (target.CurJob.def == JobDef || target.CurJob.def == JobDefOf_Vehicles.CarryItemToVehicle))
			{
				if (target.jobs.curDriver is JobDriver_LoadVehicle driver)
				{
					Thing toHaul = driver.Item;
					if (toHaul != null && thingDefCount.ThingDef == toHaul.def)
					{
						return toHaul.stackCount;
					}
				}
			}
			return 0;
		}
	}
}
