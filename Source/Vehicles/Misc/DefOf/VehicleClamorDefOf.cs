using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class VehicleClamorDefOf
	{
		static VehicleClamorDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleClamorDefOf));
		}

		public static ClamorDef VF_Painting;
	}
}
