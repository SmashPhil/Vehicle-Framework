using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools;

namespace Vehicles
{
	public abstract class ConditionalVehiclePatch : ConditionalPatch
	{
		public override string SourceId => VehicleHarmony.VehiclesUniqueId;
	}
}
