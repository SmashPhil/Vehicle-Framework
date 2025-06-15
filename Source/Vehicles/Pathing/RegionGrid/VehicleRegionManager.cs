namespace Vehicles;

public abstract class VehicleGridManager
{
  protected readonly VehiclePathingSystem mapping;
  protected internal VehicleDef createdFor;

  protected VehicleGridManager(VehiclePathingSystem mapping, VehicleDef createdFor)
  {
    this.mapping = mapping;
    this.createdFor = createdFor;
  }

  public VehicleDef CreatedFor => createdFor;

  public virtual void PostInit()
  {
  }
}