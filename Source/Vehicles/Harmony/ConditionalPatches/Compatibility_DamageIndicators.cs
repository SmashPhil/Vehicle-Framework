using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_DamageIndicators : ConditionalVehiclePatch
{
  public static Action<float, Map, Vector3, string> throwDamageMote;

  /// <summary>
  /// Static helper for caching the load status of this mod
  /// </summary>
  public static bool ModLoaded { get; private set; }

  public override string PackageId => ModPackageIds.DamageIndicators;

  public override void PatchAll(ModMetaData mod, Harmony instance)
  {
    ModLoaded = true;
    Type classType = GenTypes.GetTypeInAnyAssembly("DamageMotes.DamageMotes_Patch");
    MethodInfo method = AccessTools.Method(classType, "ThrowDamageMote");
    throwDamageMote = (Action<float, Map, Vector3, string>)
      Delegate.CreateDelegate(typeof(Action<float, Map, Vector3, string>), method, throwOnBindFailure: true);
  }
}