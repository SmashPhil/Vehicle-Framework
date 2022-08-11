using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class HediffDefOf_Vehicles
	{
		public static HediffDef VF_Drowning;

		static HediffDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf_Vehicles));
		}
	}
}
