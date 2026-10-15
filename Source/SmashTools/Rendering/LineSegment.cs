using JetBrains.Annotations;
using UnityEngine;

namespace SmashTools.Rendering;

/// <summary>
/// Defines a line segment between 2 points in 3D world space.
/// </summary>
[PublicAPI]
public readonly record struct LineSegment
{
  public readonly Vector3 from;
  public readonly Vector3 to;
  public readonly Color color;

  /// <param name="from">Start point</param>
  /// <param name="to">End point</param>
  public LineSegment(Vector3 from, Vector3 to) : this(from, to, Color.white)
  {
  }

  /// <param name="from">Start point</param>
  /// <param name="to">End point</param>
  /// <param name="color">Color of line</param>
  public LineSegment(Vector3 from, Vector3 to, Color color)
  {
    this.from = from;
    this.to = to;
    this.color = color;
  }
}