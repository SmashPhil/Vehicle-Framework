using DevTools.Testing;
using Verse;

namespace SmashTools.UnitTesting;

[Disabled]
[UnitTest(TestType.MainMenu)]
[TestDescription("Synchronization util class for posting actions to the main thread.")]
[TestCategory(TestCategoryNames.Multithreading, TestCategoryNames.Utils)]
internal class UnitTest_UnityThread
{
	[Test]
	private void UpdateLoop()
	{
	}

	[Test]
	private void ExecuteMainThreadNonBlocking()
	{
	}

	[Test]
	private void ExecuteMainThreadBlocking()
	{
	}

	private static void TestMethod()
	{
		Expect.IsTrue(UnityData.IsInMainThread, "Executing From MainThread");
	}
}