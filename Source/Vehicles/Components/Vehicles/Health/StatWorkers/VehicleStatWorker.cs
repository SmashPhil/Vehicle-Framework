using System.Collections.Generic;
using UnityEngine;
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

		public virtual float DrawVehicleStat(Rect leftRect, float curY, VehiclePawn vehicle)
		{
			Rect rect = new Rect(0f, curY, leftRect.width, 20f);
			if (Mouse.IsOver(rect))
			{
				GUI.color = TexData.HighlightColor;
				GUI.DrawTexture(rect, TexUI.HighlightTex);
			}
			GUI.color = Color.white;
			Widgets.Label(new Rect(0f, curY, leftRect.width * 0.65f, 30f), statDef.LabelCap);
			float baseValue = GetBaseValue(vehicle.VehicleDef);
			Color effColor = baseValue == 0 ? TexData.WorkingCondition : VehicleComponent.gradient.Evaluate(GetValue(vehicle) / baseValue);
			Widgets.Label(new Rect(leftRect.width * 0.65f, curY, leftRect.width * 0.35f, 30f), StatValueFormatted(vehicle).Colorize(effColor));
			Rect rect2 = new Rect(0f, curY, leftRect.width, 20f);
			if (Mouse.IsOver(rect2))
			{
				TooltipHandler.TipRegion(rect2, new TipSignal(TipSignal(vehicle), vehicle.thingIDNumber ^ statDef.index));
			}
			curY += 20f;
			return curY;
		}

		public virtual string StatValueFormatted(VehiclePawn vehicle)
		{
			string output = GetValue(vehicle).ToStringByStyle(statDef.toStringStyle);
			if (!statDef.formatString.NullOrEmpty())
			{
				output = string.Format(statDef.formatString, output);
			}
			return output;
		}

		public virtual string TipSignal(VehiclePawn vehicle)
		{
			return string.Empty;
		}

		public virtual string StatBuilderExplanation(VehiclePawn vehicle)
		{
			return statDef.description;
		}
	}
}
