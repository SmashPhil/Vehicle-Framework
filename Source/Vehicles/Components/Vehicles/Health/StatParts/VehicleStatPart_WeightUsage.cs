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
		public LinearCurve usageCurve;
		public OperationType operation = OperationType.Addition;
		public string formatString;

		public override float TransformValue(VehiclePawn vehicle, float value)
		{
			float modifier = 0;
			if (usageCurve != null)
			{
				float capacity = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
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
			float capacity = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			string weightUsage;
			if (formatString.NullOrEmpty())
			{
				weightUsage = string.Format(statDef.formatString, MassUtility.InventoryMass(vehicle), capacity);
			}
			else
			{
				weightUsage = string.Format(formatString, MassUtility.InventoryMass(vehicle), capacity);
			}
			return "VF_StatsReport_CargoWeight".Translate(weightUsage);
		}
	}
}
