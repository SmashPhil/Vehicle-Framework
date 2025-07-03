using DevTools.UnitTesting;
using SmashTools;

namespace Vehicles;

[StaticConstructorOnModInit]
public static class ModSettingsDisabler
{
  private static bool enabled;

  static ModSettingsDisabler()
  {
    UnitTestManager.OnUnitTestStateChange += DisableModifiableSettingsForTesting;
  }

  private static void DisableModifiableSettingsForTesting(bool state)
  {
    if (state)
      Disable();
    else
      Restore();
  }

  private static void Disable()
  {
    enabled = VehicleMod.settings.main.modifiableSettings;
    VehicleMod.settings.main.modifiableSettings = false;
  }

  private static void Restore()
  {
    VehicleMod.settings.main.modifiableSettings = enabled;
  }
}