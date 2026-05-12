using System;
using System.Threading;
using CoreLib.Performance;
using DevTools.Testing;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

// ReSharper disable AccessToDisposedClosure
namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_DeferredGridGeneration
{
  private const double MaxWaitTime = 10000; // ms

  // We're specifically testing thread enqueueing with deferred grid generation, we need the
  // dedicated thread unsuspended but blocked in order to test
  private ThreadEnabler threadEnabler;
  private VehiclePathingSystem mapping;

  private VehicleGroup group;
  private PathData pathData;

  [OneTimeSetUp]
  private void SetUpMap()
  {
    Assert.IsNotNull(Find.CurrentMap);
    mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
    Assert.IsNotNull(mapping.deferredGridGeneration);
    if (!mapping.ThreadAlive)
    {
      Test.Skip("Thread not available.");
      return;
    }
    Assert.IsNull(threadEnabler);
    threadEnabler = new ThreadEnabler();
    Assert.AreEqual(DedicatedThread.ThreadState.Running, mapping.dedicatedThread.State);
  }

  [OneTimeTearDown]
  private void CleanUpMap()
  {
    mapping.deferredGridGeneration.DoPassExpectClear();
    mapping.RegenerateGrids(deferment: VehiclePathingSystem.GridDeferment.Forced);
    mapping = null;

    threadEnabler.Dispose();
    threadEnabler = null;
  }

  [SetUp]
  private void CreateVehicle()
  {
    // Tests running through the full grid generation process need xml-loaded defs. Mock defs won't
    // have path data cached in VehiclePathingSystem.
    VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading
      .FirstOrDefault(PathingHelper.ShouldCreateRegions);
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      vehicleDef = vehicleDef,
      drivers = 1
    });
    Assert.IsTrue(PathingHelper.ShouldCreateRegions(group.vehicle.VehicleDef));
    pathData = mapping[group.vehicle.VehicleDef];

    mapping.deferredGridGeneration.DoPassExpectClear();
    Assert.IsTrue(pathData.Suspended);
    Assert.IsTrue(mapping.ThreadAvailable);
  }

  [TearDown]
  private void DestroyVehicle()
  {
    group.Dispose();
    group = null;
  }

  [Test]
  [TestDescription("Region is generated async through the dedicated thread.")]
  private void DeferredGeneration()
  {
    group.Spawn();
    using ManualResetEventSlim resetEvent = new(false);
    resetEvent.Reset();
    // We need to wait for the dedicated thread to finish generating vehicle's grids so we can
    // validate that the grid is initialized.
    AsyncLongOperationAction longOp = AsyncPool<AsyncLongOperationAction>.Get();
    longOp.OnInvoke += () => resetEvent.Set();
    mapping.dedicatedThread.Enqueue(longOp);
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsTrue(resetEvent.Wait(TimeSpan.FromMilliseconds(MaxWaitTime)));
    
    Expect.IsTrue(pathData.VehiclePathGrid.Enabled);
    Expect.IsTrue(pathData.VehicleRegionAndRoomUpdater.Enabled);
    Expect.IsFalse(pathData.Suspended);
  }

  [Test]
  [TestDescription("Npc vehicles generate urgently for incidents and responsive AI actions.")]
  private void NpcGeneratesUrgently()
  {
    // Block dedicated thread without flagging as suspended so we can still validate that grid
    // generation is not being sent to the thread for async processing. There are extra checks
    // in place that prevent enqueueing actions to a suspended DedicatedThread so this is the
    // only way we can both allow it and pause the DedicatedThread from processing any actions.
    using BlockThread block = new(mapping.dedicatedThread);
    Assert.IsNotNull(Find.World.factionManager.OfAncientsHostile);
    group.vehicle.SetFactionDirect(Find.World.factionManager.OfAncientsHostile);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.AreEqual(DeferredGridGeneration.Urgency.Urgent, DeferredGridGeneration.UrgencyFor(group.vehicle));
    Expect.IsTrue(pathData.VehicleRegionAndRoomUpdater.Enabled);
    Expect.IsTrue(pathData.VehiclePathGrid.Enabled);
    Expect.IsFalse(pathData.Suspended);
  }

  private class BlockThread : IDisposable
  {
    private readonly ManualResetEventSlim resetEvent;

    public BlockThread(DedicatedThread thread)
    {
      resetEvent = new ManualResetEventSlim(false);
      AsyncLongOperationAction blockingOp = AsyncPool<AsyncLongOperationAction>.Get();
      blockingOp.OnInvoke += () => resetEvent.Wait(TimeSpan.FromMilliseconds(MaxWaitTime));
      thread.Enqueue(blockingOp);
    }

    void IDisposable.Dispose()
    {
      resetEvent.Set();
    }
  }
}