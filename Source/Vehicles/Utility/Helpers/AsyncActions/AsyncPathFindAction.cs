using System;
using System.Threading;
using CoreLib.Performance;
using PathRequestStatus = Vehicles.VehiclePathFollower.PathRequestStatus;

namespace Vehicles
{
  public class AsyncPathFindAction : AsyncAction
  {
    private VehiclePawn vehicle;
    private CancellationToken token;

    public override bool IsValid => !token.IsCancellationRequested && vehicle is
    {
      Spawned: true, vehiclePather.Moving: true,
      vehiclePather.RequestStatus: PathRequestStatus.Calculating
    };

    public void Set(VehiclePawn vehicle, in CancellationToken token)
    {
      this.vehicle = vehicle;
      this.token = token;
    }

    public override void Invoke()
    {
      vehicle.vehiclePather.GeneratePath(token);
    }

    public override void ReturnToPool()
    {
      vehicle = null;
      AsyncPool<AsyncPathFindAction>.Return(this);
    }

    public override void ExceptionThrown(Exception ex)
    {
      // Clear destination targeted so request doesn't just get requeued again.
      vehicle.vehiclePather.PatherFailed();
    }
  }
}