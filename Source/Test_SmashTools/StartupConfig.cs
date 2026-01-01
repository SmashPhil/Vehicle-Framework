using DevTools.Testing;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
internal static class StartupConfig
{
	static StartupConfig()
	{
		// Set test-specific states for main Vehicles project
		TestRunner.OnTestRunnerStateChange +=
			isRunningTests => TestWatcher.RunningTests = isRunningTests;
	}
}