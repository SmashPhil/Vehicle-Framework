using DevTools.Testing;
using RimWorld;
using SmashTools;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("VehiclePawn reactor mechanics.")]
internal sealed class Test_VehiclePawn_Reactors
{
  [Test]
  private void ExplosiveUnspawned()
  {
    const string ComponentKey = "MockComp";
    const int MaxHealth = 100;
    const float ExplosionHealthPct = 1;
    const float ExplosionChance = 1;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          health = MaxHealth,

          reactors =
          [
            // Verify explosions don't attempt to spawn when vehicle is unspawned
            new Reactor_Explosive
            {
              healthPercent = ExplosionHealthPct,
              chance = ExplosionChance
            }
          ]
        }
      ]
    });

    DamageInfo damageInfo = new(DamageDefOf.Bullet, 10, armorPenetration: 1);
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.DamageTaken);
    component.TakeDamage(null, damageInfo);
    Expect.AreEqual(listener.CountRaised, 1);
  }
}