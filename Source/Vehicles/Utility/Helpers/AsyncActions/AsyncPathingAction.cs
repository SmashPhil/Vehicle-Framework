using CoreLib.Performance;
using Verse;

namespace Vehicles;

public class AsyncPathingAction : AsyncAction
{
  private VehiclePathingSystem mapping;
  private IntVec3 position;

  public override bool IsValid => mapping?.map is { Disposed: false };

  public void Set(VehiclePathingSystem mapping, IntVec3 position)
  {
    this.mapping = mapping;
    this.position = position;
  }

  public override void Invoke()
  {
    PathingHelper.RecalculatePerceivedPathCostAtFor(mapping, position);
  }

  public override void ReturnToPool()
  {
    mapping = null;
    AsyncPool<AsyncPathingAction>.Return(this);
  }
}