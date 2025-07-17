using DevTools.UnitTesting;
using UnityEngine.Assertions;
using Vehicles.Rendering;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestCategoryNames.ParallelRenderer)]
[Disabled] // TODO VF-125
internal sealed class UnitTest_BlitTarget
{
  [Test]
  private void VehicleGraphic()
  {
    VehicleDef vehicleDef = TestDefGenerator.CreateTransientVehicleDef("VehicleDef_BlitTarget",
      new VehicleGroup.MockSettings
      {
      });
    BlitRequest request = BlitRequest.For(vehicleDef);
    Assert.AreEqual(request.blitTargets.Count, 1);
    Expect.IsNotNull(request.patternData);
    Expect.AreEqual(request.rot, vehicleDef.drawProperties.displayRotation);
    Expect.ReferencesAreEqual(request.blitTargets[0], vehicleDef);
  }

  [Test]
  private void VehicleTurret()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    BlitRequest request = BlitRequest.For(group.vehicle);
    Assert.AreEqual(request.blitTargets.Count, 1);
    Expect.ReferencesAreEqual(request.blitTargets[0], group.vehicle.VehicleDef);
  }
}