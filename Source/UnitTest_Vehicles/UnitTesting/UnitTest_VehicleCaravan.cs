using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.WorldPawnGC, TestCategoryNames.WorldObject)]
[TestDescription("VehicleCaravan mechanics on the world map.")]
internal sealed class UnitTest_VehicleCaravan
{
  [Test]
  private void GetCaravan()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    Assert.AreEqual(group.pawns.Count, 1);

    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    Assert.AreEqual(vehicleCaravan, group.vehicle.GetVehicleCaravan());
    Assert.AreEqual(vehicleCaravan, group.pawns[0].GetVehicleCaravan());
    Expect.AreEqual(vehicleCaravan, group.vehicle.GetCaravan());
    Assert.AreEqual(vehicleCaravan, group.pawns[0].GetCaravan());

    vehicleCaravan.RemoveAllPawns();
  }

  [Test]
  private void Visibility()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 5,
      properties = new VehicleProperties
      {
        visibilityWeight = 6
      }
    });
    Assert.AreEqual(group.pawns.Count, 6);

    // Base game caravans should behave as expected
    Caravan caravan = CaravanMaker.MakeCaravan(group.pawns, Faction.OfPlayer, 1, true);
    float visibility = CaravanVisibilityCalculator.Visibility(caravan);
    // weight = 6
    Assert.AreApproximatelyEqual(caravan.Visibility, CaravanVisibilityCalculator.NotMovingFactor);
    Assert.AreApproximatelyEqual(visibility, CaravanVisibilityCalculator.NotMovingFactor);

    caravan.RemoveAllPawns();
    caravan.Destroy();

    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    Assert.IsFalse(vehicleCaravan.vehiclePather.MovingNow);
    Assert.AreEqual(vehicleCaravan.pawns.Count, 1);
    Assert.AreEqual(vehicleCaravan.pawns[0], group.vehicle);
    visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);

    // weight = 6
    Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
      1 * CaravanVisibilityCalculator.NotMovingFactor);
    Expect.AreApproximatelyEqual(visibility, 1 * CaravanVisibilityCalculator.NotMovingFactor);
    group.vehicle.DisembarkAll();
    Assert.AreEqual(vehicleCaravan.pawns.Count, 7);

    // weight = 12
    visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);
    Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
      1.12f * CaravanVisibilityCalculator.NotMovingFactor);
    Expect.AreApproximatelyEqual(visibility, 1.12f * CaravanVisibilityCalculator.NotMovingFactor);

    // Moving
    visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    group.BoardAll();
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1);

    // Pawns inside vehicles (returned by getter) should not count in visibility
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1);

    // Visibility is capped at 112%
    group.vehicle.VehicleDef.properties.visibilityWeight = 999;
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    group.DisembarkAll();
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);

    vehicleCaravan.RemoveAllPawns();
  }

  [Test]
  private void NeedsTracker()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
  }
}