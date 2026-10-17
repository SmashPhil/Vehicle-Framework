using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SmashTools;

public static class Ext_LongEventHandler
{
  private static readonly FieldInfo currentEventField;
  private static readonly FieldInfo longEventTextField;
  private static readonly object currentEventTextLock;

  static Ext_LongEventHandler()
  {
    currentEventField = AccessTools.Field(typeof(LongEventHandler), "currentEvent");
    Type longQueuedEventType = AccessTools.TypeByName("Verse.LongEventHandler+QueuedLongEvent");
    longEventTextField = AccessTools.Field(longQueuedEventType, "eventText");
    currentEventTextLock = AccessTools.Field(typeof(LongEventHandler), "CurrentEventTextLock")
     .GetValue(null);
  }

  public static string GetLongEventText()
  {
    object currentEvent = currentEventField.GetValue(null);
    if (currentEvent != null)
    {
      lock (currentEventTextLock)
      {
        return (string)longEventTextField.GetValue(currentEvent);
      }
    }
    return null;
  }
}