using CoreLib.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal struct SnapshotOffsetsJob : IJobFor
{
  [ReadOnly]
  private readonly NativeArrayList<short> extraCosts;

  private NativeArray<int> snapshot;

  public SnapshotOffsetsJob(in NativeArray<int> snapshot, in NativeArrayList<short> extraCosts)
  {
    this.snapshot = snapshot;
    this.extraCosts = extraCosts;
  }

  public void Execute(int index)
  {
    int cost = snapshot[index];
    if (cost >= PathFinder.ImpassableCost)
      return;

    if (cost < 0)
    {
      snapshot[index] = PathFinder.ImpassableCost;
      return;
    }

    for (int j = 0; j < extraCosts.Size; j++)
    {
      cost += extraCosts[j, index];
    }
    snapshot[index] = math.clamp(cost, 0, PathFinder.ImpassableCost - 1);
  }
}
