using System.Collections.Generic;
using CoreLib.Performance;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

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
    CompleteAll();
  }

  public static void CompleteAll()
  {
    if (!instance)
      return;

    UnityThread.ExecuteOnMainThreadAndWait(delegate
    {
      if (instance.dispatchers.Count == 0)
        return;

      foreach (IJobDispatcher dispatcher in instance.dispatchers)
      {
        dispatcher.CompleteAll();
      }
    });
  }

  public static void Register(IJobDispatcher dispatcher)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    if (Exists)
    {
      GameObject go = new("DispatchManager");
      go.AddComponent<JobDispatchManager>();
      DontDestroyOnLoad(go);
    }
    instance.dispatchers.Add(dispatcher);
  }

  public static void Deregister(IJobDispatcher dispatcher)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    if (instance)
    {
      dispatcher.CompleteAll();
      instance.dispatchers.Remove(dispatcher);
    }
  }
}
