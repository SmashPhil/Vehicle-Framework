using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace CoreLib.Performance;

[PublicAPI]
public class DedicatedThread
{
  internal readonly Thread thread;
  public readonly int id;
  public readonly ThreadType type;

  private bool shouldTerminate;
  private readonly ManualResetEventSlim workHandle;
  private readonly ManualResetEventSlim suspendHandle;
  private readonly ConcurrentQueue<AsyncAction> queue;

  internal DedicatedThread(int id, ThreadType type)
  {
    this.id = id;
    this.type = type;

    workHandle = new ManualResetEventSlim(false);
    suspendHandle = new ManualResetEventSlim(false);
    queue = [];
    thread = new Thread(Execute)
    {
      IsBackground = true,
      Priority = ThreadPriority.BelowNormal,
    };
    thread.Start();
  }

  public int QueueCount => queue.Count;

  /// <summary>
  /// Thread has been suspended and action queue is empty.
  /// </summary>
  public bool IsSuspended => State is ThreadState.Suspended;

  /// <summary>
  /// Thread has been terminated and disposed.
  /// </summary>
  public bool IsTerminated => State is ThreadState.Terminated;

  /// <summary>
  /// Wait handle is currently unset and thread is waiting for work.
  /// </summary>
  public bool IsBlocked => !workHandle.IsSet;

  /// <summary>
  /// State of the thread worker. <b>Not equivalent to <see cref="System.Threading.ThreadState"/></b>
  /// </summary>
  public ThreadState State { get; private set; }

  /// <summary>
  /// Suspends new actions from being enqueued and blocks after all remaining actions are executed.
  /// </summary>
  /// <remarks>
  /// Execution of the calling thread will be blocked until the action queue is empty.
  /// </remarks>
  /// <exception cref="InvalidOperationException">Attempting to suspend the thread from inside the thread.</exception>
  public void Suspend()
  {
    if (Thread.CurrentThread.ManagedThreadId == thread.ManagedThreadId)
      throw new InvalidOperationException("Attempting to suspend thread from inside the thread.");

    // Suspending is lower priority than everything else including stopping.
    if (State is not ThreadState.Running)
      return;

    // Suspending empties the queue before entering a suspended state, however we still have this
    // intermediate state where we can't accept new actions yet the thread is not fully suspended.
    suspendHandle.Reset();
    State = ThreadState.Suspending;
    UnpauseConsumer();
    suspendHandle.Wait();
  }

  /// <summary>
  /// Resume execution of the thread and reenable enqueueing actions.
  /// </summary>
  /// <exception cref="InvalidOperationException">Attempting to unsuspend the thread when it is not currently suspended.</exception>
  public void Unsuspend()
  {
    switch (State)
    {
      case ThreadState.Running:
        return;
      case ThreadState.Suspending:
        throw new InvalidOperationException(
          "Unsuspending a thread which is still spinning up to suspend.");
      case not ThreadState.Suspended:
        throw new InvalidOperationException("Unsuspending a thread which was not suspended.");
    }
    State = ThreadState.Running;
    UnpauseConsumer();
  }

  /// <summary>
  /// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
  /// </summary>
  internal void Snapshot(List<AsyncAction> items)
  {
    items.AddRange(queue);
  }

  /// <summary>
  /// Enqueue action to queue for execution and unblock the thread worker.
  /// </summary>
  public void Enqueue(AsyncAction action)
  {
    if (State is ThreadState.Suspending or ThreadState.Suspended)
      throw new InvalidOperationException(
        $"Thread {id} has been enqueued an item while suspended. It will not execute.");
    queue.Enqueue(action);
    UnpauseConsumer();
  }

  /// <summary>
  /// Enqueue item to action queue without unblocking thread worker.
  /// </summary>
  /// <remarks>Should not be used outside of debugging contexts.</remarks>
  internal void EnqueueSilently(AsyncAction action)
  {
    queue.Enqueue(action);
  }

  private void UnpauseConsumer()
  {
    if (State is not ThreadState.Terminated)
      workHandle.Set();
  }

  /// <summary>
  /// Thread will finish executing the rest of the queue and then terminate
  /// </summary>
  public void Stop()
  {
    shouldTerminate = true;
    // Unblock consumer so we can exit the loop and terminate
    UnpauseConsumer();
  }

  /// <summary>
  /// Stops thread execution as soon as possible, discarding any remaining queued actions
  /// </summary>
  public void StopImmediately()
  {
    if (State is ThreadState.Terminated)
      return;

    State = ThreadState.Stopping;
    Stop();
  }

  private void Execute()
  {
    State = ThreadState.Running;
    try
    {
      while (!shouldTerminate)
      {
        workHandle.Wait();
        workHandle.Reset();

        while (State is not ThreadState.Stopping && queue.TryDequeue(out AsyncAction asyncAction))
        {
          try
          {
            if (asyncAction.IsValid)
              asyncAction.Invoke();
          }
          catch (Exception ex)
          {
            Logger.Error($"Exception thrown while executing {asyncAction} on DedicatedThread " +
              $"#{id:D3}.\nException={ex}");
            asyncAction.ExceptionThrown(ex);
          }
          finally
          {
            asyncAction.ReturnToPool();
          }
        }
        if (State is ThreadState.Suspending)
        {
          State = ThreadState.Suspended;
          suspendHandle.Set();
        }
      }
    }
    catch (Exception ex)
    {
      Logger.Error($"Exception thrown from thread={thread.ManagedThreadId}.\n{ex}");
    }
    finally
    {
      State = ThreadState.Terminated;
      workHandle.Dispose();
      suspendHandle.Dispose();
    }
  }

  public enum ThreadType
  {
    Single,
    Shared
  }

  public enum ThreadState
  {
    Uninitialized,
    Running,
    Suspending,
    Suspended,
    Stopping,
    Terminated
  }
}