using JetBrains.Annotations;
using Verse;

namespace Vehicles;

public sealed class FishTask : IVehicleTask
{
  private VehiclePawn vehicle;

  private int ticksFished;

  [UsedWithReflection]
  public FishTask()
  {
  }

  public FishTask(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  bool IVehicleTask.ShouldFail => false;

  bool IVehicleTask.ShouldEnd
  {
    get
    {
      if (!vehicle.fishingTracker.CanFish || !vehicle.fishingTracker.HasWorkers)
        return true;

      // TODO - Better end condition, this is just for testing
      return ticksFished > 1600;
    }
  }

  string IVehicleTask.GetReportString(Pawn pawn)
  {
    return "VF_FishingInVehicle".Translate(vehicle.LabelCap);
  }

  void IVehicleTask.StartTask()
  {
    vehicle.fishingTracker.StartFishing();
  }

  void IVehicleTask.FinishTask()
  {
    vehicle.fishingTracker.StopFishing();
  }

  void IVehicleTask.TaskTick()
  {
    ticksFished++;
  }

  void IExposable.ExposeData()
  {
    Scribe_References.Look(ref vehicle, nameof(vehicle));
  }
}
