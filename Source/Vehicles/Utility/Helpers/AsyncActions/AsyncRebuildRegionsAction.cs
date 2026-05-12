using CoreLib.Performance;

namespace Vehicles
{
  public class AsyncRebuildRegionsAction : AsyncAction
  {
    private PathData pathData;

    public void Set(PathData pathData)
    {
      this.pathData = pathData;
    }

    public override void Invoke()
    {
      // It's fine if there's nothing to update due to duplicate enqueues, this won't
      // trigger a forced region rebuild, it will only check dirty cells and see if there's
      // any regions that still need refreshing.
      pathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
    }

    public override void ReturnToPool()
    {
      AsyncPool<AsyncRebuildRegionsAction>.Return(this);
    }
  }
}