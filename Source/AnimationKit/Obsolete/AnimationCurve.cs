using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Animations;

[PublicAPI]
public sealed class AnimationCurve : IXmlExport
{
  public List<KeyFrame> points = [];

  public KeyFrame LeftBound => points.Count > 0 ? points[0] : KeyFrame.Invalid;

  public KeyFrame RightBound => points.Count > 0 ? points[points.Count - 1] : KeyFrame.Invalid;

  public FloatRange RangeX => new(LeftBound.frame, RightBound.frame);

  public FloatRange RangeY => throw new NotImplementedException();

  public int PointsCount => points.Count;

  public bool IsValid => !points.NullOrEmpty();

  public float this[int frame]
  {
    get { return Function(frame); }
  }

  public bool Add(int frame, float value)
  {
    foreach (KeyFrame point in points)
    {
      if (point.frame == frame)
        return false;
    }
    points.Add(new KeyFrame(frame, value));
    points.Sort();
    return true;
  }

  public void Set(int frame, float value)
  {
    for (int i = 0; i < points.Count; i++)
    {
      KeyFrame point = points[i];
      if (point.frame == frame)
      {
        points[i] = new KeyFrame(point.frame, value);
        return;
      }
      if (point.frame > frame)
      {
        points.Insert(i, new KeyFrame(frame, value));
        return;
      }
    }
    Add(frame, value); // Only add if insert attempt failed
  }

  public void Remove(int frame)
  {
    for (int i = 0; i < points.Count; i++)
    {
      KeyFrame point = points[i];
      if (point.frame == frame)
      {
        points.RemoveAt(i);
        break;
      }
    }
  }

  public bool KeyFrameAt(float frame)
  {
    foreach (KeyFrame keyFrame in points)
    {
      if (Mathf.Approximately(keyFrame.frame, frame))
        return true;
      if (keyFrame.frame > frame)
      {
        // If past the frame check point, it won't be found at future points.
        // Curve is kept sorted at all times
        return false;
      }
    }
    return false;
  }

  public float Function(float time)
  {
    if (points.NullOrEmpty() || RightBound.frame <= 0)
    {
      return 0;
    }
    if (points.Count == 1)
    {
      return LeftBound.value;
    }
    if (time <= LeftBound.frame)
    {
      return LeftBound.value;
    }
    else if (time >= RightBound.frame)
    {
      return RightBound.value;
    }
    return CubicSpline(time);
  }

  /// <summary>
  /// Hermite cubic spline interpolation.
  /// </summary>
  /// <remarks>
  /// <para>Documentation for curve formulas <see href="https://github.khronos.org/glTF-Tutorials/gltfTutorial/gltfTutorial_007_Animations.html#cubic-spline-interpolation">here.</see></para>
  /// <para>Desmos example graph <see href="https://www.desmos.com/calculator/mcgp64duuy">here.</see></para>
  /// </remarks>
  private float CubicSpline(float time)
  {
    KeyFrame prev = KeyFrame.Invalid;
    KeyFrame next = KeyFrame.Invalid;
    for (int i = 0; i < points.Count; i++)
    {
      if (Mathf.Approximately(points[i].frame, time))
        return points[i].value;

      if (points[i].frame > time)
        break;

      Assert.IsFalse(points.OutOfBounds(i + 1));
      prev = points[i];
      next = points[i + 1];
    }
    if (Mathf.Approximately(prev.outTangent, float.PositiveInfinity))
      return prev.value;
    if (Mathf.Approximately(prev.outTangent, float.NegativeInfinity))
      return next.value;
    if (Mathf.Approximately(next.inTangent, float.PositiveInfinity))
      return prev.value;
    if (Mathf.Approximately(next.inTangent, float.NegativeInfinity))
      return next.value;

    float dt = next.frame - prev.frame;
    // (f - kt(i))
    float t = (time - prev.frame) / dt;
    // t^2
    float t2 = t * t;
    // t^3
    float t3 = t * t * t;

    // tangents scaled to { 0 ≤ t ≤ 1 }
    float m0 = prev.outTangent * dt;
    float m1 = next.inTangent * dt;

    // k(0) = 2t^3 - 3t^2 + 1
    float k0 = (2 * t3 - 3 * t2 + 1) * prev.value;
    // k(1) = t^3 - 2t^2 + t
    float k1 = (t3 - 2 * t2 + t) * m0;
    // k'(0) = t^3 - t^2
    float k2 = (t3 - t2) * m1;
    // k'(1) = -2t^3 + 3t^2
    float k3 = (-2 * t3 + 3 * t2) * next.value;

    if (prev.weightedMode == WeightedMode.Out || prev.weightedMode == WeightedMode.Both)
    {
      k1 *= prev.outWeight;
    }
    if (next.weightedMode == WeightedMode.In || next.weightedMode == WeightedMode.Both)
    {
      k2 *= next.inWeight;
    }
    return k0 + k1 + k2 + k3;
  }

  // Linear function for testing rendering in the animation editor
  private float Lerp(float frame)
  {
    KeyFrame leftPoint = points[0];
    KeyFrame rightPoint = points[points.Count - 1];
    for (int i = 0; i < points.Count; i++)
    {
      if (frame <= points[i].frame)
      {
        rightPoint = points[i];
        if (i > 0)
        {
          leftPoint = points[i - 1];
        }
        break;
      }
    }
    float t = (frame - leftPoint.frame) / (rightPoint.frame - leftPoint.frame);
    return Mathf.LerpUnclamped(leftPoint.value, rightPoint.value, t);
  }

  void IXmlExport.Export()
  {
    XmlExporter.WriteCollection(nameof(points), points);
  }
}