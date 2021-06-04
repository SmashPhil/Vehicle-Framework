using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class ProjectileSkyfallerDefOf
	{
		public static ThingDef ProjectileSkyfaller;

		static ProjectileSkyfallerDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(ProjectileSkyfallerDefOf));
		}
	}
}
