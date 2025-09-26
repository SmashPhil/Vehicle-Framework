using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

public class WorkGiver_RefuelVehicleTurret : WorkGiver_CarryToVehicle<ThingDefCountClass>
{
	public override string ReservationName => ReservationType.LoadTurret;

	public override JobDef JobDef => JobDefOf_Vehicles.CarryItemToVehicle;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadTurret);
	}

	// TODO 1.6.2091 - Stub to avoid breaking VehicleMapFramework patch.
	public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
	{
		return base.JobOnThing(pawn, thing, forced);
	}

	protected override bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
	{
		if (vehicle.CompVehicleTurrets == null)
			return false;
		if (vehicle.IsBurning() || vehicle.vehiclePather.Moving)
			return false;

		return !MassUtility.IsOverEncumbered(vehicle);
	}

	// TODO - Revisit when more granular menu is added
	protected override List<ThingDefCountClass> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn)
	{
		return JobDriver_GiveItemToVehicle.FindThingDefsToPack(vehicle, pawn)?.ToList();
	}

	protected override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<ThingDefCountClass> things)
	{
		return JobDriverGetItemForVehicleBase.FindThingToPack(vehicle, pawn, things);
	}
}