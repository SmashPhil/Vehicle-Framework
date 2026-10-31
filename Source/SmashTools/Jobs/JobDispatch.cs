using CoreLib.Jobs;
using CoreLib.Performance;
using Verse;

namespace SmashTools.Jobs;

[StaticConstructorOnStartup]
internal static class JobDispatch
{
  static JobDispatch()
  {
    GameEvent.OnNewGame += CompleteAllJobs;
    GameEvent.OnLoadGame += CompleteAllJobs;
    GameEvent.OnGameDisposing += CompleteAllJobs;
  }

  private static void CompleteAllJobs()
  {
    if (JobDispatchManager.Exists)
    {
      UnityThread.ExecuteOnMainThreadAndWait(JobDispatchManager.CompleteAll);
    }
  }
}
