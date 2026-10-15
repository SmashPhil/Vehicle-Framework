using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal struct SnapshotEntityCostsJob : IJobFor
{
  private readonly int entityId;
  private readonly int costToOverlap;

  [ReadOnly]
  private NativeArray<int>.ReadOnly entityGrid;

  private NativeArray<int> snapshot;

  public SnapshotEntityCostsJob(in EntityConfig config, in NativeArray<int> snapshot)
  {
    entityId = config.entityId;
    costToOverlap = config.costToOverlap;
    entityGrid = config.entityGrid;
    this.snapshot = snapshot;
  }

  public void Execute(int index)
  {
    int otherEntity = entityGrid[index];
    if (otherEntity >= 0 && otherEntity != entityId)
    {
      snapshot[index] = math.clamp(snapshot[index] + costToOverlap, 0, PathFinder.ImpassableCost);
    }
  }
}
