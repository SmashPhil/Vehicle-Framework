using System.Linq;
using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_GasGrid([ParametersSource(typeof(VehicleSources), "GridSizes")] IntVec2 size) : Test_MapTest
{
  private VehicleGroup group;
  private HitboxTester<bool> gasTester;
  private bool blocksGas;

  [SetUp]
  private void SetUp()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      size = size
    });
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    TestUtils.PrepareArea(map, TestArea(vehicleDef), vehicleDef);

    blocksGas = group.vehicle.VehicleDef.Fillage == FillCategory.Full;
    gasTester = new HitboxTester<bool>(group.vehicle, root,
      map.gasGrid.AnyGasAt,
      // Gas can only occupy if vehicle Fillage != Full
      (gasAt, _) => gasAt == (!group.vehicle.Spawned || !blocksGas),
      (_) => map.gasGrid.Debug_ClearAll());
    gasTester.Start();

    map.gasGrid.Debug_FillAll();
    Assert.IsTrue(TestArea(group.vehicle.VehicleDef).All(map.gasGrid.AnyGasAt));
    GenSpawn.Spawn(group.vehicle, root, map);
    Assert.IsTrue(group.vehicle.Spawned, "Vehicle needs to be spawned.");
  }

  [TearDown]
  private void TearDown()
  {
    gasTester.Reset();
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    group.Dispose();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.Destroyed);
    group = null;
    TestUtils.PrepareArea(map, TestArea(vehicleDef), vehicleDef);
    gasTester = null;
  }

  [Test]
  private void Spawn()
  {
    Expect.IsTrue(blocksGas ? gasTester.Hitbox(true) : gasTester.All(true));
  }

  [Test]
  private void DeSpawn()
  {
    group.vehicle.DeSpawn();
    map.gasGrid.Debug_FillAll();
    Expect.IsTrue(gasTester.All(true), "Despawned vehicles should not block gas.");
    gasTester.Reset();
  }

  [Test]
  private void SetPosition()
  {
    int maxSize = Mathf.Max(group.vehicle.VehicleDef.Size.x, group.vehicle.VehicleDef.Size.z);
    IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
    group.vehicle.Position = reposition;
    map.gasGrid.Debug_FillAll();
    Expect.IsTrue(blocksGas ? gasTester.Hitbox(true) : gasTester.All(true));
    group.vehicle.Position = root;
    gasTester.Reset();
  }

  [Test]
  private void SetRotation()
  {
    group.vehicle.Rotation = Rot4.East;
    map.gasGrid.Debug_FillAll();
    Expect.IsTrue(blocksGas ? gasTester.Hitbox(true) : gasTester.All(true));
    group.vehicle.Rotation = Rot4.North;
    gasTester.Reset();
  }
}