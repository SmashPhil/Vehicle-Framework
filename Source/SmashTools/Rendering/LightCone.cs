using System;
using CoreLib.Rendering;
using JetBrains.Annotations;
using LudeonTK;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace SmashTools.Rendering;

public class LightCone : GlowEmitter
{
  private IntVec3 position;

  public LightCone([NotNull] Map map, in Configuration config) : base(map)
  {
    position = config.position;
    Angle = config.angle;
    Direction = config.direction;
    Length = config.length;
    Intensity = config.intensity ?? 1f;
    overlightRadius = config.overlightRadius;
    if (config.color.HasValue)
    {
      color = config.color.Value;
    }
  }

  public float Angle
  {
    get;
    set
    {
      if (Mathf.Approximately(field, value))
        return;

      field = value;
      MarkDirty();
    }
  }

  public float Direction
  {
    get;
    set
    {
      if (Mathf.Approximately(field, value))
        return;

      field = value;
      MarkDirty();
    }
  }

  public int Length
  {
    get;
    set
    {
      if (field == value)
        return;

      field = value;
      MarkDirty();
    }
  }

  public float Intensity
  {
    get;
    set
    {
      value = Mathf.Max(0f, value);
      if (Mathf.Approximately(field, value))
        return;

      field = value;
      MarkDirty();
    }
  }

  public virtual IntVec3 Position
  {
    get => position;
    set
    {
      if (position.x == value.x && position.z == value.z)
        return;

      position = value;
      MarkDirty();
    }
  }

  public float OverlightRadius
  {
    get => overlightRadius;
    set
    {
      if (Mathf.Approximately(overlightRadius, value))
        return;

      overlightRadius = value;
      MarkDirty();
    }
  }

  public Color32 Color
  {
    get => color;
    set
    {
      if (color.r == value.r && color.g == value.g && color.b == value.b && color.a == value.a)
        return;

      color = value;
      MarkDirty();
    }
  }

  protected internal override CellRect AffectedRect => CalculateAffectedRect(Position, Direction);

  protected CellRect CalculateAffectedRect(IntVec3 origin, float facing)
  {
    int radius = Math.Min(Math.Max(Length, 0), Math.Max(map.Size.x, map.Size.z));
    float halfAngle = Mathf.Clamp(Angle * 0.5f, 0f, 180f);
    float left = (facing - halfAngle) * Mathf.Deg2Rad;
    float right = (facing + halfAngle) * Mathf.Deg2Rad;
    float leftX = Mathf.Sin(left) * radius;
    float leftZ = Mathf.Cos(left) * radius;
    float rightX = Mathf.Sin(right) * radius;
    float rightZ = Mathf.Cos(right) * radius;

    float minX = Mathf.Min(0f, Mathf.Min(leftX, rightX));
    float maxX = Mathf.Max(0f, Mathf.Max(leftX, rightX));
    float minZ = Mathf.Min(0f, Mathf.Min(leftZ, rightZ));
    float maxZ = Mathf.Max(0f, Mathf.Max(leftZ, rightZ));

    if (Mathf.Abs(Mathf.DeltaAngle(facing, 0f)) <= halfAngle)
      maxZ = radius;
    if (Mathf.Abs(Mathf.DeltaAngle(facing, 90f)) <= halfAngle)
      maxX = radius;
    if (Mathf.Abs(Mathf.DeltaAngle(facing, 180f)) <= halfAngle)
      minZ = -radius;
    if (Mathf.Abs(Mathf.DeltaAngle(facing, 270f)) <= halfAngle)
      minX = -radius;

    // Diagonal propagation also reads the two adjacent side cells as blockers.
    return CellRect.FromLimits(
      Mathf.FloorToInt(origin.x + minX) - 1,
      Mathf.FloorToInt(origin.z + minZ) - 1,
      Mathf.CeilToInt(origin.x + maxX) + 1,
      Mathf.CeilToInt(origin.z + maxZ) + 1).ClipInsideMap(map);
  }

  protected internal override void Compute([ReadOnly] NativeBitArray blockers)
  {
    ComputeCone(Position, Direction, blockers);
  }

  protected void ComputeCone(IntVec3 origin, float facing, NativeBitArray blockers)
  {
    if (!origin.InBounds(map) || Length < 0)
      return;

    int mapWidth = map.Size.x;
    int mapHeight = map.Size.z;
    int radius = Math.Min(Length, Math.Max(mapWidth, mapHeight));
    int minX = Math.Max(0, origin.x - radius);
    int maxX = Math.Min(mapWidth - 1, origin.x + radius);
    int minZ = Math.Max(0, origin.z - radius);
    int maxZ = Math.Min(mapHeight - 1, origin.z + radius);
    long radiusSquared = (long)radius * radius;

    float radians = facing * Mathf.Deg2Rad;
    float directionX = Mathf.Sin(radians);
    float directionZ = Mathf.Cos(radians);
    float cosineLimit = Mathf.Cos(Mathf.Clamp(Angle * 0.5f, min: 0f, max: 180f) * Mathf.Deg2Rad);
    ColorInt sourceColor = color.AsColorInt() with { a = 0 };

    RayCast<LightBlocker> ray = new(origin.x, origin.z, new RayCast<LightBlocker>.Settings
    {
      mapWidth = mapWidth,
      blocker = new LightBlocker(blockers)
    });
    for (int z = minZ; z <= maxZ; z++)
    {
      for (int x = minX; x <= maxX; x++)
      {
        int offsetX = x - origin.x;
        int offsetZ = z - origin.z;
        long distanceSquared = (long)offsetX * offsetX + (long)offsetZ * offsetZ;
        if (distanceSquared > radiusSquared)
          continue;

        float distance = Mathf.Sqrt(distanceSquared);
        float projection = offsetX * directionX + offsetZ * directionZ;
        if (distanceSquared != 0 && projection < distance * cosineLimit)
          continue;

        int mapIndex = CellIndicesUtility.CellToIndex(x, z, mapWidth);
        if ((offsetX != 0 || offsetZ != 0) && blockers.IsSet(mapIndex))
          continue;

        if (!ray.Hit(x, z))
          continue;

        float factor = distanceSquared == 0 ? 1f : Mathf.Lerp(
          1f - distance / Math.Max(1, Length),
          1f / (distance * distance), 0.4f);
        ColorInt light = sourceColor * (factor * Intensity);
        light.ClampToNonNegative();
        if (light is { r: <= 0, g: <= 0, b: <= 0 })
          continue;

        light.ProjectToColor32Fast(out Color32 contribution);
        contribution.a = distance < OverlightRadius ? (byte)1 : (byte)0;
        Fill(mapIndex, contribution);
      }
    }
  }

  private struct LightBlocker(NativeBitArray blockers) : ICellBlocker
  {
    private NativeBitArray blockers = blockers;

    public bool IsBlocked(int index)
    {
      return blockers.IsSet(index);
    }
  }

  [PublicAPI]
  public struct Configuration
  {
    public required IntVec3 position;
    public required float angle;
    public required float direction;
    public required int length;
    public float? intensity;
    public float overlightRadius;
    public Color? color;
  }
}
