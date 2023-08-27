using System;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class SettingsCache
	{
		private static readonly Dictionary<Pair<Type, string>, FieldInfo> cachedFieldInfos = new Dictionary<Pair<Type, string>, FieldInfo>();

		public static FieldInfo GetCachedField(this Type type, string name)
		{
			Pair<Type, string> typeInfo = new Pair<Type, string>(type, name);
			if (!cachedFieldInfos.TryGetValue(typeInfo, out FieldInfo field))
			{
				field = AccessTools.Field(type, name);
				cachedFieldInfos.Add(typeInfo, field);
			}
			return field;
		}

		public static bool TryGetValue<T>(VehicleDef def, FieldInfo field, out T value)
		{
			value = default;
			if (VehicleMod.settings.vehicles.fieldSettings.TryGetValue(def.defName, out var dict))
			{
				SaveableField saveableField = new SaveableField(def, field);
				if (dict.TryGetValue(saveableField, out SavedField<object> result))
				{
					value = (T)result.EndValue;
					return true;
				}
				return false;
			}
			Log.Error($"Unable to retrieve {field.Name} for {def.defName} in ModSettings. Is this field posted to settings?");
			return false;
		}

		public static T TryGetValue<T>(VehicleDef def, Type containingType, string fieldName, T fallback = default)
		{
			FieldInfo fieldInfo = GetCachedField(containingType, fieldName);
			if (fieldInfo is null)
			{
				Log.Error($"{fieldName} could not be found in CachedFields. Defaulting to defined fallback value.");
				return fallback;
			}
			if (!VehicleMod.ModifiableSettings)
			{
				return fallback;
			}
			Type objType = null;
			try
			{
				if (TryGetValue(def, fieldInfo, out object value))
				{
					objType = value.GetType();
					if (objType != typeof(T))
					{
						if (typeof(T).IsEnum)
						{
							return (T)value;
						}
						return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InstalledUICulture.NumberFormat);
					}
					return (T)value;
				}
			}
			catch (InvalidCastException ex)
			{
				Log.Error($"Cannot cast {fieldName} from {objType?.GetType().ToString() ?? "[Null]"} to {typeof(T)}.\nException=\"{ex.Message}\"");
			}
			return fallback;
		}

		public static float TryGetValue(VehicleDef def, StatDef statDef, float fallback)
		{
			throw new NotImplementedException("StatDef SettingsCache"); //This would have to be patched onto vanilla for the settings values to be retrieved.  This will need to be looked into later

			if (VehicleMod.settings.vehicles.vanillaStats.TryGetValue(def.defName, out var dict))
			{
				if (dict.TryGetValue(statDef.defName, out float value))
				{
					return value;
				}
			}
			return fallback;
		}

		public static float TryGetValue(VehicleDef def, VehicleStatDef statDef, float fallback)
		{
			if (VehicleMod.settings.vehicles.vehicleStats.TryGetValue(def.defName, out var dict))
			{
				if (dict.TryGetValue(statDef.defName, out float value))
				{
					return value;
				}
			}
			return fallback;
		}
	}
}
