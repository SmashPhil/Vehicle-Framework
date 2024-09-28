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
		public static StatCategoryDef VehicleAerial;
		public static StatCategoryDef VehicleRefuelable;
		public static StatCategoryDef VehicleTurrets;

		static VehicleStatCategoryDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(StatCategoryDefOf));
		}
	}
}
