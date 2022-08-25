using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;
namespace Vehicles
{
	public class VehicleStatPart_WeightUsage : VehicleStatPart
	{
		public SimpleCurve usageCurve;
		public OperationType operation = OperationType.Addition;

		public override float TransformValue(VehiclePawn vehicle, float value)
		{
			float capacity = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			float modifier = 0;
			if (usageCurve != null)
			{
				if (capacity > 0)
				{
					modifier = MassUtility.InventoryMass(vehicle) / capacity;
				}
				modifier = usageCurve.Evaluate(modifier);
			}
			else
			{
				modifier = MassUtility.InventoryMass(vehicle);
			}
			return operation.Apply(value, modifier);
		}

		public override string ExplanationPart(VehiclePawn vehicle)
		{
			return "StatsReport_BaseValue".Translate();
		}
	}
}
