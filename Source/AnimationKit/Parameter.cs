using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnimationKit;

/// <summary>
/// Packed Variant type for animation primitives.
/// </summary>
[SkipLocalsInit]
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct Parameter
{
  public enum Type : byte { Float, Int, Bool };

  [FieldOffset(0)]
  public readonly Type type;
  [FieldOffset(4)]
  private float value_float;
  [FieldOffset(4)]
  private int value_int;
  [FieldOffset(4)]
  private Boolean value_bool;

  public T Value<T>() where T : unmanaged
  {
    AssertType<T>(in this);
    return type switch
    {
      Type.Float => UnsafeUtility.As<float, T>(ref value_float),
      Type.Int => UnsafeUtility.As<int, T>(ref value_int),
      Type.Bool => UnsafeUtility.As<Boolean, T>(ref value_bool),
      _ => throw new NotSupportedException("Type must be a supported variant type.")
    };
  }

  public T Cast<T>() where T : unmanaged
  {
    switch (type)
    {
      case Type.Float:
        if (typeof(T) == typeof(float))
          return Value<T>();

        Assert.IsTrue(typeof(int) == typeof(T) || typeof(Boolean) == typeof(T));
        if (typeof(int) == typeof(T))
        {
          int value = (int)value_float;
          return UnsafeUtility.As<int, T>(ref value);
        }
        else
        {
          Boolean value = Mathf.Approximately(value_float, 0);
          return UnsafeUtility.As<Boolean, T>(ref value);
        }
      case Type.Int:
        if (typeof(T) == typeof(int))
          return Value<T>();

        Assert.IsTrue(typeof(float) == typeof(T) || typeof(Boolean) == typeof(T));
        if (typeof(float) == typeof(T))
        {
          float value = value_int;
          return UnsafeUtility.As<float, T>(ref value);
        }
        else
        {
          Boolean value = value_int != 0;
          return UnsafeUtility.As<Boolean, T>(ref value);
        }
      case Type.Bool:
        if (typeof(T) == typeof(Boolean))
          return Value<T>();

        Assert.IsTrue(typeof(int) == typeof(T) || typeof(float) == typeof(T));
        if (typeof(int) == typeof(T))
        {
          int value = value_bool ? 1 : 0;
          return UnsafeUtility.As<int, T>(ref value);
        }
        else
        {
          float value = value_bool ? 1 : 0;
          return UnsafeUtility.As<float, T>(ref value);
        }
    }
    throw new NotSupportedException("Type must be a supported variant type.");
  }

  [Conditional("DEBUG")]
  private static void AssertType<T>(in Parameter variant)
  {
    switch (variant.type)
    {
      case Type.Float:
        Assert.AreEqual(typeof(float), typeof(T));
        break;
      case Type.Int:
        Assert.AreEqual(typeof(int), typeof(T));
        break;
      case Type.Bool:
        Assert.AreEqual(typeof(Boolean), typeof(T));
        break;
    }
  }
}
