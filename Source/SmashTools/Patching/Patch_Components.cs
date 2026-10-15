using HarmonyLib;
using Verse;

namespace SmashTools.Patching;

internal class Patch_Components : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.AddMap)),
      postfix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.PreCache)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Map), nameof(Map.ExposeData)),
      prefix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.PreCacheInst)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MapDeiniter),
        nameof(MapDeiniter.Deinit)),
      postfix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.ClearMap), [typeof(Map)]));
  }
}