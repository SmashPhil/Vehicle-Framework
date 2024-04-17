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
		//Fuel
		public static StatUpgradeCategoryDef FuelCapacity;
		public static StatUpgradeCategoryDef FuelConsumptionRate;
		public static StatUpgradeCategoryDef ChargeRate;
		public static StatUpgradeCategoryDef DischargeRate;
		
		//World Map
		public static StatUpgradeCategoryDef WorldSpeedMultiplier;
		public static StatUpgradeCategoryDef OffRoadMultiplier;
		public static StatUpgradeCategoryDef WinterCostMultiplier;
		
		//Combat
		public static StatUpgradeCategoryDef PawnCollisionMultiplier;
		public static StatUpgradeCategoryDef PawnCollisionRecoilMultiplier;

		static VehicleStatUpgradeCategoryDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStatUpgradeCategoryDefOf));
		}
	}
}
