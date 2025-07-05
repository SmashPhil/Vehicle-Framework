using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using Verse;

namespace Vehicles;

public static class SettingsCache
{
  private static readonly Dictionary<Pair<Type, string>, FieldInfo> cachedFieldInfos = [];

  public static FieldInfo GetCachedField(this Type type, string name)
  {
    Pair<Type, string> typeInfo = new(type, name);
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
    if (VehicleMod.settings.vehicles.fieldSettings.TryGetValue(def.defName,
      out Dictionary<SaveableField, SavedField<object>> dict))
    {
      SaveableField saveableField = new(def, field);
      if (dict.TryGetValue(saveableField, out SavedField<object> result))
      {
        value = (T)result.EndValue;
        return true;
      }
      return false;
    }
    // Only unit tests should be creating transient vehicle defs
    if (!TestWatcher.RunningUnitTests)
      Trace.Fail(
        $"{def.defName} has not been cached in ModSettings.");
    return false;
  }

  public static T TryGetValue<T>(VehicleDef def, Type containingType, string fieldName,
    T fallback = default)
  {
    if (!VehicleMod.ModifiableSettings)
      return fallback;
    FieldInfo fieldInfo = GetCachedField(containingType, fieldName);
    if (fieldInfo is null)
    {
      Trace.Fail(
        $"{fieldName} could not be found in CachedFields. Defaulting to defined fallback value.");
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
          return (T)Convert.ChangeType(value, typeof(T),
            CultureInfo.InstalledUICulture.NumberFormat);
        }
        return (T)value;
      }
    }
    catch (InvalidCastException ex)
    {
      Log.Error(
        $"Cannot cast {fieldName} from {objType?.ToString() ?? "[Null]"} to {typeof(T)}.\nException=\"{ex}\"");
    }
    return fallback;
  }

  public static float TryGetValue(VehicleDef def, VehicleStatDef statDef, float fallback)
  {
    if (VehicleMod.settings.vehicles.vehicleStats.TryGetValue(def.defName,
      out Dictionary<string, float> dict))
    {
      if (dict.TryGetValue(statDef.defName, out float value))
      {
        return value;
      }
    }
    return fallback;
  }
}