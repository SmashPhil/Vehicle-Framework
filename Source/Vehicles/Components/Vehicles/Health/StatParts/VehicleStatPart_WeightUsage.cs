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
			return value * overweightSpeedCurve.Evaluate(MassUtility.InventoryMass(vehicle) / vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity));
		}

		public override string ExplanationPart(VehiclePawn vehicle)
		{
			return "StatsReport_BaseValue".Translate();
		}
	}
}
