using System;
using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools;

/// <summary>
/// Scoped struct for restoring GUI matrix via Dispose pattern.
/// </summary>
[PublicAPI]
public readonly struct GUIMatrixScope : IDisposable
{
  private readonly Matrix4x4 matrix;

  public GUIMatrixScope()
  {
    matrix = GUI.matrix;
  }

  public GUIMatrixScope(Matrix4x4 matrix)
  {
    this.matrix = GUI.matrix;
    GUI.matrix = matrix;
  }

  void IDisposable.Dispose()
  {
    GUI.matrix = matrix;
  }
}