using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Vehicles
{
	public class StatUpgradeFuelCapacity : StatUpgradeCategoryDef
	{
		public StatUpgradeFuelCapacity()
		{
		}

		public override bool AppliesToVehicle(VehicleDef def) => def.HasComp(typeof(CompFueledTravel));

		public override void ApplyStatUpgrade(VehiclePawn vehicle, float value)
		{
			try
			{
				vehicle.CompFueledTravel.FuelCapacity += value;
			}
			catch(Exception ex)
			{
				Log.Error($"Failed to apply StatUpgrade {defName} to {vehicle?.LabelShort ?? "[Null]"}. Exception={ex.Message}");
			}
		}

		public override void DrawStatLister(VehicleDef def, Listing_Settings lister, SaveableField field, float value)
		{
			throw new NotImplementedException();
		}
	}
}
