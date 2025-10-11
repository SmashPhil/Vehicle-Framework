using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles;

public class WorkGiver_PackVehicle : WorkGiver_CarryToVehicle<TransferableOneWay>
{
	protected override bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
	{
		if (vehicle.cargoToLoad.Count == 0)
			return false;

		return !MassUtility.IsOverEncumbered(vehicle);
	}

	protected override List<TransferableOneWay> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn)
	{
		return vehicle.cargoToLoad;
	}

	protected override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<TransferableOneWay> things)
	{
		return JobDriver_LoadVehicle.FindThingToPack(vehicle, pawn, JobDef, things);
	}
}