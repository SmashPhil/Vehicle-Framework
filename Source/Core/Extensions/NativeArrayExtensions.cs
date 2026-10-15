
using System;
using CoreLib.Collections;
using JetBrains.Annotations;
using Unity.Collections;

namespace CoreLib;

[PublicAPI]
public static class NativeArrayExtensions
{
  public static ManagedArrayHandle<T> AsNativeArray<T>(this T[] array,
    Allocator allocator = Allocator.Persistent) where T : unmanaged
  {
    ArgumentNullException.ThrowIfNull(array);
    return new ManagedArrayHandle<T>(array, allocator);
  }
}