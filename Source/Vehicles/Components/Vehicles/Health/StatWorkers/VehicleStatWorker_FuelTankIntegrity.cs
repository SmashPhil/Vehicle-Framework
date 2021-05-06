using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_FuelTankIntegrity : VehicleStatWorker
	{
		public VehicleStatWorker_FuelTankIntegrity()
		{
		}

		public override object Stat(VehiclePawn vehicle) => vehicle.CompFueledTravel.FuelPercent;

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
