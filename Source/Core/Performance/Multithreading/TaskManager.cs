using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace CoreLib.Performance;

[PublicAPI]
public static class TaskManager
{
  [Pure]
  public static Task Run(Action action, CancellationToken token)
  {
    return ForgetAwaited(Task.Run(action, token));

    static async Task ForgetAwaited(Task task)
    {
      try
      {
        await task.ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        // Uncaught exceptions from thread pool thread will crash
        Logger.Error($"Exception thrown executing task.\n{ex}");
      }
    }
  }

  /// <summary>
  /// Run async action through a task as opposed to enqueueing on the dedicated thread.
  /// </summary>
  public static void FireAndForget(AsyncAction action, CancellationToken token)
  {
    Assert.IsTrue(action.IsValid);
    _ = Run(action.Invoke, token);
  }
}