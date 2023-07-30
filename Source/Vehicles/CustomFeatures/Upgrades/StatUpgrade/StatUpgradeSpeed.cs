using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	public class StatUpgradeSpeed : StatUpgradeCategoryDef
	{
		public StatUpgradeSpeed()
		{
		}

		public override bool AppliesToVehicle(VehicleDef def) => true;

		public override void ApplyStatUpgrade(VehiclePawn vehicle, float value)
		{
			//vehicle.MoveSpeedModifier += value;
		}

		public override void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
			FloatRange? range = settingListerRange;
			if (range is null)
			{
				range = new FloatRange(-value, value);
			}
			lister.SliderLabeled(def, field, "VF_MaxSpeed".Translate(), "VF_MaxSpeedTooltip".Translate(), string.Empty, string.Empty, value + range.Value.min, value + range.Value.max, 1, -1, 0.1f);
		}
	}
}
