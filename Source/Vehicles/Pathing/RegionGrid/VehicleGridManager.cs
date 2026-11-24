using System;
using Verse;

namespace Vehicles;

public abstract class VehicleGridManager : IDisposable
{
  protected readonly IPathingManager pathing;
  protected readonly Map map;
  protected VehicleDef createdFor;

  protected VehicleGridManager(IPathingManager pathing, VehicleDef createdFor)
  {
    this.pathing = pathing;
    map = pathing.Map;
    this.createdFor = createdFor;
  }

  protected internal bool Disposed { get; private set; }

  public VehicleDef CreatedFor => createdFor;

  public virtual void PostInit()
  {
  }

  protected internal virtual void ChangeOwner(VehicleDef newOwner)
  {
    createdFor = newOwner;
  }

  public virtual void Dispose()
  {
    if (Disposed)
      return;

    Disposed = true;
    GC.SuppressFinalize(this);
  }
}