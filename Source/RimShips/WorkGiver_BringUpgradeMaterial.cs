using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Vehicles.Defs;
using UnityEngine;

namespace Vehicles.Jobs
{
    public class WorkGiver_BringUpgradeMaterial : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
            return pawn.Map.spawnedThings;
		}


        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
                return null;
            if(t is VehiclePawn availableBoat && t.TryGetComp<CompUpgradeTree>() != null && t.TryGetComp<CompUpgradeTree>().CurrentlyUpgrading && 
                !t.TryGetComp<CompUpgradeTree>().NodeUnlocking.StoredCostSatisfied && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
            {
                Log.Message("Boat: " + t);
                return UpgradeNodeDeliverJob(pawn, availableBoat);
            }
            
            return null;
        }

        protected Job UpgradeNodeDeliverJob(Pawn pawn, VehiclePawn boat)
        {
            IUpgradeable u = boat.GetComp<CompUpgradeTree>().NodeUnlocking;
            List<ThingDefCountClass> materials = u.MaterialsNeeded();
            int count = materials.Count;
            for(int i = 0; i < count; i++)
            {
                ThingDefCountClass materialRequired = materials[i];

                /*
                if(!pawn.Map.itemAvailability.ThingsAvailableAnywhere(materialRequired, pawn))
                    continue;
                */

                Thing foundResource = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(materialRequired.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn,
                    Danger.Deadly, TraverseMode.ByPawn, false), 9999f, (Thing t) => t.def == materialRequired.thingDef && !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, false));
                if(foundResource != null)
                {
                    FindAvailableNearbyResources(foundResource, pawn, out int resourceTotalAvailable);
                    Job job = JobMaker.MakeJob(JobDefOf_Ships.LoadUpgradeMaterials, foundResource, boat);
                    job.count = foundResource.stackCount > materialRequired.count ? materialRequired.count : foundResource.stackCount;
                    return job;
                }
            }
            return null;
        }

        private void FindAvailableNearbyResources(Thing firstFoundResource, Pawn pawn, out int resTotalAvailable)
        {
	        int num = Mathf.Min(firstFoundResource.def.stackLimit, pawn.carryTracker.MaxStackSpaceEver(firstFoundResource.def));
	        resTotalAvailable = 0;
	        resourcesAvailable.Clear();
	        resourcesAvailable.Add(firstFoundResource);
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
				        resourcesAvailable.Add(thing);
				        resTotalAvailable += thing.stackCount;
			        }
		        }
	        }
        }

        public static List<Thing> resourcesAvailable = new List<Thing>();
    }
}
