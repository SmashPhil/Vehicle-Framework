using System.Runtime.InteropServices;
using CoreLib.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal struct SnapshotBitOffsetsJob : IJobFor
{
  [ReadOnly]
  private NativeArrayList<ulong> bitArrays;
  [ReadOnly]
  private NativeArray<short> offsetSet;
  [ReadOnly]
  private NativeArray<short> offsetUnset;

  private NativeArray<int> snapshot;

  public SnapshotBitOffsetsJob(in NativeArray<int> snapshot, in Data data)
  {
    this.snapshot = snapshot;
    bitArrays = data.bitArrays;
    offsetSet = data.offsetSet;
    offsetUnset = data.offsetUnset;
  }

  public unsafe void Execute(int index)
  {
    int cost = snapshot[index];
    if (cost >= PathFinder.ImpassableCost)
      return;

    if (cost < 0)
    {
      snapshot[index] = PathFinder.ImpassableCost;
      return;
    }

    for (int j = 0; j < bitArrays.Size; j++)
    {
      cost += NativeBitArraySlim.IsSet(bitArrays[j], index) ? offsetSet[j] : offsetUnset[j];
    }
    snapshot[index] = math.clamp(cost, 0, PathFinder.ImpassableCost - 1);
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Data : INativeDisposable
  {
    [ReadOnly]
    public NativeArrayList<ulong> bitArrays;
    public NativeArray<short> offsetSet;
    public NativeArray<short> offsetUnset;

    public bool IsCreated => bitArrays.IsCreated && offsetSet.IsCreated && offsetUnset.IsCreated;

    public void Dispose()
    {
      bitArrays.Dispose();
      offsetSet.Dispose();
      offsetUnset.Dispose();
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
      if (!IsCreated)
        return default;

      JobHandle jobHandle = bitArrays.Dispose(inputDeps);
      jobHandle = offsetSet.Dispose(jobHandle);
      jobHandle = offsetUnset.Dispose(jobHandle);

      bitArrays = default;
      offsetSet = default;
      offsetUnset = default;
      return jobHandle;
    }
  }
}
