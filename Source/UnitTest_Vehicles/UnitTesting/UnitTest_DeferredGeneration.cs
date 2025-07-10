using System;
using System.Threading;
using DevTools.UnitTesting;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.UnitTesting.TestType;

// ReSharper disable AccessToDisposedClosure
namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_DeferredGeneration
{
  private const double MaxWaitTime = 10000; // ms

  private VehicleGroup group;
  private VehiclePathingSystem mapping;
  private VehiclePathingSystem.VehiclePathData pathData;
  private ManualResetEventSlim resetEvent;

  // We're specifically testing thread enqueueing with deferred grid generation, we need the
  // dedicated thread unsuspended but blocked in order to test
  private ThreadEnabler threadEnabler;

  [SetUp, ExecutionPriority(Priority.Last)]
  private void EnableThread()
  {
    Assert.IsNull(threadEnabler);
    threadEnabler = new ThreadEnabler();
  }

  [SetUp]
  private void JumpMap()
  {
    Assert.IsNotNull(Find.CurrentMap);
    VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading
     .FirstOrDefault(PathingHelper.ShouldCreateRegions);
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      vehicleDef = vehicleDef,
      drivers = 1
    });

    resetEvent = new ManualResetEventSlim(false);
    mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
    pathData = mapping[group.vehicle.VehicleDef];

    Assert.IsNotNull(mapping.deferredGridGeneration);
    if (!mapping.ThreadAlive)
      Test.Skip("Thread not available.");
  }

  [TearDown]
  private void RegenerateAllGrids()
  {
    group.Dispose();

    mapping.deferredGridGeneration.DoPassExpectClear();
    mapping.RegenerateGrids(deferment: VehiclePathingSystem.GridDeferment.Forced);

    resetEvent.Dispose();
    resetEvent = null;
    mapping = null;
    pathData = null;
  }

  [TearDown, ExecutionPriority(Priority.Last)]
  private void DisableThread()
  {
    threadEnabler.Dispose();
    threadEnabler = null;
  }

  [Test]
  private void PlayerDeferred()
  {
    Assert.AreEqual(mapping.dedicatedThread.State, DedicatedThread.ThreadState.Running);

    mapping.deferredGridGeneration.DoPassExpectClear();
    Assert.IsTrue(pathData.Suspended);

    group.Spawn();

    // Faction.OfPlayer
    Expect.IsTrue(group.vehicle.Spawned, "Spawned");
    Expect.AreEqual(DeferredGridGeneration.UrgencyFor(mapping.map, group.vehicle),
      DeferredGridGeneration.Urgency.Deferred, "Player Deferred");

    // We need to wait for the dedicated thread to finish generating vehicle's grids so we can
    // validate that every grid is initialized.
    AsyncLongOperationAction longOp = AsyncPool<AsyncLongOperationAction>.Get();
    longOp.OnInvoke += () => NotifyReadyToContinue(resetEvent);
    mapping.dedicatedThread.Enqueue(longOp);
    Assert.IsTrue(resetEvent.Wait(TimeSpan.FromMilliseconds(MaxWaitTime)));

    Expect.IsTrue(pathData.VehiclePathGrid.Enabled, "Player PathGrid Generated");
    Expect.IsTrue(pathData.VehicleRegionAndRoomUpdater.Enabled, "Player Regions Generated");
    Expect.IsFalse(pathData.Suspended, "Player PathData Suspended");

    group.vehicle.DeSpawn();
    Assert.IsFalse(group.vehicle.Spawned);
  }

  [Test]
  private void NpcImmediate()
  {
    mapping.deferredGridGeneration.DoPassExpectClear();
    Assert.IsTrue(pathData.Suspended);
    Assert.AreEqual(mapping.dedicatedThread.State, DedicatedThread.ThreadState.Running);

    // Block dedicated thread without flagging as suspended so we can still validate that grid
    // generation is not being sent to the thread for async processing. There are extra checks
    // in place that prevent enqueueing actions to a suspended DedicatedThread so this is the
    // only way we can both allow it and pause the DedicatedThread from processing any actions.
    resetEvent.Reset();
    AsyncLongOperationAction blockingOp = AsyncPool<AsyncLongOperationAction>.Get();
    blockingOp.OnInvoke += () => WaitForSignal(resetEvent);
    mapping.dedicatedThread.Enqueue(blockingOp);

    Assert.IsNotNull(Find.World.factionManager.OfAncientsHostile);
    group.vehicle.SetFactionDirect(Find.World.factionManager.OfAncientsHostile);
    group.Spawn();
    Expect.IsTrue(group.vehicle.Spawned, "Enemy Spawned");
    Expect.AreEqual(DeferredGridGeneration.UrgencyFor(mapping.map, group.vehicle),
      DeferredGridGeneration.Urgency.Urgent, "Enemy Spawn Urgent");

    Expect.IsTrue(pathData.VehicleRegionAndRoomUpdater.Enabled, "Enemy Regions Generated");
    Expect.IsTrue(pathData.VehiclePathGrid.Enabled, "Enemy PathGrid Generated");
    Expect.IsFalse(pathData.Suspended, "Enemy PathData Suspended");

    // Unblock dedicated thread
    resetEvent.Set();
  }

  private static void WaitForSignal(ManualResetEventSlim mre)
  {
    mre.Wait(TimeSpan.FromMilliseconds(MaxWaitTime));
  }

  private static void NotifyReadyToContinue(ManualResetEventSlim mre)
  {
    mre.Set();
  }
}