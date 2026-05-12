using System;
using CoreLib.Performance;

namespace Vehicles;

[Obsolete] // TODO 1.7 - Remove
public class AsyncConnectionAction : AsyncAction
{
  private VehicleRegionConnector connector;
  private VehicleRegion region;

  public override bool IsValid => region is
    { InPool: false, valid: true, Map.Disposed: false };

  public void Set(VehicleRegionConnector connector, VehicleRegion region)
  {
    this.connector = connector;
    this.region = region;
  }

  public override void Invoke()
  {
    connector.RecalculateWeights(region);
  }

  public override void ReturnToPool()
  {
    connector = null;
    region = null;
    AsyncPool<AsyncConnectionAction>.Return(this);
  }
}