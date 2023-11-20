using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class VehicleStatPart_Deployed : VehicleStatPart
	{
		public override float TransformValue(VehiclePawn vehicle, float value)
		{
			if (vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.Deployed)
			{
				return 0;
			}
			return value;
		}

		public override string ExplanationPart(VehiclePawn vehicle)
		{
			if (vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.Deployed)
			{
				return "VF_StatsReport_Deployed".Translate();
			}
			return null;
		}
	}
}
