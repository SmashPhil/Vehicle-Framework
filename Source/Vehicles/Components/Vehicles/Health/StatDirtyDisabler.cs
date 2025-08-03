using System;

namespace Vehicles;

public readonly struct StatDirtyDisabler : IDisposable
{
  private readonly bool prevValue;
  private readonly VehicleStatHandler statHandler;

  public StatDirtyDisabler(VehiclePawn vehicle)
  {
    statHandler = vehicle.statHandler;
    prevValue = statHandler.CanDirty;
    statHandler.CanDirty = false;
  }

  public void Dispose()
  {
    statHandler.CanDirty = prevValue;
  }
}