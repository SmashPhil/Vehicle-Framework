using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using SmashTools.Patching;
using SmashTools.Performance;
using CoreLib;
using CoreLib.Performance;
using SmashTools.Xml;
using UnityEngine.SceneManagement;
using Verse;

namespace SmashTools;

public class ProjectSetup : Mod
{
  public const string ProjectLabel = "SmashTools";

  public const string LogLabel = $"[{ProjectLabel}]";

  // TODO 1.7 - Remove and use universal harmony patcher for conditional patches
  public const string HarmonyId = "SmashPhil.SmashTools";

  public ProjectSetup(ModContentPack content) : base(content)
  {
    Logger.Link<RimWorldLogger>();
    HarmonyPatcher.Init(content);

    SceneManager.sceneLoaded += ThreadManager.OnSceneChanged;
    SceneManager.sceneLoaded += ComponentCache.OnSceneChange;

    GameEvent.OnWorldUnloading += ThreadManager.ReleaseAll;
    GameEvent.OnWorldUnloading += ComponentCache.ClearAll;

#if DEBUG
    GameEvent.OnNewGame += StartupTest.ExecuteNewGameTesting;
    GameEvent.OnLoadGame += StartupTest.ExecutePostLoadTesting;
    GameEvent.OnMainMenu += StartupTest.ExecuteOnStartupTesting;
#endif

    // Logging
#if DEBUG
    // Just removing brackets from stacktrace for clarity. Let's not force other modders to deal
    // with the performance hit of constant regex filtering in release builds.
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Log), nameof(Log.Message),
        parameters: [typeof(string)]),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogTranspiler)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Warning)),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogWarningTranspiler)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Error)),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogErrorTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextMessageDetailsTranspiler)));
#endif

    XmlParseHelper.RegisterParseTypes();
    HarmonyPatcher.Run(PatchSequence.Mod);
    HarmonyPatcher.Run(PatchSequence.Async);

    // Mod Init
    StaticConstructorOnModInit();
  }

  private static void StaticConstructorOnModInit()
  {
    foreach (Type type in GenTypes.AllTypesWithAttribute<StaticConstructorOnModInitAttribute>())
    {
      try
      {
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Exception thrown running constructor of type <type>{type}</type>. Ex=\"{ex}\"");
      }
    }
  }

  private class RimWorldLogger : Logger.ILogger
  {
    void Logger.ILogger.Message(string message)
    {
      if (!UnityThread.IsInMainThread && Prefs.DevMode)
      {
        UnityThread.ExecuteOnMainThread(() => Log.Message(message));
      }
      else
      {
        Log.Message(message);
      }
    }

    void Logger.ILogger.Warning(string message)
    {
      if (!UnityThread.IsInMainThread && Prefs.DevMode)
      {
        UnityThread.ExecuteOnMainThread(() => Log.Warning(message));
      }
      else
      {
        Log.Warning(message);
      }
    }

    void Logger.ILogger.Error(string message)
    {
      if (!UnityThread.IsInMainThread && Prefs.DevMode)
      {
        UnityThread.ExecuteOnMainThread(() => Log.Error(message));
      }
      else
      {
        Log.Error(message);
      }
    }
  }
}