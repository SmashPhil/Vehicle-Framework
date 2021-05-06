using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class WorkGiver_RepairVehicle : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<ListerVehiclesRepairable>().RepairsForFaction(pawn.Faction);

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction)
			{
				return null;
			}
			if(t is VehiclePawn vehicle && vehicle.statHandler.NeedsRepairs && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<LocalTargetInfo, VehicleTargetReservation>(vehicle, pawn, null) &&
				pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				return JobMaker.MakeJob(JobDefOf_Vehicles.RepairVehicle, vehicle);
			}
			return null;
		}
	}
}
