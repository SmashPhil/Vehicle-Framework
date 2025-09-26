using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public abstract class WorkGiver_CarryToVehicle<T> : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public virtual string ReservationName => ReservationType.LoadVehicle;

	public virtual JobDef JobDef => JobDefOf_Vehicles.LoadVehicle;

	protected abstract List<T> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn);

	protected abstract Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<T> things);

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.GetCachedMapComponent<VehicleReservationManager>()
		 .VehicleListers(ReservationName);
	}

	protected virtual bool JobAvailable(VehiclePawn vehicle)
	{
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not VehiclePawn vehicle)
			return null;
		if (vehicle.IsForbidden(pawn))
			return null;
		if (!JobAvailable(vehicle))
			return null;

		List<T> things = GetThingsToLoad(vehicle, pawn);
		if (things.NullOrEmpty())
			return null;
		if (!pawn.CanReach(vehicle.Position, PathEndMode.Touch, Danger.Deadly))
			return null;

		Thing thing = FindThingToPack(vehicle, pawn, things);
		if (thing == null || thing == pawn || thing == vehicle)
			return null;

		return JobMaker.MakeJob(JobDef, thing, vehicle);
	}
}