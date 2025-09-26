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

	protected virtual bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
	{
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not VehiclePawn vehicle)
			return null;
		if (vehicle.IsForbidden(pawn))
			return null;
		// Vehicle itself must be on passable terrain, otherwise BFS search for turret and upgrade loading
		// will be unable to start (from the vehicle root position).
		if (!pawn.CanReach(vehicle.Position, PathEndMode.OnCell, Danger.Deadly))
			return null;
		if (!JobAvailable(pawn, vehicle))
			return null;

		List<T> things = GetThingsToLoad(vehicle, pawn);
		if (things.NullOrEmpty())
			return null;

		Thing thing = FindThingToPack(vehicle, pawn, things);
		if (thing == null || thing == pawn || thing == vehicle)
			return null;

		return JobMaker.MakeJob(JobDef, thing, vehicle);
	}
}