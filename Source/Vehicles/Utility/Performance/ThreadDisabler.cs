using System;
using System.Collections.Generic;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles
{
  /// <summary>
  /// RAII pattern for suspending dedicated thread activity on VehicleMapping components.
  /// This does not stop or abort the threads, it only flags the thread as being unavailable
  /// so that further actions are executed synchronously rather than getting enqueued to
  /// the dedicated thread.
  /// </summary>
  public class ThreadDisabler : IDisposable
  {
    // True = thread was active before disabling
    private Dictionary<Map, bool> threadStates = [];

    public ThreadDisabler()
    {
      // Need to disable from main thread, Find.Maps is not thread safe
      Assert.IsTrue(ThreadManager.InMainOrEventThread);

      foreach (Map map in Find.Maps)
      {
        VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
        if (mapping.ThreadAlive)
        {
          threadStates[map] = !mapping.dedicatedThread.IsSuspended;
          mapping.dedicatedThread.IsSuspended = true;
        }
      }
    }

    public void Dispose()
    {
      // Need to dispose from main thread, Find.Maps is not thread safe
      Assert.IsTrue(ThreadManager.InMainOrEventThread);

      foreach (Map map in Find.Maps)
      {
        VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
        if (mapping.ThreadAlive && threadStates.TryGetValue(map, out bool wasActive))
        {
          mapping.dedicatedThread.IsSuspended = !wasActive;
        }
      }
      GC.SuppressFinalize(this);
    }
  }
}