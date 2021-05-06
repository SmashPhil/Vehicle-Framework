using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_Cargo : VehicleStatWorker
	{
		public VehicleStatWorker_Cargo()
		{
		}

		public override object Stat(VehiclePawn vehicle) => vehicle.CargoCapacity;

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
