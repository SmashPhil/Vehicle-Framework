using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class PatternDefOf
	{
		public static PatternDef Default;

		static PatternDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(PatternDefOf));
		}
	}
}
