using System;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using Verse;

namespace Vehicles.Rendering;

[PublicAPI]
public class PointLightProperties
{
  public Type glowType;

  public IntVec2 position;
  public IntVec2? northPositionOverride;
  public IntVec2? eastPositionOverride;
  public IntVec2? southPositionOverride;
  public IntVec2? westPositionOverride;
  public IntVec2? northEastPositionOverride;
  public IntVec2? southEastPositionOverride;
  public IntVec2? southWestPositionOverride;
  public IntVec2? northWestPositionOverride;

  public float overlightRadius;
  public Color? color;
  public float intensity = 1;

  public ComponentRequirement component;

  public IntVec2 RealPosition(Rot8 rotation, IntVec2 vehicleSize)
  {
    IntVec2? positionOverride = rotation.AsInt switch
    {
      Rot8.NorthInt => northPositionOverride ??
                       FlipVertical(southPositionOverride),
      Rot8.EastInt => eastPositionOverride ??
                      FlipHorizontal(westPositionOverride),
      Rot8.SouthInt => southPositionOverride ??
                       FlipVertical(northPositionOverride),
      Rot8.WestInt => westPositionOverride ??
                      FlipHorizontal(eastPositionOverride),
      Rot8.NorthEastInt => northEastPositionOverride ??
                           RotateCounterClockwise(southEastPositionOverride) ??
                           FlipHorizontal(northWestPositionOverride) ??
                           FlipAntiDiagonal(southWestPositionOverride),
      Rot8.SouthEastInt => southEastPositionOverride ??
                           RotateClockwise(northEastPositionOverride) ??
                           FlipHorizontal(southWestPositionOverride) ??
                           FlipMainDiagonal(northWestPositionOverride),
      Rot8.SouthWestInt => southWestPositionOverride ??
                           RotateCounterClockwise(northWestPositionOverride) ??
                           FlipHorizontal(southEastPositionOverride) ??
                           FlipAntiDiagonal(northEastPositionOverride),
      Rot8.NorthWestInt => northWestPositionOverride ??
                           RotateClockwise(southWestPositionOverride) ??
                           FlipHorizontal(northEastPositionOverride) ??
                           FlipMainDiagonal(southEastPositionOverride),
      _ => throw new ArgumentOutOfRangeException(nameof(rotation))
    };

    if (positionOverride.HasValue)
      return positionOverride.Value;

    if (rotation.IsDiagonal)
    {
      Vector2 rotated = Ext_Math.RotatePointClockwise(position.x, position.z, rotation.AsAngle);
      return new IntVec2(Mathf.RoundToInt(rotated.x), Mathf.RoundToInt(rotated.y));
    }

    return position.MirrorRotatedBy(rotation, vehicleSize);
  }

  private static IntVec2? FlipHorizontal(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(-value.x, value.z) : null;
  }

  private static IntVec2? FlipVertical(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(value.x, -value.z) : null;
  }

  private static IntVec2? RotateClockwise(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(value.z, -value.x) : null;
  }

  private static IntVec2? RotateCounterClockwise(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(-value.z, value.x) : null;
  }

  // Opposite diagonal facings reflect the lateral offset instead of reversing it.
  private static IntVec2? FlipAntiDiagonal(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(-value.z, -value.x) : null;
  }

  private static IntVec2? FlipMainDiagonal(IntVec2? offset)
  {
    return offset is { } value ? new IntVec2(value.z, value.x) : null;
  }

  public virtual GlowEmitter CreateNew(VehiclePawn vehicle)
  {
    return (GlowEmitter)Activator.CreateInstance(glowType, args: [vehicle, this]);
  }
}
