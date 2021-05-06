using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class AntiAircraftDefOf
	{
		static AntiAircraftDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(AntiAircraftDefOf));
		}

		public static AntiAircraftDef FlakProjectile;
	}
}
