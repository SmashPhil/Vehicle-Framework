using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

public static class MainMenuKeyBindHandler
{
  private static readonly List<(KeyBindingDef keyBindingDef, Action action)> KeyBindings = [];

  public static void RegisterKeyBind([NotNull] KeyBindingDef keyBindingDef, Action action)
  {
    if (!KeyBindings.Exists(pair => pair.keyBindingDef == keyBindingDef))
    {
      KeyBindings.Add((keyBindingDef, action));
    }
  }

  internal static bool HandleKeyInputs()
  {
    // Find.WindowStack will be null for 1 frame since Root.Start initializes UIRoot::windowStack
    if (!Prefs.DevMode || Find.WindowStack == null)
      return true;

    foreach ((KeyBindingDef keyBindingDef, Action action) in KeyBindings)
    {
      if (Event.current != null && keyBindingDef.KeyDownEvent)
      {
        action();
        Event.current.Use();
      }
    }
    return true;
  }
}