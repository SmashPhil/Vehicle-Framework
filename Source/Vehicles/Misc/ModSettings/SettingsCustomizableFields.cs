using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class SettingsCustomizableFields
	{
		static SettingsCustomizableFields()
		{
			if (VehicleMod.ModifiableSettings)
			{
				List<bool> successfulGenerations = new List<bool>();

				VehicleMod.PopulateCachedFields();
				foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefsListForReading)
				{
					bool fields = PopulateSaveableFields(def);
					bool upgrades = true;// PopulateSaveableUpgrades(def);
					successfulGenerations.Add(fields);
					successfulGenerations.Add(upgrades);
					if (!fields || !upgrades)
					{
						VehicleMod.settingsDisabledFor.Add(def.defName);
					}
				}
				if (!successfulGenerations.NullOrEmpty() && successfulGenerations.All(b => b == false))
				{
					Log.Error($"SaveableFields have failed for every VehicleDef. Consider turning off the ModifiableSettings option in the ModSettings to bypass customizable field generation. This will require a restart.");
				}
			}
		}

		public static bool PopulateSaveableFields(VehicleDef def, bool hardReset = false)
		{
			try
			{
				if (hardReset)
				{
					VehicleMod.settings.vehicles.fieldSettings.Remove(def.defName);
				}
				if (!VehicleMod.settings.vehicles.fieldSettings.TryGetValue(def.defName, out var currentDict))
				{
					VehicleMod.settings.vehicles.fieldSettings[def.defName] = new Dictionary<SaveableField, SavedField<object>>();
					currentDict = VehicleMod.settings.vehicles.fieldSettings[def.defName];
				}
				VehicleMod.settings.vehicles.defaultValues[def.defName] = new Dictionary<SaveableField, object>();
				IterateTypeFields(def, def.GetType(), def, ref currentDict);
				foreach (CompProperties props in def.comps)
				{
					IterateTypeFields(def, props.GetType(), props, ref currentDict);
				}
				//Redundancy only
				VehicleMod.settings.vehicles.fieldSettings[def.defName] = currentDict;
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to populate field settings for <text>{def.defName}</text>.\nException=\"{ex.Message}\"\nInnerException=\"{ex.InnerException}\"");
				return false;
			}
			return true;
		}

		public static bool PopulateSaveableUpgrades(VehicleDef def, bool hardReset = false)
		{
			try
			{
				if (hardReset)
				{
					VehicleMod.settings.upgrades.upgradeSettings.Remove(def.defName);
				}
				if (def.HasComp(typeof(CompUpgradeTree)))
				{
					if (!VehicleMod.settings.upgrades.upgradeSettings.TryGetValue(def.defName, out var currentUpgradeDict))
					{
						VehicleMod.settings.upgrades.upgradeSettings.Add(def.defName, new Dictionary<SaveableField, SavedField<object>>());
						currentUpgradeDict = VehicleMod.settings.upgrades.upgradeSettings[def.defName];
						foreach (UpgradeNode node in def.GetSortedCompProperties<CompProperties_UpgradeTree>().upgrades)
						{
							IterateUpgradeNode(def, node, ref currentUpgradeDict);
						}
						//Redundancy only
						VehicleMod.settings.upgrades.upgradeSettings[def.defName] = currentUpgradeDict;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to populate upgrade settings for {def.defName}. Exception=\"{ex.Message}\"\nInnerException=\"{ex.InnerException}\"");
				return false;
			}
			return true;
		}

		public static void IterateTypeFields(VehicleDef def, Type type, object obj, ref Dictionary<SaveableField, SavedField<object>> currentDict)
		{
			if (VehicleMod.cachedFields.TryGetValue(type, out List<FieldInfo> fields))
			{
				var dict = VehicleMod.settings.vehicles.fieldSettings[def.defName];
				var defaultValuesDict = VehicleMod.settings.vehicles.defaultValues[def.defName];

				foreach (FieldInfo field in fields)
				{
					if (field.TryGetAttribute<PostToSettingsAttribute>(out var settings) && settings.ParentHolder)
					{
						object value = field.GetValue(obj);
						if (field.FieldType.IsGenericType)
						{
							MethodInfo method = field.DeclaringType.GetMethod("ResolvePostToSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
							if (method != null)
							{
								object[] arguments = new object[] { def, currentDict };
								method.Invoke(obj, arguments);
								currentDict = (Dictionary<SaveableField, SavedField<object>>)arguments[1];
							}
							else
							{
								SmashLog.Error($"Unable to generate customizable setting <field>{field.Name}</field> for <text>{def.defName}</text>. Fields of type <type>Dictionary<T></type> must implement ResolvePostToSettings method to be manually resolved.");
							}
						}
						else
						{
							IterateTypeFields(def, field.FieldType, value, ref currentDict);
						}
					}
					else
					{
						SaveableField saveField = new SaveableField(def, field);
						defaultValuesDict[saveField] = field.GetValue(obj);
						//if (!dict.ContainsKey(saveField))
						//{
						//	dict.Add(saveField, new SavedField<object>(field.GetValue(obj)));
						//}
					}
				}
				//Redundancy sake.
				VehicleMod.settings.vehicles.fieldSettings[def.defName] = dict;
			}
		}

		public static void IterateUpgradeNode(VehicleDef def, UpgradeNode node, ref Dictionary<SaveableField, SavedField<object>> currentDict)
		{
			if (VehicleMod.cachedFields.TryGetValue(node.GetType(), out var fields))
			{
				var dict = VehicleMod.settings.upgrades.upgradeSettings[def.defName];
				foreach (FieldInfo field in fields)
				{
					if (field.TryGetAttribute<PostToSettingsAttribute>(out var settings) && settings.ParentHolder)
					{
						object value = field.GetValue(node);
						if (field.FieldType.IsGenericType)
						{
							MethodInfo method = field.DeclaringType.GetMethod("ResolvePostToSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
							if (method != null)
							{
								object[] arguments = new object[] { def, currentDict };
								method.Invoke(node, arguments);
								currentDict = (Dictionary<SaveableField, SavedField<object>>)arguments[1];
							}
							else
							{
								Log.Error($"Unable to generate customizable setting {field.Name} for {def.defName}. Fields of type Dictionary<> must implement ResolvePostToSettings method to be manually resolved.");
							}
						}
						else
						{
							IterateTypeFields(def, field.FieldType, value, ref currentDict);
						}
					}
					else
					{
						SaveableField saveField = new SaveableField(def, field);
						if (!dict.TryGetValue(saveField, out var _))
						{
							dict.Add(saveField, new SavedField<object>(field.GetValue(node)));
						}
					}
				}
				//Redundancy sake.
				VehicleMod.settings.upgrades.upgradeSettings[def.defName] = dict;
			}
		}
	}
}
