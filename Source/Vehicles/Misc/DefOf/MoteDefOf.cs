using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class MoteDefOf
	{
		public static ThingDef Mote_RocketExhaust;

		public static ThingDef Mote_RocketExhaust_Long;

		public static ThingDef Mote_RocketSmoke_Long;

		public static ThingDef Mote_RocketSmoke_Low;

		static MoteDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(MoteDefOf));
		}
	}
}
