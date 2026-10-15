using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace CoreLib;

[PublicAPI]
public static class MathUtils
{
  public static readonly float Sqrt2 = Mathf.Sqrt(2);

  extension(int value)
  {
    /// <summary>
    /// Integer is odd
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOdd()
    {
      return (value & 1) != 0;
    }

    /// <summary>
    /// Integer is even
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEven()
    {
      return (value & 1) == 0;
    }
  }
}