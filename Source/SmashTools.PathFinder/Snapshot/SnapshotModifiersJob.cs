using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal struct SnapshotModifiersJob : IJobFor
{
  [ReadOnly]
  private readonly NativeArray<Modifier>.ReadOnly modifiers;

  private NativeArray<int> snapshot;

  public SnapshotModifiersJob(in NativeArray<int> snapshot, in NativeArray<Modifier>.ReadOnly modifiers)
  {
    this.snapshot = snapshot;
    this.modifiers = modifiers;
  }

  public void Execute(int index)
  {
    int cost = snapshot[index];
    if (cost is < 0 or >= PathFinder.ImpassableCost)
      return;

    Modifier modifier = modifiers[index];
    cost = Modifier.Apply(modifier, cost);
    snapshot[index] = math.clamp(cost, 0, PathFinder.ImpassableCost - 1);
  }
}
