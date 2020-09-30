using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles.Defs;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class WorkGiver_WorkOnUpgrade : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(VehicleRequest.Upgrade);

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
                return null;
            if(t is VehiclePawn vehicle && t.TryGetComp<CompUpgradeTree>() != null && t.TryGetComp<CompUpgradeTree>().CurrentlyUpgrading && 
                pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<ThingDefCountClass, VehicleNodeReservation>(vehicle, pawn, null) &&
                t.TryGetComp<CompUpgradeTree>().NodeUnlocking.StoredCostSatisfied && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
            {
                return JobMaker.MakeJob(JobDefOf_Vehicles.UpgradeVehicle, vehicle);
            }
            return null;
        }
    }
}
