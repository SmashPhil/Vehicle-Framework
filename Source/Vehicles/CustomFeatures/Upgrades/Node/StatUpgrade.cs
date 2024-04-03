using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class StatUpgrade : Upgrade
	{
		[PostToSettings(ParentHolder = true)]
		public Dictionary<VehicleStatDef, float> vehicleStats = new Dictionary<VehicleStatDef, float>();
		[PostToSettings(ParentHolder = true)]
		public Dictionary<StatUpgradeCategoryDef, float> statCategories = new Dictionary<StatUpgradeCategoryDef, float>();

		public override int ListerCount => vehicleStats.Count + statCategories.Count;

		public override bool UnlockOnLoad => true;

		public override void Unlock(VehiclePawn vehicle)
		{
			try
			{
				foreach ((VehicleStatDef statDef, float value) in vehicleStats)
				{
					vehicle.statHandler.AddStatOffset(statDef, value);
				}
				foreach ((StatUpgradeCategoryDef upgradeCategory, float value) in statCategories)
				{
					vehicle.statHandler.AddStatOffset(upgradeCategory, value);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} to {vehicle.LabelShort}. \nException: {ex}");
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			try
			{
				foreach ((VehicleStatDef statDef, float value) in vehicleStats)
				{
					vehicle.statHandler.RemoveStatOffset(statDef, value);
				}
				foreach ((StatUpgradeCategoryDef upgradeCategory, float value) in statCategories)
				{
					vehicle.statHandler.RemoveStatOffset(upgradeCategory, value);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to reset {GetType()} to {vehicle.LabelShort}. \nException: {ex}");
			}
		}
	}
}
