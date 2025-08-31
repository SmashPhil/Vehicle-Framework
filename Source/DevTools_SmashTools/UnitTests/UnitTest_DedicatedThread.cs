using System;
using System.Threading;
using DevTools.Testing;
using SmashTools.Performance;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription(
  "DedicatedThread execution, wait handles, and validation for no polling-like behavior.")]
internal class UnitTest_DedicatedThread
{
  private const int ThreadJoinTimeout = 5000;
  private const int WaitTime = 1000;
  private const int ItemWorkMS = WaitTime / 10;

  private DedicatedThread dedicatedThread;
  private ManualResetEventSlim resetEvent;

  [SetUp]
  private void CreateThread()
  {
    Assert.IsNull(dedicatedThread);
    dedicatedThread = ThreadManager.CreateNew();
    Assert.IsNull(resetEvent);
    resetEvent = new ManualResetEventSlim(false);
  }

  [TearDown]
  private void DisposeThread()
  {
    if (!dedicatedThread.IsTerminated)
      dedicatedThread.Release();
    dedicatedThread = null;
    resetEvent.Dispose();
    resetEvent = null;
  }

  [Test]
  private void Dispatcher()
  {
    // No signal should be received, it should've already entered a blocked state while waiting 
    // for an item to enqueue.
    Assert.IsTrue(dedicatedThread.IsBlocked, "No Polling");

    // NOTE - Yes this may seem a little dubious but we're using the wait handle to run this test
    // synchronously specifically so we can verify that this thread is processing items correctly.
    // That means signaling the thread to resume execution, but then wait for us to finish testing.
    AsyncLongOperationAction pollingOp = AsyncPool<AsyncLongOperationAction>.Get();
    pollingOp.OnValidate += () => !resetEvent.WaitHandle.SafeWaitHandle.IsClosed;
    pollingOp.OnInvoke += () => SleepThread(ItemWorkMS, resetEvent: resetEvent);
    dedicatedThread.Enqueue(pollingOp);

    // Signal should be received this time, enqueueing item will set the event handler and resume
    // the thread's execution.
    Expect.IsFalse(dedicatedThread.IsBlocked, "Execution Resumed");

    Expect.IsTrue(resetEvent.Wait(TimeSpan.FromMilliseconds(WaitTime)), "WaitHandle Execution");
    resetEvent.Reset();

    Assert.AreEqual(dedicatedThread.QueueCount, 0);
    Expect.IsTrue(dedicatedThread.IsBlocked, "Execution Waiting");
  }

  [Test]
  private void Suspend()
  {
    // Suspend will finish executing queue and block new items from being enqueued whilst still
    // keeping the thread alive for future work after being unsuspended.
    EnqueueWorkItems(dedicatedThread, resetEvent);
    Assert.IsTrue(dedicatedThread.QueueCount > 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);
    dedicatedThread.Suspend();
    Expect.AreEqual(dedicatedThread.QueueCount, 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);
    Assert.IsTrue(dedicatedThread.IsSuspended);
    Expect.Throws<InvalidOperationException>(() => dedicatedThread.Enqueue(null));
    dedicatedThread.Unsuspend();
    // It will take a cycle or two for the thread to jump back to the top of the execution loop and reset.
    // For testing purposes (and to avoid any potential race conditions) we can busy wait those few cycles.
    SpinWait sw = new();
    while (!dedicatedThread.IsBlocked)
      sw.SpinOnce();
    Expect.AreEqual(dedicatedThread.QueueCount, 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);
    Assert.IsFalse(dedicatedThread.IsSuspended);
    // Does nothing since we've already unsuspended the thread.
    dedicatedThread.Unsuspend();
    ThreadManager.ReleaseAndJoin(dedicatedThread);
    Assert.IsTrue(dedicatedThread.IsTerminated);
    Expect.Throws<InvalidOperationException>(dedicatedThread.Unsuspend);
    dedicatedThread = ThreadManager.CreateNew();
  }

