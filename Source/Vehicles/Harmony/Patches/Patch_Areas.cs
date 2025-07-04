using HarmonyLib;
using SmashTools;
using Verse;
using SmashTools.Patching;

namespace Vehicles;

internal class Patch_Areas : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(AreaManager), nameof(AreaManager.AddStartingAreas)),
      postfix: new HarmonyMethod(typeof(Patch_Areas),
        nameof(AddVehicleAreas)));
    // Back compatibility for maps that were not saved with these area types
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
      postfix: new HarmonyMethod(typeof(Patch_Areas),
        nameof(BackfillVehicleAreas)));
  }

  /// <summary>
  /// Create and add road areas to <see cref="AreaManager"/> while map is initializing.
  /// </summary>
  /// <remarks>This only applies to new maps.</remarks>
  private static void AddVehicleAreas(AreaManager __instance)
  {
    __instance.map.EnsureAreaInitialized<Area_Road>();
    __instance.map.EnsureAreaInitialized<Area_RoadAvoidal>();
  }

  /// <summary>
  /// Add road areas to <see cref="AreaManager"/> if they are missing.
  /// </summary>
  /// <remarks>This only applies to loaded maps for back compatibility.</remarks>
  private static void BackfillVehicleAreas(Map __instance)
  {
    __instance.EnsureAreaInitialized<Area_Road>();
    __instance.EnsureAreaInitialized<Area_RoadAvoidal>();
  }
}