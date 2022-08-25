using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public class VehicleStatCategoryDefOf
	{
		public static StatCategoryDef VehicleBasics;
		public static StatCategoryDef VehicleBasicsImportant;
		public static StatCategoryDef VehicleCombat;
		public static StatCategoryDef VehicleAerial;

		static VehicleStatCategoryDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(StatCategoryDefOf));
		}
	}
}
