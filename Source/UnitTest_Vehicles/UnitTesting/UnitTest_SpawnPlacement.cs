using DevTools.UnitTesting;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

// NOTE - Both GenAdj.OccupiedRect and GenSpawn.Spawn have patches that adjust positions for
// vehicles. We can verify the adjustment keeps the vehicle stable (and doesn't shift positions)
// by comparing the CellRects of entity-based occupied rect vs. size based (which is not patched)
[UnitTest(TestType.Playing)]
internal sealed class UnitTest_SpawnPlacement : UnitTest_MapTest
{
  [Test]
  private void PlacementDrift()
  {
    DeferredGridGeneration gridGen =
      map.GetCachedMapComponent<VehiclePathingSystem>().deferredGridGeneration;
    Assert.IsNotNull(gridGen);
    using DeferredGridGeneration.PassDisabler pd = new(gridGen);

    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);

      IntVec2 size = vehicle.VehicleDef.Size;

      // North
      CellRect occupiedRect = GenAdj.OccupiedRect(root, Rot4.North, size);
      GenSpawn.Spawn(vehicle, root, map, Rot4.North);
      Expect.AreEqual(occupiedRect, vehicle.OccupiedRect(), "North OccupiedRect");
      Expect.AreEqual(vehicle.Position, root, "North Position");

      vehicle.DeSpawn();
      Assert.IsFalse(vehicle.Spawned);
      Assert.IsFalse(vehicle.Destroyed);
      Assert.IsFalse(vehicle.Discarded);

      // East
      occupiedRect = GenAdj.OccupiedRect(root, Rot4.East, size);
      GenSpawn.Spawn(vehicle, root, map, Rot4.East);
      Expect.AreEqual(occupiedRect, vehicle.OccupiedRect(), "East OccupiedRect");
      Expect.AreEqual(vehicle.Position, root, "East Position");

      vehicle.DeSpawn();
      Assert.IsFalse(vehicle.Spawned);
      Assert.IsFalse(vehicle.Destroyed);
      Assert.IsFalse(vehicle.Discarded);

      // South
      occupiedRect = GenAdj.OccupiedRect(root, Rot4.South, size);
      GenSpawn.Spawn(vehicle, root, map, Rot4.South);
      Expect.AreEqual(occupiedRect, vehicle.OccupiedRect(), "South OccupiedRect");
      Expect.AreEqual(CorrectedPosition(vehicle, Rot4.South, vehicle.Position), root,
        "South Position");

      vehicle.DeSpawn();
      Assert.IsFalse(vehicle.Spawned);
      Assert.IsFalse(vehicle.Destroyed);
      Assert.IsFalse(vehicle.Discarded);

      // West
      occupiedRect = GenAdj.OccupiedRect(root, Rot4.West, size);
      GenSpawn.Spawn(vehicle, root, map, Rot4.West);
      Expect.AreEqual(occupiedRect, vehicle.OccupiedRect(), "West OccupiedRect");
      Expect.AreEqual(CorrectedPosition(vehicle, Rot4.West, vehicle.Position), root,
        "West Position");

      vehicle.Destroy();
      Assert.IsFalse(vehicle.Spawned);
      Assert.IsTrue(vehicle.Destroyed);
    }
    return;

    // Adjust position back to expected 'OccupiedRect' center based on RimWorld multi-cell
    // entity rotations. The spawning process will correct it opposite to this conversion,
    // we're validating that 'inverted' correction will result back to the root position.
    static IntVec3 CorrectedPosition(VehiclePawn vehicle, Rot4 rot, IntVec3 cell)
    {
      switch (rot.AsInt)
      {
        case 2:
          if (vehicle.VehicleDef.Size.x % 2 == 0)
            cell.x += 1;
          if (vehicle.VehicleDef.Size.z % 2 == 0)
            cell.z += 1;
        break;
        case 3:
          if (vehicle.VehicleDef.Size.x % 2 == 0)
            cell.z -= 1;
          if (vehicle.VehicleDef.Size.z % 2 == 0)
            cell.x += 1;
        break;
      }
      return cell;
    }
  }
}