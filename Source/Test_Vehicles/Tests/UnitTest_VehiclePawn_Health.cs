using DevTools.Testing;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("VehiclePawn damage and health mechanics.")]
internal sealed class UnitTest_VehiclePawn_Health
{
  [Test]
  private void DamageEvent()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float Damage = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth
        }
      ]
    });
    group.Spawn();
    Assert.IsTrue(group.vehicle.statHandler.CanDirty);

    DamageInfo damageInfo = new(DamageDefOf.Bullet, Damage, armorPenetration: 1);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using EventListener<VehicleEventDef> dmgListener = new(group.vehicle, VehicleEventDefOf.DamageTaken);
    using EventListener<VehicleEventDef> hpListener = new(group.vehicle, VehicleEventDefOf.HealthChanged);
    component.TakeDamage(null, damageInfo);
    Expect.AreEqual(dmgListener.CountRaised, 1);
    Expect.AreEqual(hpListener.CountRaised, 1);
  }

  [Test]
  private void RepairEvent()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float Heal = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth
        }
      ]
    });
    group.Spawn();
    Assert.IsTrue(group.vehicle.statHandler.CanDirty);

    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    component.SetHealth(MaxHealth / 2f);
    using EventListener<VehicleEventDef> repairListener = new(group.vehicle, VehicleEventDefOf.Repaired);
    using EventListener<VehicleEventDef> hpListener = new(group.vehicle, VehicleEventDefOf.HealthChanged);
    component.HealComponent(0);
    Expect.AreEqual(repairListener.CountRaised, 0);
    Expect.AreEqual(hpListener.CountRaised, 0);
    component.HealComponent(Heal);
    Expect.AreEqual(repairListener.CountRaised, 1);
    Expect.AreEqual(hpListener.CountRaised, 1);
    component.HealComponent(MaxHealth);
    Expect.AreEqual(repairListener.CountRaised, 2);
    Expect.AreEqual(hpListener.CountRaised, 2);
  }

  [Test]
  private void StatCaching()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float Damage = 1;

    VehicleStatDef statDef = VehicleStatDefOf.RepairRate;

    VehicleGroup.MockSettings settings = new()
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth
        }
      ]
    };
    VehicleDef def = VehicleGroup.CreateVehicleDef(settings);
    def.statEvents =
    [
      // There are stat value pulls for BodyIntegrity that would immediately recache the stat,
      // but we need to test before that happens so use atypical event listener and stat def. 
      new StatCache.EventLister
      {
        statDef = statDef,
        eventDefs = [VehicleEventDefOf.DamageTaken]
      }
    ];
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);

    group.Spawn();
    Assert.IsTrue(group.vehicle.statHandler.CanDirty);

    _ = group.vehicle.GetStatValue(statDef); // Trigger stat cache if it hasn't already
    Assert.IsFalse(group.vehicle.statHandler.statCache.IsDirty(statDef));
    DamageInfo damageInfo = new(DamageDefOf.Bullet, Damage, armorPenetration: 1);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    component.TakeDamage(null, damageInfo);
    Expect.IsTrue(group.vehicle.statHandler.statCache.IsDirty(statDef));
    _ = group.vehicle.GetStatValue(statDef);
    Expect.IsFalse(group.vehicle.statHandler.statCache.IsDirty(statDef));
  }

  [Test]
  private void Destroy()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float Damage = MaxHealth;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth
        }
      ]
    });
    group.Spawn();
    Assert.IsTrue(group.vehicle.statHandler.CanDirty);

    DamageInfo damageInfo = new(DamageDefOf.Bullet, Damage, armorPenetration: 1);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.DamageTaken);
    Expect.AreEqual(group.vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity), 1);
    component.TakeDamage(null, damageInfo);
    Assert.AreEqual(component.Health, 0);
    Assert.AreEqual(component.HealthPercent, 0);
    Expect.IsTrue(group.vehicle.Destroyed);
    Expect.AreEqual(listener.CountRaised, 1);
  }
}