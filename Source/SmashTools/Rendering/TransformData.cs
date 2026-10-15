using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Rendering;

[PublicAPI]
public readonly record struct TransformData
{
  public readonly Vector3 position;
  public readonly Rot8 orientation;
  public readonly float rotation;

  public TransformData()
  {
    position = Vector3.zero;
    orientation = Rot8.Invalid;
    rotation = 0;
  }

  public TransformData(Vector3 position)
  {
    this.position = position;
  }

  public TransformData(Vector3 position, Rot8 orientation) : this(position)
  {
    this.position = position;
    this.orientation = orientation;
  }

  public TransformData(Vector3 position, Rot8 orientation, float rotation)
    : this(position, orientation)
  {
    this.rotation = rotation;
  }

  // NOTE - Orientation is not added or modified. The original orientation will be copied
  // over for all arithmetic operations.
  public TransformData Add(Vector3 position, float rotation)
  {
    return new TransformData(this.position + position, orientation, this.rotation + rotation);
  }

  public static TransformData For(Thing thing, Rot8? rot = null, float? extraRotation = null)
  {
    return new TransformData(thing.DrawPos, rot ?? thing.Rotation, extraRotation ?? 0);
  }

  // Transforms should only be additive for hierarchal values. Other arithmetic operators are not necessary
  public static TransformData operator +(TransformData lhs, TransformData rhs)
  {
    Assert.AreEqual(lhs.orientation, rhs.orientation,
      "Mismatched orientations for TransformData. 2nd transform orientation will be overridden.");
    return new TransformData(lhs.position + rhs.position, lhs.orientation,
      lhs.rotation + rhs.rotation);
  }
}