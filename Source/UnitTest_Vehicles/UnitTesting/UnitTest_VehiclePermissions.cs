using DevTools.UnitTesting;
using UnityEngine.Assertions;
using Priority = DevTools.UnitTesting.Priority;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePermissions)]
[TestDescription("Vehicle permissions enable specific vehicle behavior with pawns on board.")]
internal sealed class UnitTest_VehiclePermissions
{
  private VehicleGroup manualVehicle;
  private VehicleGroup autonomousVehicle;
  private VehicleGroup immobileVehicle;

  [SetUp]
  private void GenerateVehicle()
  {
    manualVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.DriverNeeded,
      drivers = 1,
      passengers = 1
    });
    autonomousVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Autonomous,
      passengers = 1
    });
    immobileVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Immobile,
      drivers = 1,
      passengers = 1
    });
  }

  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void DestroyAll()
  {
    manualVehicle.Dispose();
    autonomousVehicle.Dispose();
    immobileVehicle.Dispose();

    TestUtils.EmptyWorldAndMapOfVehicles();
  }

  [Test] // Driver Required
  private void DriverPermissions_Manual()
  {
    Assert.IsNotNull(manualVehicle);

    manualVehicle.Spawn();
    Assert.IsTrue(manualVehicle.vehicle.Spawned);

    // Can move when role requirements satisifed
    Expect.IsTrue(manualVehicle.vehicle.CanMove);
    Expect.IsTrue(manualVehicle.vehicle.CanMoveWithOperators);

    // Cannot move when role requirements not satisfied
    manualVehicle.DisembarkAll();
    Expect.IsTrue(manualVehicle.vehicle.CanMove);
    Expect.IsFalse(manualVehicle.vehicle.CanMoveWithOperators);

    // Cannot move unless operator count is satisfied
    manualVehicle.BoardOne();

    Expect.IsTrue(manualVehicle.vehicle.CanMove);
    Expect.IsFalse(manualVehicle.vehicle.CanMoveWithOperators);

    manualVehicle.BoardAll();

    manualVehicle.vehicle.DeSpawn();
  }

  [Test] // Autonomous
  private void DriverPermissions_Autonomous()
  {
    Assert.IsNotNull(autonomousVehicle);

    autonomousVehicle.Spawn();
    Assert.IsTrue(autonomousVehicle.vehicle.Spawned);

    // Can move by default
    Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
    Expect.IsTrue(autonomousVehicle.vehicle.CanMoveWithOperators);

    // Can move even without any passengers
    autonomousVehicle.DisembarkAll();
    Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
    Expect.IsTrue(autonomousVehicle.vehicle.CanMoveWithOperators);

    // Boarding does not invalidate any movement permissions
    autonomousVehicle.BoardOne();
    Expect.IsTrue(autonomousVehicle.vehicle.CanMove);
    Expect.IsTrue(autonomousVehicle.vehicle.CanMoveWithOperators);

    autonomousVehicle.BoardAll();

    autonomousVehicle.vehicle.DeSpawn();
  }

  [Test] // Immobile
  private void DriverPermissions_Immobile()
  {
    Assert.IsNotNull(immobileVehicle);

    immobileVehicle.Spawn();
    Assert.IsTrue(immobileVehicle.vehicle.Spawned);

    // Cannot move by default
    Expect.IsFalse(immobileVehicle.vehicle.CanMove);
    Expect.IsFalse(immobileVehicle.vehicle.CanMoveWithOperators);

    // Disembarking does not enable movement permissions
    immobileVehicle.DisembarkAll();
    Expect.IsFalse(immobileVehicle.vehicle.CanMove);
    Expect.IsFalse(immobileVehicle.vehicle.CanMoveWithOperators);

    // Sanity check for single boarding event, should be the same as before
    immobileVehicle.BoardOne();
    Expect.IsFalse(immobileVehicle.vehicle.CanMove);
    Expect.IsFalse(immobileVehicle.vehicle.CanMoveWithOperators);

    immobileVehicle.BoardAll();

    immobileVehicle.vehicle.DeSpawn();
  }
}