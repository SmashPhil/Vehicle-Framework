using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

[PublicAPI]
public static class Ext_Comp
{
  private static readonly AccessTools.FieldRef<ThingWithComps, List<ThingComp>> CompList =
    AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

  /// <summary>
  /// Adds <paramref name="comp"/> to <paramref name="thingWithComps"/> and inits inner 'comps' list if empty.
  /// </summary>
  public static bool TryAddComp<T>(this ThingWithComps thingWithComps, T comp) where T : ThingComp
  {
    try
    {
      CompList.Invoke(instance: thingWithComps) ??= [];
      thingWithComps.AllComps.Add(comp);
      return true;
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown while trying to add {comp.GetType()} to {thingWithComps}." +
                $"\nException={ex}");
    }
    return false;
  }

  /// <summary>
  /// Insert <paramref name="comp"/> to <paramref name="thingWithComps"/> and inits inner 'comps' list if empty.
  /// </summary>
  public static bool TryInsertComp<T>(this ThingWithComps thingWithComps, T comp, int index) where T : ThingComp
  {
    try
    {
      CompList.Invoke(instance: thingWithComps) ??= [];
      thingWithComps.AllComps.Insert(index, comp);
      return true;
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown while trying to reflectively add {comp.GetType()} to " +
                $"{thingWithComps}.\nException={ex}");
    }
    return false;
  }
}