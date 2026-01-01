using System;
using System.Collections;
using System.Threading;
using CoreLib.Performance;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace CoreLib.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Utility class for synchronization and temporary update loops.")]
[TestCategory(TestCategoryNames.Multithreading, TestCategoryNames.Performance)]
internal class UnitTest_UnityThread
{
  [Test]
  private void InMainThread()
  {
    // Unity only allows game objects to be instantiated on the main thread. This will throw
    // if we're not in the main thread.
    GameObject obj = new();
    Object.Destroy(obj);
    Assert.IsTrue(UnityThread.IsInMainThread);
  }

  [Test]
  private IEnumerator AddUpdater()
  {
    using MockUpdater updater = new();
    Assert.IsFalse(updater.invoked);
    yield return null;
    Assert.IsTrue(updater.invoked);
  }

  [Test]
  private IEnumerator AddOnGUI()
  {
    using MockOnGUI onGui = new();
    Assert.IsFalse(onGui.invoked);
    yield return null;
    Assert.IsTrue(onGui.invoked);
  }

  [Test]
  private IEnumerator RemoveUpdater()
  {
    using MockUpdater updater = new();
    Assert.IsFalse(updater.invoked);
    UnityThread.RemoveUpdate(updater.OnUpdate);
    yield return null;
    Assert.IsFalse(updater.invoked);
  }

  [Test]
  private IEnumerator RemoveOnGUI()
  {
    using MockOnGUI onGui = new();
    Assert.IsFalse(onGui.invoked);
    UnityThread.RemoveOnGUI(onGui.OnGUI);
    yield return null;
    Assert.IsFalse(onGui.invoked);
  }

  [Test]
  private IEnumerator ExecuteMainThread()
  {
    CrossThreadState cts = new();
    Thread thread = new(start: () =>
    {
      try
      {
        UnityThread.ExecuteOnMainThread(cts.CheckState);
      }
      catch (Exception ex)
      {
        Test.Fail($"Exception thrown in thread.\n{ex}");
      }
    });
    thread.Start();

    SpinWait.SpinUntil(condition: () => !thread.IsAlive, millisecondsTimeout: 500);

    // thread didn't wait for action queue to drain, state is still Invalid for this frame.
    Assert.AreEqual(CrossThreadState.State.NotInvoked, cts.state);

    // Skip 1 frame to process the action queue.
    yield return null;
    
    Assert.AreEqual(CrossThreadState.State.MainThread, cts.state);
  }

  [Test]
  private IEnumerator ExecuteMainThreadAndWait()
  {
    int startFrame = Time.frameCount;
    CrossThreadState cts = new();
    Thread thread = new(start: () =>
    {
      try
      {
        UnityThread.ExecuteOnMainThreadAndWait(cts.CheckState, waitTimeout: 1000);
        Assert.IsTrue(cts.frame > startFrame);
      }
      catch (Exception ex)
      {
        Test.Fail($"Exception thrown in thread.\n{ex}");
      }
    });
    thread.Start();

    // Skip 1 frame to process action queue
    yield return null;

    while (thread.IsAlive)
    {
      yield return null;
    }

    Assert.AreEqual(CrossThreadState.State.MainThread, cts.state);
  }

  private class CrossThreadState
  {
    public State state = State.NotInvoked;
    public int frame;

    public void CheckState()
    {
      state = UnityThread.IsInMainThread ? State.MainThread : State.ThreadPool;
      frame = Time.frameCount;
    }

    public enum State
    {
      NotInvoked,
      MainThread,
      ThreadPool
    }
  }

  private class MockUpdater : IDisposable
  {
    public bool invoked;

    public MockUpdater()
    {
      UnityThread.StartUpdate(OnUpdate);
    }

    public bool OnUpdate()
    {
      invoked = true;
      return false;
    }

    void IDisposable.Dispose()
    {
      UnityThread.RemoveUpdate(OnUpdate);
    }
  }

  private class MockOnGUI : IDisposable
  {
    public bool invoked;

    public MockOnGUI()
    {
      UnityThread.StartGUI(OnGUI);
    }

    public bool OnGUI()
    {
      invoked = true;
      return false;
    }

    void IDisposable.Dispose()
    {
      UnityThread.RemoveUpdate(OnGUI);
    }
  }
}