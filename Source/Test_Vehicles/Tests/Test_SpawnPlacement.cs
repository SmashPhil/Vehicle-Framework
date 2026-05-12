using System.Linq;
using DevTools.Testing;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

// NOTE - Both GenAdj.OccupiedRect and GenSpawn.Spawn have patches that adjust positions for
// vehicles. We can verify the adjustment keeps the vehicle stable (and doesn't shift positions)
// by comparing the CellRects of entity-based occupied rect vs. size based (which is not patched)
[TestFixture(TestType.Playing)]
internal sealed class Test_SpawnPlacement([ParametersSource("VehicleSizes")] IntVec2 size) : Test_MapTest
{
  private VehicleGroup group;

  private DeferredGridGeneration DeferredGrid =>
    map.GetCachedMapComponent<VehiclePathingSystem>().deferredGridGeneration;

  [SetUp]
  public void SetUp()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      size = size
    });
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    TestUtils.PrepareArea(map, TestArea(vehicleDef), vehicleDef);

    map.gasGrid.Debug_FillAll();
    Assert.IsTrue(TestArea(group.vehicle.VehicleDef).All(map.gasGrid.AnyGasAt));
    GenSpawn.Spawn(group.vehicle, root, map);
  }

  [TearDown]
  public void TearDown()
  {
    group.Dispose();
    group = null;
  }

  [Test]
  private void PlacementDrift()
  {
    using DeferredGridGeneration.PassDisabler pd = new(DeferredGrid);

    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, map, TestArea(vehicle.VehicleDef));

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