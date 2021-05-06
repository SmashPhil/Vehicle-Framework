using System;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

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

		public static object TryGetValue(this VehicleDef def, FieldInfo field)
		{
			if (VehicleMod.settings.vehicles.fieldSettings.TryGetValue(def.defName, out var dict))
			{
				if (dict.TryGetValue(new SaveableField(def, field), out var result))
				{
					return result.Second;
				}
			}
			Log.Error($"Unable to retrieve {field.Name} for {def.defName} in ModSettings. Is this field posted to settings?");
			return default;
		}

		public static T TryGetValue<T>(this VehicleDef def, Type containingType, string fieldName, T fallback = default)
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
			object value = def.TryGetValue(fieldInfo);
			if (value is T castedValue)
			{
				return castedValue;
			}
			try
			{
				if (typeof(T).IsEnum)
				{
					return (T)value;
				}
				return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InstalledUICulture.NumberFormat);
			}
			catch (InvalidCastException ex)
			{
				Log.Error($"Cannot cast {fieldName} from {value.GetType()} to {typeof(T)}.\nException=\"{ex.Message}\"");
			}
			return fallback;
		}
	}
}
