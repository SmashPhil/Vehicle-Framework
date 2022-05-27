using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public abstract class VehicleStatPart
	{
		public VehicleStatDef statDef;

		public abstract float TransformValue(VehiclePawn vehicle, float value);

		public abstract string ExplanationPart(VehiclePawn vehicle);
	}
}
