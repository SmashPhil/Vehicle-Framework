using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SmashTools.PathFinder
{
  public static class BurstUtils
  {
    public static unsafe void MemClear<T>(NativeArray<T> array) where T : unmanaged
    {
      UnsafeUtility.MemClear(array.GetUnsafePtr(), (long)array.Length * sizeof(T));
    }
  }
}