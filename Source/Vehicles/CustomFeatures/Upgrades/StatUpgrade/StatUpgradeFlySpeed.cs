using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	public class StatUpgradeFlySpeed : StatUpgradeCategoryDef
	{
		public StatUpgradeFlySpeed()
		{
		}

		public override bool AppliesToVehicle(VehicleDef def) => def.HasComp(typeof(CompVehicleLauncher));

		public override void ApplyStatUpgrade(VehiclePawn vehicle, float value)
		{
			vehicle.CompVehicleLauncher.flightSpeedModifier += value;
		}

		public override void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
			throw new NotImplementedException();
		}
	}
}
