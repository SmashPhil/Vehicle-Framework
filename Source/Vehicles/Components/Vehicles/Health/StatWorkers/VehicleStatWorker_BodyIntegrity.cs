using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatWorker_BodyIntegrity : VehicleStatWorker
	{
		public VehicleStatWorker_BodyIntegrity() : base()
		{
		}

		public override float TransformValue(VehiclePawn vehicle, float value)
		{
			float current = 0;
			float total = 0;
			foreach (VehicleComponent component in vehicle.statHandler.components)
			{
				current += component.health;
				total += component.props.health;
			}
			if (total <= 0)
			{
				throw new InvalidOperationException($"Total health of VehicleDef {vehicle.VehicleDef} is less than or equal to 0.");
			}
			value = current / total;
			return base.TransformValue(vehicle, value);
		}
	}
}
