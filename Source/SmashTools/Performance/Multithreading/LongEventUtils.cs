using System.Threading;
using CoreLib.Performance;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance;

[PublicAPI]
public static class LongEventUtils
{
  private static readonly AccessTools.FieldRef<object, Thread> EventThreadFieldRef;

  static LongEventUtils()
  {
    EventThreadFieldRef =
      AccessTools.FieldRefAccess<Thread>(typeof(LongEventHandler), "eventThread");
  }

  public static bool InMainOrEventThread
  {
    get
    {
      if (UnityThread.IsInMainThread)
        return true;

      Thread eventThread = EventThreadFieldRef.Invoke();
      return eventThread == null ||
             Thread.CurrentThread.ManagedThreadId == eventThread.ManagedThreadId;
    }
  }
}
