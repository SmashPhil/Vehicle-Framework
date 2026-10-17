using CoreLib.Jobs;
using CoreLib.Performance;
using UnityEngine;
using Verse;

namespace SmashTools.Jobs;

[StaticConstructorOnStartup]
internal static class JobDispatch
{
  static JobDispatch()
  {
    GameEvent.OnNewGame += CreateDispatchManager;
    GameEvent.OnLoadGame += CreateDispatchManager;
    GameEvent.OnGameDisposing += DestroyDispatchManager;
  }

  private static void CreateDispatchManager()
  {
    LongEventHandler.ExecuteWhenFinished(static delegate
    {
      if (!JobDispatchManager.Exists)
      {
        GameObject go = new("DispatchManager");
        go.AddComponent<JobDispatchManager>();
      }
    });
  }

  private static void DestroyDispatchManager()
  {
    if (JobDispatchManager.Exists)
    {
      UnityThread.ExecuteOnMainThreadAndWait(JobDispatchManager.Destroy);
    }
  }
}
