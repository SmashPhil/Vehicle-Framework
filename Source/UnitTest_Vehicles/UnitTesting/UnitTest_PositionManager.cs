using DevTools.UnitTesting;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_PositionManager : UnitTest_MapTest
{
  [Test]
  private void Registration()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);

      int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);

      IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);

      VehiclePositionManager positionManager =
        map.GetDetachedMapComponent<VehiclePositionManager>();
      GenSpawn.Spawn(vehicle, root, map);
      HitboxTester<VehiclePawn> positionTester = new(vehicle, root,
        positionManager.ClaimedBy,
        (claimant) => claimant == vehicle);
      positionTester.Start();

      // Validate spawned vehicle claims rect in position manager
      Expect.IsTrue(positionTester.Hitbox(true), "Spawn");

      // Validate position set updates valid claims
      vehicle.Position = reposition;
      Expect.IsTrue(positionTester.Hitbox(true), "set_Position");
      vehicle.Position = root;

      // Validate rotation set updates valid claims
      vehicle.Rotation = Rot4.East;
      Expect.IsTrue(positionTester.Hitbox(true), "set_Rotation");
      vehicle.Rotation = Rot4.North;

      // Validate despawning releases claim in position manager
      vehicle.DeSpawn();
      Expect.IsTrue(positionTester.All(false), "DeSpawn");
    }
  }
}