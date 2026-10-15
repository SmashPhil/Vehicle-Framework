using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace SmashTools.PathFinder;

/// <summary>
/// Copies all values from source to snapshot
/// </summary>
internal readonly struct SnapshotGridJob : IJob
{
  [ReadOnly]
  private readonly NativeArray<int>.ReadOnly source;
  [WriteOnly]
  private readonly NativeArray<int> snapshot;

  public SnapshotGridJob(in NativeArray<int>.ReadOnly source, in NativeArray<int> snapshot)
  {
    this.source = source;
    this.snapshot = snapshot;
  }

  public unsafe void Execute()
  {
    UnsafeUtility.MemCpy(snapshot.GetUnsafePtr(), source.GetUnsafeReadOnlyPtr(),
      sizeof(int) * source.Length);
  }
}
