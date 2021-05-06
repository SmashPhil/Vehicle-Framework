using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_FlightControl : VehicleStatWorker
	{
		public VehicleStatWorker_FlightControl()
		{
		}

		public override object Stat(VehiclePawn vehicle) => vehicle.CompVehicleLauncher?.FlySpeed ?? 0;

		public override void DrawVehicleStat(Listing_SplitColumns lister, VehiclePawn vehicle)
		{
			lister.Label(StatValueFormatted(vehicle));
		}

		public override string StatBuilderExplanation(VehiclePawn vehicle)
		{
			return statDef.description;
		}
	}
}
