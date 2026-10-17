using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SmashTools.PathFinder;

public static class NativeArrayUtility
{
  public static unsafe void SetAll<T>(NativeArray<T> array, byte value) where T : struct
  {
    UnsafeUtility.MemSet(array.GetUnsafePtr(), value, array.Length * UnsafeUtility.SizeOf<T>());
  }
}
