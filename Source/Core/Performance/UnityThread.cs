using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;

namespace CoreLib.Performance;

/// <summary>
/// Provides a Unity MonoBehaviour to marshal actions onto the main thread
/// and maintain per-frame and per-GUI delegates.
/// </summary>
[PublicAPI]
public sealed class UnityThread : MonoBehaviour
{
  private static int mainThreadId;

  private readonly List<OnUpdate> onUpdateMethods = [];
  private readonly List<OnGui> onGuiMethods = [];

  private readonly ConcurrentQueue<Action> actionQueue = [];

  /// <summary>
  /// Delegate invoked each frame during Update.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> if <see cref="OnUpdate"/> should remain in queue for the next frame.
  /// <see langword="false"/> if it should be dequeued immediately.
  /// </returns>
  public delegate bool OnUpdate();

  /// <summary>
  /// Delegate invoked each GUI event during OnGUI.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> if <see cref="OnGui"/> should remain in queue for the next event.
  /// <see langword="false"/> if it should be dequeued immediately.
  /// </returns>
  public delegate bool OnGui();

  static UnityThread()
  {
    Instance = InjectToScene();
  }

  private static UnityThread Instance { get; }

  public static bool IsInMainThread => mainThreadId == Thread.CurrentThread.ManagedThreadId;

  internal static int ActionQueueCount => Instance ? Instance.actionQueue.Count : 0;

  private void Awake()
  {
    mainThreadId = Thread.CurrentThread.ManagedThreadId;
  }

  private void Update()
  {
    while (actionQueue.TryDequeue(out Action action))
    {
      try
      {
        action();
      }
      catch (Exception ex)
      {
        Logger.Error($"Exception thrown executing action from UnityThread.\n{ex}");
      }
    }

    for (int i = onUpdateMethods.Count - 1; i >= 0; i--)
    {
      if (!onUpdateMethods[i]())
        onUpdateMethods.RemoveAt(i);
    }
  }

  private void OnGUI()
  {
    for (int i = onGuiMethods.Count - 1; i >= 0; i--)
    {
      try
      {
        if (!onGuiMethods[i]())
          onGuiMethods.RemoveAt(i);
      }
      catch (Exception ex)
      {
        onGuiMethods.RemoveAt(i);
        Logger.Error($"Exception thrown from OnGUI.{Environment.NewLine}{ex}");
      }
    }
  }

  /// <summary>
  /// Removes a previously enqueued OnUpdate delegate. Must be called from the main thread.
  /// </summary>
  /// <param name="onUpdate">The delegate to remove.</param>
  public static void RemoveUpdate(OnUpdate onUpdate)
  {
    if (!IsInMainThread)
    {
      Trace.Fail(
        "Trying to remove update method to queue from another thread. This can only be done from the main thread.");
      return;
    }
    Instance.onUpdateMethods.Remove(onUpdate);
  }

  /// <summary>
  /// Enqueues an OnUpdate delegate to run once per frame. Must be called from the main thread.
  /// </summary>
  /// <param name="onUpdate">The delegate to enqueue.</param>
  public static void StartUpdate(OnUpdate onUpdate)
  {
    if (!IsInMainThread)
    {
      Trace.Fail(
        "Trying to add update method to queue from another thread. This can only be done from the main thread.");
      return;
    }
    Instance.onUpdateMethods.Add(onUpdate);
  }

  /// <summary>
  /// Enqueues an OnGui delegate to run each GUI event. Must be called from the main thread.
  /// </summary>
  /// <param name="onGui">The delegate to enqueue.</param>
  public static void StartGui(OnGui onGui)
  {
    if (!IsInMainThread)
    {
      Trace.Fail(
        "Trying to add OnGUI method to queue from another thread. This can only be done from the main thread.");
      return;
    }
    Instance.onGuiMethods.Add(onGui);
  }

