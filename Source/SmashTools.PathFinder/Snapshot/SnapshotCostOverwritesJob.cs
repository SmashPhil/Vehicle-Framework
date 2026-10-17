using CoreLib.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal struct SnapshotCostOverwritesJob : IJobFor
{
  [ReadOnly]
  private readonly NativeArray<int>.ReadOnly source;
  [ReadOnly]
  private readonly NativeArrayList<short> extraCosts;

  [WriteOnly]
  private NativeArray<int> snapshot;

  public SnapshotCostOverwritesJob(in NativeArray<int>.ReadOnly source, in NativeArray<int> snapshot,
    in NativeArrayList<short> extraCosts)
  {
    this.source = source;
    this.snapshot = snapshot;
    this.extraCosts = extraCosts;
  }

  public void Execute(int index)
  {
    for (int j = 0; j < extraCosts.Size; j++)
    {
      int cost = source[index];
      if (cost is < 0 or >= PathFinder.ImpassableCost)
      {
        snapshot[index] = PathFinder.ImpassableCost;
        return;
      }
      cost += extraCosts[j, index];
      snapshot[index] = math.clamp(cost, 0, PathFinder.ImpassableCost - 1);
    }
  }
}
