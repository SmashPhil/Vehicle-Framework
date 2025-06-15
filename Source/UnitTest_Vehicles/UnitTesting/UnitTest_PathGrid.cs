using DevTools.UnitTesting;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_PathGrid : UnitTest_MapTest
{
  [Test]
  private void PathGrid()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);

      int maxSize = Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z);

      IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
      VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
      VehiclePathingSystem.VehiclePathData pathData = mapping[vehicle.VehicleDef];
      TerrainDef terrainDef = map.terrainGrid.TerrainAt(root);

      VehiclePathGrid pathGrid = pathData.VehiclePathGrid;
      GenSpawn.Spawn(vehicle, root, map);
      Assert.IsTrue(vehicle.Spawned);

      HitboxTester<int> positionTester = new(vehicle, root,
        (cell) => pathGrid.CalculatedCostAt(cell),
        (cost) => cost == VehiclePathGrid.TerrainCostAt(vehicle.VehicleDef, terrainDef));
      positionTester.Start();

      // Spawn
      Expect.IsTrue(positionTester.All(true), "VehiclePathGrid Spawn");

      // set_Position
      vehicle.Position = reposition;
      Expect.IsTrue(positionTester.All(true), "VehiclePathGrid set_Position");
      vehicle.Position = root;

      // set_Rotation
      vehicle.Rotation = Rot4.East;
      Expect.IsTrue(positionTester.All(true), "VehiclePathGrid set_Rotation");
      vehicle.Rotation = Rot4.North;

      // Despawn
      vehicle.DeSpawn();
      Expect.IsTrue(positionTester.All(true), "VehiclePathGrid DeSpawn");

      // Vanilla PathGrid costs should take vehicles into account
      PathGrid vanillaPathGrid = map.pathing.Normal.pathGrid;
      positionTester = new HitboxTester<int>(vehicle, root,
        (cell) => vanillaPathGrid.CalculatedCostAt(cell, true, IntVec3.Invalid),
        (cost) => cost == terrainDef.pathCost ||
          (cost == Verse.AI.PathGrid.ImpassableCost &&
            terrainDef.passability == Traversability.Impassable));
      positionTester.Start();

      GenSpawn.Spawn(vehicle, root, map);
      Assert.IsTrue(vehicle.Spawned, "Vehicle unable to spawn.");

      // Spawn
      Expect.IsTrue(terrainDef.passability == Traversability.Impassable ?
        positionTester.All(true) :
        positionTester.Hitbox(false), "PathGrid Spawn");

      // set_Position
      vehicle.Position = reposition;
      Expect.IsTrue(terrainDef.passability == Traversability.Impassable ?
        positionTester.All(true) :
        positionTester.Hitbox(false), "PathGrid set_Position");
      vehicle.Position = root;

      // set_Rotation
      vehicle.Rotation = Rot4.East;
      Expect.IsTrue(terrainDef.passability == Traversability.Impassable ?
        positionTester.All(true) :
        positionTester.Hitbox(false), "PathGrid set_Rotation");
      vehicle.Rotation = Rot4.North;

      // Despawn
      vehicle.DeSpawn();
      Expect.IsTrue(positionTester.All(true), "PathGrid DeSpawn");
    }
  }
}