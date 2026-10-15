using System;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace SmashTools;

[PublicAPI]
public readonly struct WidgetGroupScope : IDisposable
{
  public WidgetGroupScope(Rect rect)
  {
    Widgets.BeginGroup(rect);
  }

  void IDisposable.Dispose()
  {
    Widgets.EndGroup();
  }
}