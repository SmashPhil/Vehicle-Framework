using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn)]
[Disabled]
internal sealed class UnitTest_VehiclePawn_Rendering
{
  [Test]
  private void ParallelRendererInit()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1,
      comps =
      [
        new CompProperties_VehicleTurrets()
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurret",
                graphicData = new GraphicDataRGB()
              }
            }
          ]
        }
      ]
    });
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    // TODO - count vs. expected parallel renders registered on init,
    // TODO - ensure despawn + respawn doesn't double register.
    // TODO - account for upgrades adding / removing parallel renderers
  }
}