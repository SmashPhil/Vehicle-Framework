using System;
using System.Runtime.InteropServices;

namespace AnimationKit;

/// <summary>
/// Blittable boolean type
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Boolean(bool value) : IEquatable<Boolean>
{
  private readonly byte value = value ? (byte)1 : (byte)0;

  public static implicit operator bool(Boolean b) => b.value != 0;

  public static implicit operator Boolean(bool value) => new(value);

  public bool Equals(Boolean other)
  {
    return value == other.value;
  }

  public override bool Equals(object obj)
  {
    return obj is Boolean other && Equals(other);
  }

  public override int GetHashCode()
  {
    return value.GetHashCode();
  }

  public static bool operator ==(Boolean left, Boolean right)
  {
    return left.Equals(right);
  }

  public static bool operator !=(Boolean left, Boolean right)
  {
    return !left.Equals(right);
  }
}