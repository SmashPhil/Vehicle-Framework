namespace Vehicles.Testing;

public static class CompGenerator
{
  public static CompProperties_VehicleLauncher CompPropertiesVehicleLauncher => new()
  {
    compClass = typeof(CompVehicleLauncher),
    launchProtocol = new DefaultTakeoff
    {
      launchProperties = new LaunchProtocolProperties(),
      landingProperties = new LaunchProtocolProperties()
    }
  };
}