  /// <summary>
  /// Removes a previously enqueued OnGui delegate.
  /// Must be called from the main thread.
  /// </summary>
  /// <param name="onGui">The delegate to remove.</param>
  public static void RemoveOnGui(OnGui onGui)
  {
    if (!IsInMainThread)
    {
      Trace.Fail(
        "Trying to remove OnGUI method to queue from another thread. This can only be done from the main thread.");
      return;
    }
    Instance.onGuiMethods.Remove(onGui);
  }

  /// <summary>
  /// Posts an action to run on the main thread. Can be invoked from background threads.
  /// </summary>
  /// <param name="action">Action to execute.</param>
  /// <exception cref="ArgumentNullException">If <paramref name="action"/> is null.</exception>
  public static void ExecuteOnMainThread(Action action)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));

    if (IsInMainThread)
    {
      action();
      return;
    }
    Instance.actionQueue.Enqueue(action);
  }

  /// <summary>
  /// Posts an action to run on the main thread, blocking until completion or timeout.
  /// </summary>
  /// <param name="waitTimeout">Milliseconds to wait before timing out.</param>
  /// <param name="action">Action to execute.</param>
  /// <exception cref="ArgumentNullException">If <paramref name="action"/> is null or empty.</exception>
  /// <exception cref="ArgumentException">If <paramref name="waitTimeout"/> is not positive.</exception>
  /// <exception cref="AssertionException">If the wait timed out.</exception>
  public static void ExecuteOnMainThreadAndWait(Action action, int waitTimeout = 5000)
  {
    if (action == null)
      throw new ArgumentNullException(nameof(action));
    if (waitTimeout <= 0)
      throw new ArgumentException("waitTimeout must be greater than 0.");

    if (IsInMainThread)
    {
      action();
      return;
    }
    ConcurrentAction concurrentAction = new(action);
    Instance.actionQueue.Enqueue(concurrentAction.InvokeAndDispose);
    bool waited = concurrentAction.Wait(waitTimeout);
    Assert.IsTrue(waited, "WaitHandle timed out.");
  }

  /// <summary>
  /// Instantiates this MonoBehaviour in a new GameObject and marks it DontDestroyOnLoad.
  /// </summary>
  private static UnityThread InjectToScene()
  {
    GameObject gameObject = new("UnityThread");
    UnityThread manager = gameObject.AddComponent<UnityThread>();
    DontDestroyOnLoad(gameObject);
    return manager;
  }

  /// <summary>
  /// TestFixture hook for validating that the update list enqueued the delegate correctly.
  /// </summary>
  internal static bool InUpdateQueue(OnUpdate update)
  {
    return Instance.onUpdateMethods.Contains(update);
  }

  public static void ThrowIfOutsideMainThread(string message = null)
  {
    throw new InvalidOperationException(message);
  }

  /// <summary>
  /// Helper that wraps multiple actions and a wait handle for ExecuteOnMainThreadAndWait.
  /// </summary>
  private sealed class ConcurrentAction : IDisposable
  {
    private readonly Action action;
    private readonly ManualResetEventSlim waitHandle = new();

    public ConcurrentAction(Action action)
    {
      this.action = action;
    }

    /// <summary>
    /// Invokes all actions on the main thread, then disposes the wait handle.
    /// </summary>
    public void InvokeAndDispose()
    {
      try
      {
        action();
        waitHandle.Set();
      }
      finally
      {
        Dispose();
      }
    }

    /// <summary>
    /// Blocks the calling thread until the actions have completed or timeout.
    /// </summary>
    /// <param name="waitTimeout">Milliseconds to wait.</param>
    /// <returns><see langword="true"/> if signaled within the timeout; otherwise <see langword="false"/>.</returns>
    public bool Wait(int waitTimeout)
    {
      bool waited = waitHandle.Wait(waitTimeout);
      return waited;
    }

    /// <summary>
    /// Dispose the wait handle.
    /// </summary>
    public void Dispose()
    {
      waitHandle.Dispose();
    }
  }
}