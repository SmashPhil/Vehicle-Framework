using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Profile;

namespace SmashTools.Patching;

internal class Patch_Events : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(DefGenerator),
        nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
      prefix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnGenerateImpliedDefs)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Game),
        nameof(Game.Dispose)),
      prefix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnGameDisposing)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnGameDisposed)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MemoryUtility),
        nameof(MemoryUtility.ClearAllMapsAndWorld)),
      prefix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnWorldUnloading)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnWorldRemoved)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GameComponentUtility),
        nameof(GameComponentUtility.StartedNewGame)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnNewGame)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GameComponentUtility),
        nameof(GameComponentUtility.LoadedGame)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnLoadGame)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnMainMenu)));
  }
}