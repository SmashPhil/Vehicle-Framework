using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public class VehicleIncidentDefOf
	{
		static VehicleIncidentDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleIncidentDefOf));
		}

		public static IncidentDef ShuttleCrashed;
	}
}
