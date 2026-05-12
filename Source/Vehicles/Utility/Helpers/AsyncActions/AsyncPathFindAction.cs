using System;
using CoreLib.PathFinding;
using CoreLib.Performance;
using JetBrains.Annotations;
using PathRequestStatus = Vehicles.VehiclePathFollower.PathRequestStatus;

namespace Vehicles;

public class AsyncPathFindAction : AsyncAction
{
  private VehiclePathFinder pathFinder;
  private Path.Node start;
  private Path.Node end;
  private PathSettings settings;

  private VehiclePathReceipt receipt;

  public override bool IsValid => receipt != null && !settings.token.IsCancellationRequested &&
                                  settings.vehicle is
                                  {
                                    Spawned: true,
                                    vehiclePather.Moving: true,
                                    vehiclePather.RequestStatus: PathRequestStatus.Calculating
                                  };

  public void Set([NotNull] VehiclePathReceipt pathReceipt, [NotNull] VehiclePathFinder vehiclePathFinder,
    Path.Node startNode, Path.Node destNode, in PathSettings pathSettings)
  {
    receipt = pathReceipt;
    start = startNode;
    end = destNode;
    settings = pathSettings;
    pathFinder = vehiclePathFinder;
  }

  public override void Invoke()
  {
    receipt.Path = pathFinder.FindPath(start, end, settings);
  }

  public override void ReturnToPool()
  {
    settings = default;
    pathFinder = null;
    receipt = null;
    AsyncPool<AsyncPathFindAction>.Return(this);
  }

  public override void ExceptionThrown(Exception ex)
  {
    // Clear destination targeted so request doesn't just get requeued again.
    settings.vehicle?.vehiclePather.PatherFailed();
  }
}