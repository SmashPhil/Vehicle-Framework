using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public abstract class VehicleWorkGiver : WorkGiver_Scanner
	{
		public abstract JobDef JobDef { get; }

		public abstract Predicate<VehiclePawn> VehicleCondition { get; }

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t.Faction != pawn.Faction)
			{
				return null;
			}
			if (t is VehiclePawn vehicle && !vehicle.vehiclePather.Moving && pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly) && VehicleCondition(vehicle))
			{
				VehicleReservationManager reservationManager = pawn.Map.GetCachedMapComponent<VehicleReservationManager>();
				if (reservationManager.CanReserve(vehicle, pawn, JobDef))
				{
					IntVec3 jobCell = vehicle.SurroundingCells.RandomOrFallback(cell => reservationManager.CanReserve<LocalTargetInfo, VehicleTargetReservation>(vehicle, pawn, cell), IntVec3.Invalid);
					if (jobCell.IsValid)
					{
						return JobMaker.MakeJob(JobDef, vehicle, jobCell);
					}
				}
			}
			return null;
		}
	}
}
