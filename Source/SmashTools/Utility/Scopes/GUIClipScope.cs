using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools;

/// <summary>
/// Scoped struct for beginning and ending GUI clip via Dispose pattern.
/// </summary>
[PublicAPI]
public readonly struct GUIClipScope : IDisposable
{
  public GUIClipScope(Rect rect)
  {
    GUI.BeginClip(rect);
  }

  void IDisposable.Dispose()
  {
    GUI.EndClip();
  }
}