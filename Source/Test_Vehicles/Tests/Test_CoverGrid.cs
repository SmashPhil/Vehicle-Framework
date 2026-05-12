using DevTools.Testing;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_CoverGrid([ParametersSource(typeof(VehicleSources), "GridSizes")] IntVec2 size)
  : Test_MapTest
{
  private VehicleGroup group;
  private HitboxTester<Thing> coverTester;

  [SetUp]
  private void SetUp()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      size = size
    });
    group.vehicle.VehicleDef.fillPercent = 1;

    coverTester = new HitboxTester<Thing>(group.vehicle, root,
      cell => map.coverGrid[cell],
      (thing, _) => thing == group.vehicle);
    coverTester.Start();

    GenSpawn.Spawn(group.vehicle, root, map);
    Assert.IsTrue(group.vehicle.Spawned);
  }

  [TearDown]
  private void TearDown()
  {
    coverTester.Reset();
    group.Dispose();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.Destroyed);
    coverTester = null;
    group = null;
  }

  [Test]
  private void Spawn()
  {
    Expect.IsTrue(coverTester.Hitbox(true), "Vehicle should provide cover when spawned.");
  }

  [Test]
  private void DeSpawn()
  {
    group.DeSpawn();
    Expect.IsTrue(coverTester.All(false), "Vehicle should stop providing cover when despawned.");
  }

  [Test]
  private void SetPosition()
  {
    int maxSize = Mathf.Max(group.vehicle.VehicleDef.Size.x, group.vehicle.VehicleDef.Size.z);
    IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
    group.vehicle.Position = reposition;
    Expect.IsTrue(coverTester.Hitbox(true));
    group.vehicle.Position = root;
  }

  [Test]
  private void SetRotation()
  {
    group.vehicle.Rotation = Rot4.North;
    Expect.IsTrue(coverTester.Hitbox(true));
    group.vehicle.Rotation = Rot4.East;
    Expect.IsTrue(coverTester.Hitbox(true));
  }
}