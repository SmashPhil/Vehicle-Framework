using HarmonyLib;
using SmashTools.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Verse;

namespace SmashTools.Patching;

internal sealed class Patch_GlowGrid : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GlowGrid),
        nameof(GlowGrid.GlowGridUpdate_First)),
      prefix: new HarmonyMethod(typeof(Patch_GlowGrid),
        nameof(PrepareUpdateGlowers)),
      postfix: new HarmonyMethod(typeof(Patch_GlowGrid),
        nameof(UpdateGlowers)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GlowGrid),
        nameof(GlowGrid.LightBlockerAdded)),
      postfix: new HarmonyMethod(typeof(Patch_GlowGrid),
        nameof(BlockerChanged)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GlowGrid),
        nameof(GlowGrid.LightBlockerRemoved)),
      postfix: new HarmonyMethod(typeof(Patch_GlowGrid),
        nameof(BlockerChanged)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GlowGrid), nameof(GlowGrid.DirtyCell)),
      postfix: new HarmonyMethod(typeof(Patch_GlowGrid),
        nameof(RecordDirtyCell)));
  }

  private static void PrepareUpdateGlowers(Map ___map, NativeBitArray ___lightBlockers)
  {
    ___map.GetDetachedMapComponent<GlowGridExtender>().PreUpdate(___lightBlockers);
  }

  private static void UpdateGlowers(Map ___map, NativeArray<Color32> ___accumulatedGlow,
    NativeArray<Color32> ___accumulatedGlowNoCavePlants)
  {
    ___map.GetDetachedMapComponent<GlowGridExtender>().Update(___accumulatedGlow,
      ___accumulatedGlowNoCavePlants);
  }

  private static void BlockerChanged(IntVec3 __0, Map ___map)
  {
    ___map.GetDetachedMapComponent<GlowGridExtender>().BlockerChanged(__0);
  }

  private static void RecordDirtyCell(IntVec3 cell, Map ___map, UnsafeBitArray ___dirtyCells,
    bool ___hasRunInitially)
  {
    if (!___dirtyCells.IsCreated || !___hasRunInitially)
      return;

    ___map.GetDetachedMapComponent<GlowGridExtender>().RecordDirtyCell(cell);
  }
}