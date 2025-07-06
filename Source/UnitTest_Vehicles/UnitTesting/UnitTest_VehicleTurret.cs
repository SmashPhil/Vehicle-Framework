using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehicleTurret, TestCategoryNames.TickBehavior, TestCategoryNames.Events)]
[TestDescription("VehicleTurret mechanics.")]
internal sealed class UnitTest_VehicleTurret
{
  // The test will fail if the method name changes, so there's no need to elevate access to VehicleTurret::ScanForTarget
  // when only the unit test wants external access.
  private const string ScanForTargetName = "ScanForTarget";

  [Test]
  private void AutoTargeting()
  {
    const string UpgradeKey = "Test Upgrade";

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurret"
              }
            }
          ]
        }
      ]
    });
    group.Spawn();
    Assert.IsNotNull(group.vehicle.CompVehicleTurrets);
    Assert.IsNotNull(group.vehicle.CompVehicleTurrets.Turrets);
    Assert.AreEqual(group.vehicle.CompVehicleTurrets.Turrets.Count, 1);

    VehicleTurret turret = group.vehicle.CompVehicleTurrets.Turrets[0];
    Expect.IsFalse(turret.AutoTarget);
    Expect.IsFalse(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
    turret.AutoTarget = true;
    Expect.IsTrue(turret.AutoTarget);
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));

    VehicleTurret upgradeTurretReference = new()
    {
      def = new VehicleTurretDef
      {
        defName = "MockTurret_Upgrade"
      }
    };
    VehicleTurret upgradeTurret = group.vehicle.CompVehicleTurrets.CopyAndAddTurret(upgradeTurretReference, UpgradeKey);
    Expect.IsFalse(upgradeTurret.AutoTarget);
    Expect.IsFalse(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{upgradeTurret.GetUniqueLoadID()}::{ScanForTargetName}"));
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
    upgradeTurret.AutoTarget = true;
    Expect.IsTrue(upgradeTurret.AutoTarget);
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{upgradeTurret.GetUniqueLoadID()}::{ScanForTargetName}"));
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
    group.vehicle.CompVehicleTurrets.RemoveTurret(upgradeTurret);
    Expect.IsFalse(upgradeTurret.AutoTarget);
    Expect.IsFalse(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{upgradeTurret.GetUniqueLoadID()}::{ScanForTargetName}"));
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
    group.vehicle.CompVehicleTurrets.AddTurret(upgradeTurret, UpgradeKey);
    // Removing turret resets auto target flag and de-registers event
    Expect.IsFalse(upgradeTurret.AutoTarget);
    Expect.IsFalse(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{upgradeTurret.GetUniqueLoadID()}::{ScanForTargetName}"));
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
    upgradeTurret.AutoTarget = true;
    Expect.IsTrue(upgradeTurret.AutoTarget);
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{upgradeTurret.GetUniqueLoadID()}::{ScanForTargetName}"));
    Expect.IsTrue(group.vehicle.EventRegistry[VehicleEventDefOf.ScanShort]
     .Contains($"{turret.GetUniqueLoadID()}::{ScanForTargetName}"));
  }
}