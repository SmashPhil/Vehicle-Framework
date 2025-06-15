using DevTools.UnitTesting;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_MapDeinit
{
  [Test]
  private void ReleaseThreads()
  {
    Assert.IsNotNull(Current.Game);
    Assert.IsNotNull(Find.CurrentMap);

    VehiclePathingSystem mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
    Assert.IsNotNull(mapping);

    // Create a few threads to validate that cleanup occurs
    ThreadManager.CreateNew();
    ThreadManager.CreateNew();
    ThreadManager.CreateNew();

    // Threads have been registered in thread manager.
    Expect.IsFalse(ThreadManager.AllThreadsTerminated, "Threads created.");

    // Validate all threads terminate and Thread::Join wait handles don't time out.
    ThreadManager.ReleaseThreads();
    Expect.IsTrue(ThreadManager.AllThreadsTerminated, "Threads terminated.");
  }
}