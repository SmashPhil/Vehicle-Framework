using DevTools;
using DevTools.UnitTesting;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
internal static class StartupConfig
{
  // Debugging with HarmonyMod's stack trace suppression for duplicates is a massive pain and for
  // some reason it isn't a mod setting, but rather a tweak value that has to be toggled off after
  // every startup! Just disable it permanently for debug builds, I'd rather not deal with this.
#if DEBUG
  private static readonly StackTraceCacheDisabler StcDisabler = new();
#endif

  static StartupConfig()
  {
    // Set test-specific states for main Vehicles project
    UnitTestManager.OnUnitTestStateChange +=
      isRunningTests => TestWatcher.RunningUnitTests = isRunningTests;
  }
}