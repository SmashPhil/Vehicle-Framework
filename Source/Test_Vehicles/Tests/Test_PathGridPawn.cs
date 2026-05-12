using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_PathGridPawn([DefParameter] VehicleDef vehicleDef)
  : Test_MapTest
{
  private VehicleGroup group;
  private HitboxTester<int> positionTester;

  [SetUp]
  private void SetUp()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      vehicleDef = vehicleDef,
      permissions = VehiclePermissions.Mobile,
      passengers = 1
    });

    positionTester = new HitboxTester<int>(group.vehicle, root,
      cell => map.pathing.Normal.pathGrid.CalculatedCostAt(cell, true, IntVec3.Invalid),
      (cost, cell) => cost == map.pathing.Normal.pathGrid.CalculatedCostAt(cell, true, IntVec3.Invalid));
    positionTester.Start();

    TestUtils.PrepareArea(map, root, vehicleDef);
    GenSpawn.Spawn(group.vehicle, root, map);
    Assert.IsTrue(group.vehicle.Spawned);
  }

  [TearDown]
  private void TearDown()
  {
    positionTester.Reset();
    group.Dispose();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.Destroyed);
    positionTester = null;
    group = null;
  }

  [Test]
  private void Spawn()
  {
    Expect.IsTrue(positionTester.All(true), "Vehicle should affect path under its hitbox when spawned.");
  }

  [Test]
  private void DeSpawn()
  {
    group.vehicle.DeSpawn();
    Expect.IsTrue(positionTester.All(true), "Vehicle should not affect path cost under its hitbox when spawned.");
  }

  [Test]
  private void SetPosition()
  {
    int maxSize = Mathf.Max(group.vehicle.VehicleDef.Size.x, group.vehicle.VehicleDef.Size.z);
    IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
    group.vehicle.Position = root;
    Expect.IsTrue(positionTester.All(true));
    group.vehicle.Position = reposition;
    Expect.IsTrue(positionTester.All(true));
  }

  [Test]
  private void SetRotation()
  {
    group.vehicle.Rotation = Rot4.North;
    Expect.IsTrue(positionTester.All(true));
    group.vehicle.Rotation = Rot4.East;
    Expect.IsTrue(positionTester.All(true));
  }
}