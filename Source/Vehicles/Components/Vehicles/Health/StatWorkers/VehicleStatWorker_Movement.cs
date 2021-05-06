using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_Movement : VehicleStatWorker
	{
		public VehicleStatWorker_Movement()
		{
		}

		public override object Stat(VehiclePawn vehicle) => vehicle.ActualMoveSpeed;

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
