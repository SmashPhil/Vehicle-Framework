using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.TickBehavior, TestCategoryNames.Events)]
[TestDescription("VehiclePawn idle mechanics for alert and dismounting.")]
internal sealed class Test_VehiclePawn_IdlePawns
{
  private const bool ForceRemoveAlert = false;

  private static readonly AccessTools.FieldRef<AlertsReadout, List<Alert>> ActiveAlertsRef =
    AccessTools.FieldRefAccess<AlertsReadout, List<Alert>>("activeAlerts");

  private static readonly AccessTools.FieldRef<VehiclePawn, int> TicksIdleRef =
    AccessTools.FieldRefAccess<VehiclePawn, int>("ticksIdle");

  private static readonly MethodInfo TickIntervalMethod = AccessTools.Method(typeof(VehiclePawn), "TickInterval");
  private static readonly MethodInfo CheckAddOrRemoveAlert = AccessTools.Method(typeof(AlertsReadout),
    "CheckAddOrRemoveAlert");

  private Alert_IdleInVehicle idleInVehicleAlert;

  private static bool HasAlert<T>() where T : Alert
  {
    return ActiveAlertsRef.Invoke(Find.Alerts).Exists(static alert => alert is T);
  }

  [OneTimeSetUp]
  private void GetAlerts()
  {
    List<Alert> alerts = (List<Alert>)AccessTools.Field(typeof(AlertsReadout), "AllAlerts").GetValue(Find.Alerts);
    idleInVehicleAlert = alerts.First(static alert => alert is Alert_IdleInVehicle) as Alert_IdleInVehicle;
    Assert.IsNotNull(idleInVehicleAlert);
  }

  [Test]
  [TestDescription("Pawns inside vehicle with tick threshold trigger alert.")]
  private void AlertIdleBoarded()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Assert.IsFalse(HasAlert<Alert_IdleInVehicle>());
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());
  }

  [Test]
  [TestDescription("Empty vehicle with tick threshold doesn't trigger alert.")]
  private void AlertIdleEmpty()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile
    });
    group.Spawn();

    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Assert.IsFalse(HasAlert<Alert_IdleInVehicle>());
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsFalse(HasAlert<Alert_IdleInVehicle>());
  }

  [Test]
  [TestDescription("Vehicle with alert triggered is reset when certain events are emitted.")]
  private void AlertIdleBoardedReset()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Assert.IsFalse(HasAlert<Alert_IdleInVehicle>());
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());

    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.PawnEntered));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.PawnChangedSeats));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.MoveStart));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.MoveStop));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.IgnitionOn));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.IgnitionOff));
    Expect.IsTrue(EventResetsIdleTicks(VehicleEventDefOf.DamageTaken));
    return;

    bool EventResetsIdleTicks(VehicleEventDef eventDef)
    {
      TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
      CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
      Assert.IsTrue(HasAlert<Alert_IdleInVehicle>());
      group.vehicle.EventRegistry[eventDef].ExecuteEvents();
      CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
      return !HasAlert<Alert_IdleInVehicle>();
    }
  }

  [Test]
  private void AlertIdleTurret()
  {
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

    VehicleTurret turret = group.vehicle.CompVehicleTurrets.Turrets.FirstOrDefault();
    Assert.IsNotNull(turret);

    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Assert.IsFalse(HasAlert<Alert_IdleInVehicle>());

    Expect.IsTrue(EventResetsIdleTicks(VehicleTurretEventDefOf.ShotFired));
    Expect.IsTrue(EventResetsIdleTicks(VehicleTurretEventDefOf.Reload));
    Expect.IsTrue(EventResetsIdleTicks(VehicleTurretEventDefOf.Warmup));
    return;

    bool EventResetsIdleTicks(VehicleTurretEventDef eventDef)
    {
      TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
      CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
      Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());
      turret.EventRegistry[eventDef].ExecuteEvents();
      CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
      return !HasAlert<Alert_IdleInVehicle>();
    }
  }

  [Test]
  private void AlertIdleInventory()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1
    });
    group.Spawn();

    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Assert.IsFalse(HasAlert<Alert_IdleInVehicle>());
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());
    group.vehicle.EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsFalse(HasAlert<Alert_IdleInVehicle>());
  }

  [Test]
  private void AlertIdleInventoryReset()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 2
    });
    group.Spawn();

    Pawn animalToRemove = group.pawns[0];

    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillAlert;
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());
    group.vehicle.inventory.innerContainer.TryDropOutsideVehicle(animalToRemove, group.vehicle.Map, group.vehicle.OccupiedRect());
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsTrue(HasAlert<Alert_IdleInVehicle>());
    group.vehicle.EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
    CheckAddOrRemoveAlert.Invoke(Find.Alerts, [idleInVehicleAlert, ForceRemoveAlert]);
    Expect.IsFalse(HasAlert<Alert_IdleInVehicle>());
  }

  [Test]
  private void DismountIdleColonistsBoarded()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    Pawn driver = group.pawns[0];

    Assert.AreEqual(group.vehicle.AllColonistsAboard.Count, 1);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
    Expect.IsFalse(driver.InVehicle());
    Expect.IsTrue(driver.Spawned);
  }

  [Test]
  private void DismountIdleColonistsInventory()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1
    });
    group.Spawn();

    Pawn driver = group.pawns[0];

    Assert.AreEqual(group.vehicle.AllInventoryPawns.Count, 1);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(group.vehicle.AllInventoryPawns.Count, 0);
    Expect.IsFalse(driver.InVehicle());
    Expect.IsTrue(driver.Spawned);
  }

  [Test]
  private void DismountIdleColonistsExitedEvent()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.PawnExited);
    Assert.AreEqual(expected: 1, group.vehicle.AllColonistsAboard.Count);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(expected: 0, group.vehicle.AllColonistsAboard.Count);
    Expect.AreEqual(expected: 1, listener.CountRaised);
  }

  [Test]
  private void DismountIdleColonistsInventoryExitedEvent()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1
    });
    group.Spawn();

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.PawnExited);
    Assert.AreEqual(expected: 1, group.vehicle.AllInventoryPawns.Count);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(expected: 0, group.vehicle.AllInventoryPawns.Count);
    Expect.AreEqual(expected: 1, listener.CountRaised);
  }

  [Test]
  private void DismountNoIdleColonistsExitedEvent()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();
    group.DisembarkAll();

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.PawnExited);
    Assert.AreEqual(expected: 0, group.vehicle.AllColonistsAboard.Count);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(expected: 0, group.vehicle.AllColonistsAboard.Count);
    Expect.AreEqual(expected: 0, listener.CountRaised);
  }

  [Test]
  private void DismountNoIdleColonistsInventoryExitedEvent()
  {
    const int TickInterval = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1
    });
    group.Spawn();
    group.DisembarkAll();

    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.PawnExited);
    Assert.AreEqual(expected: 0, group.vehicle.AllInventoryPawns.Count);
    TicksIdleRef(group.vehicle) = VehiclePawn.TicksTillDismount;
    TickIntervalMethod.Invoke(group.vehicle, [TickInterval]);
    Expect.AreEqual(expected: 0, group.vehicle.AllInventoryPawns.Count);
    Expect.AreEqual(expected: 0, listener.CountRaised);
  }
}