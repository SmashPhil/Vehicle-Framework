using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vehicles.Defs;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Vehicles.Jobs
{
    public class WorkGiver_PackBoat : WorkGiver_Scanner
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
            if(t is VehiclePawn availableBoat && t.TryGetComp<CompVehicle>().cargoToLoad.Any() && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
            {
                Thing thing = FindThingToPack(availableBoat, pawn);
                if (thing != null)
                {
                    Job job = JobMaker.MakeJob(JobDefOf_Ships.CarryItemToShip, thing, availableBoat);
                    job.count = thing.stackCount;
                    return job;
                }
            }
            
            return null;
        }

        public static Thing FindThingToPack(VehiclePawn boat, Pawn p)
		{
            List<TransferableOneWay> transferables = boat.GetComp<CompVehicle>().cargoToLoad;
			for (int i = 0; i < transferables.Count; i++)
			{
				TransferableOneWay transferableOneWay = transferables[i];
				if (CountLeftToTransferPack(boat, p, transferableOneWay) > 0)
				{
					for (int j = 0; j < transferableOneWay.things.Count; j++)
					{
						neededItems.Add(transferableOneWay.things[j]);
					}
				}
			}
			if (!neededItems.Any<Thing>())
			{
				return null;
			}
			Thing result = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, 
                TraverseMode.ByPawn, false), 9999f, (Thing x) => neededItems.Contains(x) && p.CanReserve(x, 1, -1, null, false), null, 0, -1, false, RegionType.Set_Passable, false);
			neededItems.Clear();
			return result;
		}

        public static int CountLeftToTransferPack(VehiclePawn boat, Pawn pawn, TransferableOneWay transferable)
		{
			if (transferable.CountToTransfer <= 0 || !transferable.HasAnyThing)
			{
				return 0;
			}
			return Mathf.Max(transferable.CountToTransfer - TransferableCountHauledByOthersForPacking(boat, pawn, transferable), 0);
		}

        private static int TransferableCountHauledByOthersForPacking(VehiclePawn boat, Pawn pawn, TransferableOneWay transferable)
		{
			if (!transferable.HasAnyThing)
			{
				Log.Warning("Can't determine transferable count hauled by others because transferable has 0 things.", false);
				return 0;
			}
            List<Pawn> allPawnsSpawned = boat.Map.mapPawns.AllPawnsSpawned;
			int num = 0;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				Pawn pawn2 = allPawnsSpawned[i];
				if (pawn2 != pawn && pawn2.CurJob != null && pawn2.CurJob.def == JobDefOf.HaulToContainer)
				{
					Thing toHaul = ((JobDriver_PrepareCaravan_GatherItems)pawn2.jobs.curDriver).ToHaul;
					if (transferable.things.Contains(toHaul) || TransferableUtility.TransferAsOne(transferable.AnyThing, toHaul, TransferAsOneMode.PodsOrCaravanPacking))
					{
						num += toHaul.stackCount;
					}
				}
			}
			return num;
		}

        public static HashSet<Thing> neededItems = new HashSet<Thing>();
    }
}
