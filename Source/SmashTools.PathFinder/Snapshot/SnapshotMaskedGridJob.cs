using Unity.Collections;
using Unity.Jobs;

namespace SmashTools.PathFinder;

internal struct SnapshotMaskedGridJob : IJobFor
{
  [ReadOnly]
  private readonly NativeArray<int>.ReadOnly source;
  [ReadOnly]
  private readonly NativeArray<int>.ReadOnly mask;

  [WriteOnly]
  private NativeArray<int> snapshot;

  public SnapshotMaskedGridJob(in NativeArray<int>.ReadOnly source, in NativeArray<int>.ReadOnly mask,
    in NativeArray<int> snapshot)
  {
    this.source = source;
    this.mask = mask;
    this.snapshot = snapshot;
  }

  public void Execute(int index)
  {
    int overwrite = mask[index];
    snapshot[index] = overwrite > 0 ? overwrite : source[index];
  }
}
