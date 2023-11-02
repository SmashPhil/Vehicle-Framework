using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class ThingDefOf_VehicleMotes
	{
		static ThingDefOf_VehicleMotes()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf_VehicleMotes));
		}

		public static ThingDef Mote_FishingNet;
	}
}
