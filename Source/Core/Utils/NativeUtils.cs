using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace CoreLib;

[PublicAPI]
public static class NativeUtils
{
  public static unsafe void MemClear<T>(NativeArray<T> array) where T : unmanaged
  {
    UnsafeUtility.MemClear(array.GetUnsafePtr(), (long)array.Length * sizeof(T));
  }
}
