using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_PaintVehicle : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn is VehiclePawn vehicle && vehicle.CanPaintNow);

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction)
			{
				return null;
			}
			if (t is VehiclePawn vehicle && vehicle.CanPaintNow && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<LocalTargetInfo, VehicleTargetReservation>(vehicle, pawn, null) &&
				pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
			{
				return JobMaker.MakeJob(JobDefOf_Vehicles.PaintVehicle, vehicle);
			}
			return null;
		}
	}
}
