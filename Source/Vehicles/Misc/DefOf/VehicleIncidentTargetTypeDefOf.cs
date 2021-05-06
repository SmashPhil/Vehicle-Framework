using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public class VehicleIncidentTargetTypeDefOf
	{
		static VehicleIncidentTargetTypeDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleIncidentTargetTypeDefOf));
		}

		public static IncidentTargetTagDef WorldEvent_Forced;
	}
}
