using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class ThingDefOf_Vehicles
	{
		static ThingDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf_Vehicles));
		}

		public static ThingDef Airdrop;
	}
}
