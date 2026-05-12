using Verse;

namespace Vehicles;

public abstract class VehicleGridManager
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

  public VehicleDef CreatedFor => createdFor;

  public virtual void PostInit()
  {
  }

  protected internal virtual void ChangeOwner(VehicleDef newOwner)
  {
    createdFor = newOwner;
  }
}