  [Test, ExecutionPriority(Priority.BelowNormal)]
  private void StopGracefully()
  {
    if (dedicatedThread.IsTerminated)
      dedicatedThread = ThreadManager.CreateNew();
    // Should never start suspended or work will never be dispatched
    Assert.IsFalse(dedicatedThread.IsSuspended);
    EnqueueWorkItems(dedicatedThread, resetEvent: resetEvent);
    Expect.IsTrue(dedicatedThread.QueueCount > 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);

    // Stop will send an event to the wait handle to resume so that it may exit
    dedicatedThread.Stop();
    // Allow WaitTime limit for each item in queue, but it should take nowhere near this long.
    Expect.IsTrue(dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)),
      "WaitHandle Stop Gracefully");
    resetEvent.Reset();

    Expect.AreEqual(dedicatedThread.QueueCount, 0, "Stop Gracefully Queue Empty");
    Expect.IsTrue(dedicatedThread.IsTerminated, "Stop Gracefully Terminated");
    dedicatedThread.Release();
  }

  [Test, ExecutionPriority(Priority.Last)]
  private void StopImmediately()
  {
    if (dedicatedThread.IsTerminated)
      dedicatedThread = ThreadManager.CreateNew();
    Assert.IsNotNull(dedicatedThread);
    EnqueueWorkItems(dedicatedThread, resetEvent: resetEvent);
    Expect.IsTrue(dedicatedThread.QueueCount > 0);
    Assert.IsTrue(dedicatedThread.IsBlocked);

    // Stop will send an event to the wait handle to resume so that it may exit
    dedicatedThread.StopImmediately();
    Expect.IsTrue(dedicatedThread.thread.Join(TimeSpan.FromMilliseconds(ThreadJoinTimeout)),
      "WaitHandle Stop Immediately");
    resetEvent.Reset();

    Expect.IsTrue(dedicatedThread.QueueCount > 0, "Stop Immediately Queue Not Empty");
    Expect.IsTrue(dedicatedThread.IsTerminated, "Stop Immediately Terminated");
    dedicatedThread.Release();
  }

  [Test]
  private void ReleaseAll()
  {
    const int SharedThreadId = 99;

    _ = ThreadManager.CreateNew();
    _ = ThreadManager.GetOrCreateShared(SharedThreadId);
    _ = ThreadManager.GetOrCreateShared(SharedThreadId);
    DedicatedThread suspendedThread = ThreadManager.GetOrCreateShared(SharedThreadId);
    // Will take a hundred microseconds to spin up the thread, we must wait until then.
    SpinWait sw = new();
    while (suspendedThread.State == DedicatedThread.ThreadState.Uninitialized)
      sw.SpinOnce();
    suspendedThread.Suspend();
    Assert.IsTrue(suspendedThread.IsSuspended);

    // Threads have been registered in thread manager.
    Expect.IsFalse(ThreadManager.AllThreadsTerminated, "Threads created.");

    // Validate all threads terminate and Thread::Join wait handles don't time out.
    ThreadManager.ReleaseAll();
    Expect.IsTrue(ThreadManager.AllThreadsTerminated, "Threads terminated.");
  }

  private static void EnqueueWorkItems(DedicatedThread thread, ManualResetEventSlim resetEvent)
  {
    AsyncLongOperationAction workOp;
    for (int i = 0; i < 3; i++)
    {
      workOp = AsyncPool<AsyncLongOperationAction>.Get();
      workOp.OnInvoke += () => SleepThread(ItemWorkMS);
      thread.EnqueueSilently(workOp);
    }

    // Set wait handle in the last one so we can resume test execution
    workOp = AsyncPool<AsyncLongOperationAction>.Get();
    workOp.OnInvoke += () => SleepThread(ItemWorkMS, resetEvent: resetEvent);
    thread.EnqueueSilently(workOp);
  }

  private static void SleepThread(int waitTime, ManualResetEventSlim resetEvent = null)
  {
    // Simulate work so we can validate that consumer thread has unblocked
    Thread.Sleep(waitTime);
    resetEvent?.Set();
  }
}