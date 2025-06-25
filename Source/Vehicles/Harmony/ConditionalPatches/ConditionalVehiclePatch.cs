using SmashTools.Patching;

namespace Vehicles.Compatibility;

public abstract class ConditionalVehiclePatch : ConditionalPatch
{
  public override string SourceId => VehicleHarmony.VehiclesUniqueId;
}