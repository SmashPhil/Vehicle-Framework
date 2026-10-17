using System;
using System.Collections.Generic;
using CoreLib.Performance;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Targeting;

[StaticConstructorOnStartup]
public static class TargeterDispatcher
{
  private static readonly Stack<ITargeter> Targeters = [];

  private static ITargeter Current { get; set; }

  internal static bool TargeterUpdate()
  {
    if (Current == null)
      return false;

    try
    {
      Current.Update();
    }
    catch (Exception ex)
    {
      // Remove problematic targeter or we'll end up spamming incessantly
      Targeters.TryPop(out _);
      Log.Error($"Root level exception in TargeterUpdate: {ex}");
    }
    return true;
  }

  internal static bool TargeterOnGUI()
  {
    if (Current == null)
      return false;

    try
    {
      Current.OnGUI();
    }
    catch (Exception ex)
    {
      // Remove problematic targeter or we'll end up spamming incessantly
      Targeters.Pop();
      UpdateCurrent();
      Log.Error($"Root level exception in TargeterOnGUI: {ex}");
    }

    return true;
  }

  extension(ITargeter targeter)
  {
    public void Start()
    {
      Assert.IsTrue(UnityThread.IsInMainThread);
      if (Targeters.Count == 0)
      {
        UnityThread.StartUpdate(TargeterUpdate);
        UnityThread.StartGui(TargeterOnGUI);
      }
      Targeters.Push(targeter);
      targeter.OnStart();
      UpdateCurrent();
    }

    public void Stop()
    {
      if (Targeters.Count == 0)
      {
        Log.Error("Trying to stop targeter but the targeter stack is empty.");
        return;
      }

      Assert.IsTrue(UnityThread.IsInMainThread);
      if (Targeters.Peek() == targeter)
      {
        Targeters.Pop();
      }
      else
      {
        Log.Error("Removing targeter out of sequence.");
        Remove(targeter);
      }
      targeter.OnStop();
      UpdateCurrent();
    }
  }
  
  private static void UpdateCurrent()
  {
    Current = Targeters.Count > 0 ? Targeters.Peek() : null;
  }

  private static void Remove(ITargeter targeter)
  {
    Stack<ITargeter> buffer = [];
    while (Targeters.Count > 0)
    {
      ITargeter top = Targeters.Pop();
      if (top == targeter)
        break;
      buffer.Push(top);
    }

    while (buffer.Count > 0)
    {
      Targeters.Push(buffer.Pop());
    }
  }
}