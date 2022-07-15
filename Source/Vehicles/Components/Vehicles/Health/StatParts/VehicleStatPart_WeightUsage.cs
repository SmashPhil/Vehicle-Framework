using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class VehicleStatPart_WeightUsage : VehicleStatPart
	{
		public SimpleCurve overweightSpeedCurve;

		public override float TransformValue(VehiclePawn vehicle, float value)
		{
			float capacity = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			float usage = 0;
			if (capacity > 0)
			{
				usage = MassUtility.InventoryMass(vehicle) / capacity;
			}
			return value * overweightSpeedCurve.Evaluate(usage);
		}

		public override string ExplanationPart(VehiclePawn vehicle)
		{
			return "StatsReport_BaseValue".Translate();
		}
	}
}
