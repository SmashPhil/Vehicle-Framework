using SmashTools.Performance;
using VehiclePathData = Vehicles.VehiclePathingSystem.VehiclePathData;

namespace Vehicles
{
  public class AsyncRebuildRegionsAction : AsyncAction
  {
    private VehiclePathData pathData;

    public void Set(VehiclePathData pathData)
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