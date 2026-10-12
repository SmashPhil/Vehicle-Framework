using DevTools.Testing;
using SmashTools;

namespace Vehicles;

[StaticConstructorOnModInit]
public static class ModSettingsDisabler
{
	private static bool enabled;

	private static bool draftAnyVehicles;
	private static bool instantSendOff;
	private static bool shootAnyTurret;

	static ModSettingsDisabler()
	{
		TestRunner.OnTestRunnerStateChange += DisableModifiableSettingsForTesting;
	}

	private static void DisableModifiableSettingsForTesting(bool state)
	{
    if (state)
    {
      Disable();
    }
    else
    {
      Restore();
    }
	}

	private static void Disable()
	{
		enabled = VehicleMod.settings.main.modifiableSettings;
		draftAnyVehicles = VehicleMod.settings.debug.debugDraftAnyVehicle;
		instantSendOff = VehicleMod.settings.debug.debugInstantSendOff;
		shootAnyTurret =VehicleMod.settings.debug.debugShootAnyTurret;

    VehicleMod.settings.main.modifiableSettings = false;
    VehicleMod.settings.debug.debugDraftAnyVehicle = false;
		VehicleMod.settings.debug.debugInstantSendOff = false;
		VehicleMod.settings.debug.debugShootAnyTurret = false;
	}

	private static void Restore()
	{
		VehicleMod.settings.main.modifiableSettings = enabled;
    VehicleMod.settings.debug.debugDraftAnyVehicle = draftAnyVehicles;
    VehicleMod.settings.debug.debugInstantSendOff = instantSendOff;
    VehicleMod.settings.debug.debugShootAnyTurret = shootAnyTurret;
  }
}