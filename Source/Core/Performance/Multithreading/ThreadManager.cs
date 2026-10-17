using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace CoreLib.Performance;

/// <summary>
/// Thread creation and management for dedicated threads that run continuously until released.
/// <para />
/// Useful for continuously running background calculations such as updating map or world grids.
/// </summary>
[PublicAPI]
public static class ThreadManager
{
  // TODO - implement forced sharing if this upper limit is ever hit as a fallback case. This
  // should never be the case in current implementations by Vehicle Framework.
  private const int MaxThreads = 10;

  // Single thread ids start above this threshold, everything below is dedicated to shared threads.
  private const int SingleThreadIdOffset = 100;

  private static int nextId = SingleThreadIdOffset;

  private static readonly Dictionary<int, ushort> ThreadRefCounts = new(MaxThreads);

  private static readonly List<DedicatedThread> Threads = new(MaxThreads);

  private static readonly object ThreadListLock = new();

  // Just reading the _size int, it can't be guaranteed this count isn't stale
  // if being checked immediately after List has been modified.
  public static bool AllThreadsTerminated
  {
    get
    {
      lock (ThreadListLock)
      {
        return Threads.Count == 0;
      }
    }
  }

  /// <summary>
  /// Snapshot of active threads.
  /// </summary>
  public static ListSnapshot<DedicatedThread> ThreadsSnapshot
  {
    get
    {
      lock (ThreadListLock)
      {
        return new ListSnapshot<DedicatedThread>(Threads);
      }
    }
  }

  /// <summary>
  /// Create <see cref="DedicatedThread"/> with ownership.
  /// </summary>
  /// <remarks>
  /// Returned thread will be thread type Single. When released it will be immediately disposed.
  /// </remarks>
  [MustUseReturnValue]
  public static DedicatedThread CreateNew()
  {
    DedicatedThread thread = CreateNew(nextId, DedicatedThread.ThreadType.Single);
    Interlocked.Increment(ref nextId);
    return thread;
  }

  /// <summary>
  /// Create or fetch <see cref="DedicatedThread"/> with shared ownership.
  /// </summary>
  /// <remarks>
  /// When all owners have released the thread, it will be disposed.
  /// </remarks>
  [MustUseReturnValue]
  public static DedicatedThread GetOrCreateShared(int id)
  {
    return CreateNew(id, DedicatedThread.ThreadType.Shared);
  }

  private static DedicatedThread CreateNew(int id, DedicatedThread.ThreadType type)
  {
    lock (ThreadListLock)
    {
      if (Threads.Count > MaxThreads)
        throw new InvalidOperationException(
          "Trying to create more threads than is allowed by ThreadManager.");
      if (type == DedicatedThread.ThreadType.Shared && id >= SingleThreadIdOffset)
        throw new ArgumentException(
          $"Shared thread ids must be between 0 and {SingleThreadIdOffset} to avoid conflicting with unshared thread ids.");

      DedicatedThread thread = GetThread(id);
      if (thread == null)
      {
        thread = new DedicatedThread(id, type);
        Threads.Add(thread);
        ThreadRefCounts.Add(id, 1);
      }
      else
      {
        ThreadRefCounts[id]++;
      }
      return thread;
    }
  }

  /// <summary>
  /// Get thread by id from list of active threads.
  /// </summary>
  [Pure]
  public static DedicatedThread GetThread(int id)
  {
    lock (ThreadListLock)
    {
      foreach (DedicatedThread thread in Threads)
      {
        if (thread.id == id)
          return thread;
      }
    }
    return null;
  }

  public static void Release(this DedicatedThread dedicatedThread)
  {
    switch (dedicatedThread.type)
    {
      case DedicatedThread.ThreadType.Single:
        DisposeThread(dedicatedThread);
      break;
      case DedicatedThread.ThreadType.Shared:
        ReleaseReference(dedicatedThread);
      break;
      default:
        throw new NotImplementedException(dedicatedThread.type.ToString());
    }
  }

  private static void ReleaseReference(DedicatedThread dedicatedThread)
  {
    lock (ThreadListLock)
    {
      int id = dedicatedThread.id;
      ThreadRefCounts[id]--;
      if (ThreadRefCounts[id] == 0)
      {
        DisposeThread(dedicatedThread);
      }
    }
  }

  private static void DisposeThread(DedicatedThread dedicatedThread)
  {
    lock (ThreadListLock)
    {
      dedicatedThread.StopImmediately();
      if (!Threads.Remove(dedicatedThread))
        Trace.Fail($"Failed to remove thread {dedicatedThread.id} from ThreadManager");
      ThreadRefCounts.Remove(dedicatedThread.id);
    }
  }

  internal static void OnSceneChanged(Scene scene, LoadSceneMode mode)
  {
    Assert.AreEqual(mode, LoadSceneMode.Single);
    ReleaseAll();
  }

  public static void ReleaseAll()
  {
    using ListSnapshot<DedicatedThread> threadsSnapshot = ThreadsSnapshot;

    // Will take at least a few more clock cycles for thread worker to receive cancellation
    // request and terminate. We must wait until then otherwise MemoryUtility.ClearAllMapsAndWorld
    // will set map fields to null and ongoing operations will throw. This function is executed
    // right before that happens.
    foreach (DedicatedThread dedicatedThread in threadsSnapshot)
    {
      ReleaseAndJoin(dedicatedThread);
    }
  }

  public static void ReleaseAndJoin(DedicatedThread dedicatedThread)
  {
    // It should take nowhere even close to 5 seconds to signal the thread to stop immediately and
    // wait for it to terminate. If we hit this threshold, then we probably deadlocked.
    const int JoinTimeout = 5000; // ms

    DisposeThread(dedicatedThread);
    if (!dedicatedThread.thread.Join(JoinTimeout))
    {
      Logger.Error($"Thread {dedicatedThread.id} has failed to terminate.");
    }
  }
}