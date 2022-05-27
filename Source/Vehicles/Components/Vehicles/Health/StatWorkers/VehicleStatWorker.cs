using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker
	{
		public VehicleStatDef statDef;

		protected Dictionary<VehicleDef, float> baseValues;

		public VehicleStatWorker()
		{
		}

		public virtual void InitStatWorker(VehicleStatDef statDef)
		{
			this.statDef = statDef;
			baseValues = new Dictionary<VehicleDef, float>();
		}

		public virtual float GetValue(VehiclePawn vehicle)
		{
			float value = GetBaseValue(vehicle.VehicleDef);
			value = TransformValue(vehicle, value);
			value *= StatEfficiency(vehicle);
			return value.Clamp(statDef.minValue, statDef.maxValue);
		}

		public float RecacheBaseValue(VehicleDef vehicleDef)
		{
			float value = statDef.defaultBaseValue;
			foreach (VehicleStatModifier statModifier in vehicleDef.vehicleStats)
			{
				if (statModifier.statDef == statDef)
				{
					value = statModifier.value; //TODO - Retrieve modified value from ModSettings, use statModifier value as fallback
					break;
				}
			}
			baseValues[vehicleDef] = value;
			return value;
		}

		public virtual float GetBaseValue(VehicleDef vehicleDef)
		{
			if (baseValues.TryGetValue(vehicleDef, out float value))
			{
				return value;
			}
			return RecacheBaseValue(vehicleDef);
		}

		public virtual float TransformValue(VehiclePawn vehicle, float value)
		{
			if (!statDef.parts.NullOrEmpty())
			{
				foreach (VehicleStatPart statPart in statDef.parts)
				{
					value = statPart.TransformValue(vehicle, value);
				}
			}
			return value;
		}

		public float StatEfficiency(VehiclePawn vehicle)
		{
			return vehicle.statHandler.StatEfficiency(statDef);
		}

		public virtual void DrawVehicleStat(Listing_SplitColumns lister, VehiclePawn vehicle)
		{
			lister.Label(StatValueFormatted(vehicle));
		}

		public virtual string StatValueFormatted(VehiclePawn vehicle)
		{
			string output = GetValue(vehicle).ToString();
			if (!statDef.formatString.NullOrEmpty())
			{
				output = string.Format(statDef.formatString, output);
			}
			return output;
		}

		public virtual string StatBuilderExplanation(VehiclePawn vehicle)
		{
			return statDef.description;
		}
	}
}
