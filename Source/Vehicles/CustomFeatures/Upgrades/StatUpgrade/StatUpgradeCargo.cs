using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	public class StatUpgradeCargo : StatUpgradeCategoryDef
	{
		public StatUpgradeCargo()
		{
		}

		public override bool AppliesToVehicle(VehicleDef def) => true;

		public override void ApplyStatUpgrade(VehiclePawn vehicle, float value)
		{
			//vehicle.CargoCapacity += value;
		}

		public override void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
			FloatRange? range = settingListerRange;
			if (range is null)
			{
				range = new FloatRange(-value, value);
			}
			lister.FloatBox(def, field, "VF_CargoCapacity".Translate(), "VF_CargoCapacityTooltip".Translate(), string.Empty, value + range.Value.min, value + range.Value.max);
		}
	}
}
