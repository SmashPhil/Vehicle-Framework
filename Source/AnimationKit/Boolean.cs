using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AnimationKit;

/// <summary>
/// Blittable boolean type wrapper for compatibility with C APIs.
/// </summary>
[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Boolean : IEquatable<Boolean>
{
  private readonly byte value;

  public static implicit operator bool(Boolean b) => b.value != 0;

  public static implicit operator Boolean(bool value) => new(value);

  public static implicit operator byte(Boolean b) => b.value;

  public static implicit operator Boolean(byte b) => new(b != 0);

  public Boolean()
  {
    value = 0;
  }

  public Boolean(bool value)
  {
    this.value = value ? (byte)1 : (byte)0;
  }

  public Boolean(byte value)
  {
    this.value = value;
  }

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