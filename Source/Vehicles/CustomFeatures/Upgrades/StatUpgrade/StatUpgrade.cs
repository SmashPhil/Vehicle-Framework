using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class StatUpgrade : UpgradeNode 
	{
		[PostToSettings(ParentHolder = true)]
		public Dictionary<StatUpgradeCategoryDef, float> values = new Dictionary<StatUpgradeCategoryDef, float>();

		public StatUpgrade()
		{
		}

		public StatUpgrade(VehiclePawn vehicle) : base(vehicle)
		{

		}

		public StatUpgrade(StatUpgrade reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			values = reference.values;
		}

		public override string UpgradeIdName => "StatUpgrade";

		public override int ListerCount => values.Count;

		public override void Upgrade()
		{
			try
			{
				foreach(KeyValuePair<StatUpgradeCategoryDef, float> stat in values)
				{
					stat.Key.ApplyStatUpgrade(vehicle, stat.Value);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to add stat values to {vehicle.LabelShort}. Report on workshop page. \nException: {ex}");
				return;
			}

			vehicle.VehicleDef.buildDef.soundBuilt?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
		}

		public override void Refund()
		{
			foreach(KeyValuePair<StatUpgradeCategoryDef, float> stat in values)
			{
				stat.Key.ApplyStatUpgrade(vehicle, -stat.Value);
			}
		}

		public override void SettingsWindow(VehicleDef def, Listing_Settings listing)
		{
			FieldInfo dictField = SettingsCache.GetCachedField(typeof(StatUpgrade), nameof(values));

			Rect buttonRect = listing.GetRect(16);
			buttonRect.x = buttonRect.width - 24;
			buttonRect.width = 24;
			buttonRect.height = 24;
			listing.Header(label, GameFont.Medium, TextAnchor.MiddleCenter);
			if (Widgets.ButtonImage(buttonRect, VehicleTex.ResetPage))
			{
				SettingsCustomizableFields.PopulateSaveableUpgrades(def, true);
			}
			listing.Gap();
			
			foreach (var statUpgrade in values)
			{
				SaveableDefPair<StatUpgradeCategoryDef> saveable = new SaveableDefPair<StatUpgradeCategoryDef>(def, dictField, string.Concat(statUpgrade.Key.defName, "_", upgradeID), statUpgrade.Key, LookMode.Value);
				statUpgrade.Key.DrawStatLister(def, listing, saveable, statUpgrade.Value);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref values, "values", LookMode.Def, LookMode.Value);
		}

		public void ResolvePostToSettings(VehicleDef def, ref Dictionary<SaveableField, SavedField<object>> currentDict)
		{
			FieldInfo dictField = SettingsCache.GetCachedField(typeof(StatUpgrade), nameof(values));
			foreach (var statUpgrade in values)
			{
				SaveableDefPair<StatUpgradeCategoryDef> saveable = new SaveableDefPair<StatUpgradeCategoryDef>(def, dictField, string.Concat(statUpgrade.Key.defName, "_", upgradeID), statUpgrade.Key, LookMode.Value);
				currentDict.Add(saveable, new SavedField<object>(statUpgrade.Value));
			}
		}
	}
}
