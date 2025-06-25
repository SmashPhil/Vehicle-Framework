// Must use single name for namespace, RimWorld's ParseHelper::ParseAction does not support
// sub namespaces and will fail to resolve the type.

namespace Vehicles;

public static class ModSettingsDisabler
{
  private static bool enabled;

  public static void Disable()
  {
    enabled = VehicleMod.settings.main.modifiableSettings;
    VehicleMod.settings.main.modifiableSettings = false;
  }

  public static void Restore()
  {
    VehicleMod.settings.main.modifiableSettings = enabled;
  }
}