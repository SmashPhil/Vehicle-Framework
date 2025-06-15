using DevTools.UnitTesting;
using UnityEngine;
using Verse;
using TestType = DevTools.UnitTesting.TestType;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_CoverGrid : UnitTest_MapTest
{
  [Test]
  private void CoverGrid()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);

      int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);

      IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
      CoverGrid coverGrid = map.coverGrid;
      GenSpawn.Spawn(vehicle, root, map);
      HitboxTester<Thing> coverTester = new(vehicle, root,
        (cell) => coverGrid[cell],
        (thing) => thing == vehicle);
      coverTester.Start();

      // Validate spawned vehicle shows up in cover grid
      Expect.IsTrue(coverTester.Hitbox(true), "Spawned");

      // Validate position set moves vehicle in cover grid
      vehicle.Position = reposition;
      Expect.IsTrue(coverTester.Hitbox(true), "set_Position");
      vehicle.Position = root;

      // Validate rotation set moves vehicle in cover grid
      vehicle.Rotation = Rot4.East;
      Expect.IsTrue(coverTester.Hitbox(true), "set_Rotation");
      vehicle.Rotation = Rot4.North;

      // Validate despawning reverts back to thing before vehicle was spawned
      vehicle.Destroy();
      Expect.IsTrue(coverTester.All(false), "DeSpawned");
    }
  }
}