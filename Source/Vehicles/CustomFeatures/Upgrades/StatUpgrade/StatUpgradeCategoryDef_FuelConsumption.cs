using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	public class StatUpgradeCategoryDef_FuelConsumption : StatUpgradeCategoryDef
	{
		public StatUpgradeCategoryDef_FuelConsumption()
		{
		}

		public override void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
			throw new NotImplementedException();
		}
	}
}
