using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_Armor : VehicleStatWorker
	{
		public VehicleStatWorker_Armor()
		{
		}

		public override object Stat(VehiclePawn vehicle) => vehicle.ArmorPoints;

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
