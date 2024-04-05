using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public class VehicleStatUpgradeCategoryDefOf
	{
		public static StatUpgradeCategoryDef FuelCapacity;
		public static StatUpgradeCategoryDef FuelEfficiency;
		public static StatUpgradeCategoryDef FuelConsumptionRate;
		public static StatUpgradeCategoryDef WorldSpeedMultiplier;
		public static StatUpgradeCategoryDef OffRoadEfficiency;
		public static StatUpgradeCategoryDef WinterPathCost;
		public static StatUpgradeCategoryDef PawnCollisionMultiplier;
		public static StatUpgradeCategoryDef PawnCollisionRecoilMultiplier;

		static VehicleStatUpgradeCategoryDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatUpgradeCategoryDefOf));
		}
	}
}
