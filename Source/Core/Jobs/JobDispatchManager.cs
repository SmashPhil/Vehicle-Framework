using System.Collections.Generic;
using CoreLib.Performance;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLib.Jobs;

[PublicAPI]
public sealed class JobDispatchManager : MonoBehaviour
{
  private readonly List<IJobDispatcher> dispatchers = [];

  private static JobDispatchManager instance;

  public static bool Exists => instance != null;

  private void Awake()
  {
    instance = this;
  }

  private void LateUpdate()
  {
    foreach (IJobDispatcher dispatcher in dispatchers)
    {
      dispatcher.LateUpdate();
    }
  }

  private void OnDestroy()
  {
    DestroyDispatchers();
  }

  public static void Destroy()
  {
    if (!UnityThread.IsInMainThread)
    {
      Logger.Error("Trying to destroy JobDispatchManager outside of the main thread.");
      UnityThread.ExecuteOnMainThreadAndWait(() => Object.Destroy(instance));
    }
    else
    {
      Object.Destroy(instance);
    }
  }

  public static void Register(IJobDispatcher dispatcher)
  {
    instance.dispatchers.Add(dispatcher);
  }

  public static void Deregister(IJobDispatcher dispatcher)
  {
    instance.dispatchers.Remove(dispatcher);
  }

  private void DestroyDispatchers()
  {
    if (dispatchers.Count == 0)
      return;

    foreach (IJobDispatcher dispatcher in dispatchers)
    {
      dispatcher.OnDestroy();
    }
  }
}
