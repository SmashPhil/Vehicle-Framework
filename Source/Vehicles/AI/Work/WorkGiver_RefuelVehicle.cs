using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_RefuelVehicle : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public virtual JobDef JobStandard
		{
			get
			{
				return JobDefOf_Vehicles.RefuelVehicle;
			}
		}

		public virtual JobDef JobAtomic
		{
			get
			{
				return JobDefOf_Vehicles.RefuelVehicleAtomic;
			}
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.Refuel);

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			// Assigned to flag for debugging
			bool flag = t is VehiclePawn vehicle && vehicle.CompFueledTravel != null && CanRefuel(pawn, vehicle, forced) && !vehicle.vPather.Moving;
			return flag;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Job job = null;
			if (t is VehiclePawn vehicle && vehicle.CompFueledTravel != null)
			{
				Thing t2 = vehicle.CompFueledTravel.ClosestFuelAvailable(pawn);
				if (t2 is null)
					return null;
				return JobMaker.MakeJob(JobDefOf_Vehicles.RefuelVehicle, vehicle, t2);
			}
			
			return job;
		}

		public static bool CanRefuel(Pawn pawn, VehiclePawn vehicle, bool forced = false)
		{
			var comp = vehicle.CompFueledTravel;
			if (comp is null || comp.FullTank)
			{
				return false;
			}
			if (vehicle.IsForbidden(pawn) || !pawn.CanReserve(vehicle, 1, -1, null, forced))
			{
				return false;
			}
			if (vehicle.Faction != pawn.Faction)
			{
				return false;
			}
			if (comp.ClosestFuelAvailable(pawn) is null)
			{
				JobFailReason.Is("NoFuelToRefuel".Translate(comp.Props.fuelType), null);
				return false;
			}
			return true;
		}
	}
}
