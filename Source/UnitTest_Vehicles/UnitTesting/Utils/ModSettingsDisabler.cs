using DevTools.Testing;
using SmashTools;

namespace Vehicles;

[StaticConstructorOnModInit]
public static class ModSettingsDisabler
{
	private static bool enabled;

	private static SmashTools.ScopedValueRollback<bool> draftAnyVehicles;
	private static SmashTools.ScopedValueRollback<bool> instantSendOff;
	private static SmashTools.ScopedValueRollback<bool> shootAnyTurret;

	static ModSettingsDisabler()
	{
		TestRunner.OnTestRunnerStateChange += DisableModifiableSettingsForTesting;
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
		draftAnyVehicles = new SmashTools.ScopedValueRollback<bool>(ref VehicleMod.settings.debug.debugDraftAnyVehicle);
		instantSendOff = new SmashTools.ScopedValueRollback<bool>(ref VehicleMod.settings.debug.debugInstantSendOff);
		shootAnyTurret = new SmashTools.ScopedValueRollback<bool>(ref VehicleMod.settings.debug.debugShootAnyTurret);

		VehicleMod.settings.debug.debugDraftAnyVehicle = false;
		VehicleMod.settings.debug.debugInstantSendOff = false;
		VehicleMod.settings.debug.debugShootAnyTurret = false;
	}

	private static void Restore()
	{
		VehicleMod.settings.main.modifiableSettings = enabled;

		draftAnyVehicles.Dispose();
		instantSendOff.Dispose();
		shootAnyTurret.Dispose();
	}
}