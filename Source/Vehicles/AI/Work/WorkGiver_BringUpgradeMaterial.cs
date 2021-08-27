using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;
using Vehicles.Defs;


namespace Vehicles
{
	public class WorkGiver_BringUpgradeMaterial : WorkGiver_Scanner
	{
		[ThreadStatic] public static List<Thing> tmpResourcesAvailable = new List<Thing>();

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadUpgradeMaterials);


		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction)
				return null;
			if(t is VehiclePawn availableBoat && t.TryGetComp<CompUpgradeTree>() != null && t.TryGetComp<CompUpgradeTree>().CurrentlyUpgrading && 
				!t.TryGetComp<CompUpgradeTree>().NodeUnlocking.StoredCostSatisfied && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				return UpgradeNodeDeliverJob(pawn, availableBoat);
			}
			
			return null;
		}

		protected Job UpgradeNodeDeliverJob(Pawn pawn, VehiclePawn vehicle)
		{
			UpgradeNode u = vehicle.CompUpgradeTree.NodeUnlocking;
			List<ThingDefCountClass> materials = u.MaterialsRequired().ToList();
			int count = materials.Count;

			bool flag = false;
			ThingDefCountClass thingDefCountClass = null;
			var reservationManager = vehicle.Map.GetCachedMapComponent<VehicleReservationManager>();
			for (int i = 0; i < count; i++)
			{
				ThingDefCountClass materialRequired = materials[i];

				if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(materialRequired, pawn))
				{
					flag = true;
					thingDefCountClass = materialRequired;
					break;
				}

				Thing foundResource = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(materialRequired.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn,
					Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Thing t) => t.def == materialRequired.thingDef && !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, false));

				if(foundResource != null && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<ThingDefCountClass, VehicleNodeReservation>(vehicle, pawn, null))
				{
					FindAvailableNearbyResources(foundResource, pawn, out int resourceTotalAvailable);
					Job job = JobMaker.MakeJob(JobDefOf_Vehicles.LoadUpgradeMaterials, foundResource, vehicle);
					int matCount = reservationManager.GetReservation<VehicleNodeReservation>(vehicle)?.MaterialsLeft().FirstOrDefault(m => m.thingDef == foundResource.def)?.count ?? int.MaxValue;
					job.count = foundResource.stackCount > matCount ? matCount : foundResource.stackCount;
					return job;
				}
				else
				{
					flag = true;
					thingDefCountClass = materialRequired;
				}
			}

			if (flag)
			{
				JobFailReason.Is(string.Format($"{"MissingMaterials".Translate()}: {thingDefCountClass.thingDef.label}"), null);
			}
			return null;
		}

		private void FindAvailableNearbyResources(Thing firstFoundResource, Pawn pawn, out int resTotalAvailable)
		{
			int num = Mathf.Min(firstFoundResource.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(firstFoundResource.def));
			resTotalAvailable = 0;
			tmpResourcesAvailable.Clear();
			tmpResourcesAvailable.Add(firstFoundResource);
			resTotalAvailable += firstFoundResource.stackCount;
			if (resTotalAvailable < num)
			{
				foreach (Thing thing in GenRadial.RadialDistinctThingsAround(firstFoundResource.Position, firstFoundResource.Map, 5f, false))
				{
					if (resTotalAvailable >= num)
					{
						break;
					}
					if (thing.def == firstFoundResource.def && GenAI.CanUseItemForWork(pawn, thing))
					{
						tmpResourcesAvailable.Add(thing);
						resTotalAvailable += thing.stackCount;
					}
				}
			}
		}
	}
}
