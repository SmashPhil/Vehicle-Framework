using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles;

public class WorkGiver_PackVehicle : WorkGiver_CarryToVehicle<TransferableOneWay>
{
	protected override bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
	{
		return vehicle.cargoToLoad.Count > 0;
	}

	protected override List<TransferableOneWay> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn)
	{
		return vehicle.cargoToLoad;
	}

	protected override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<TransferableOneWay> things)
	{
		return JobDriver_LoadVehicle.FindThingToPack(vehicle, pawn, things);
	}
}