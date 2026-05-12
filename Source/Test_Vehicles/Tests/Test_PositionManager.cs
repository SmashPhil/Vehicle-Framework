using System.Linq;
using System.Runtime.CompilerServices;
using CoreLib;
using DevTools.Testing;
using SmashTools;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_PositionManager([ParametersSource("VehicleSizes")] IntVec2 size) : Test_MapTest
{
  private VehicleGroup group;

  private VehiclePositionManager PositionManager => map.GetDetachedMapComponent<VehiclePositionManager>();

  private EntityRect EntityRect
  {
    get
    {
      int2 position = new(group.vehicle.Position.x, group.vehicle.Position.z);
      int2 sizeInt = new(size.x, size.z);
      return new EntityRect(position, sizeInt, group.vehicle.FullRotation);
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private bool PositionClaimed(int2 cell)
  {
    return PositionManager.ClaimedBy(new IntVec3(cell.x, 0, cell.y)) == group.vehicle;
  }

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

    GenSpawn.Spawn(group.vehicle, root, map);
  }

  [TearDown]
  public void TearDown()
  {
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    group.Dispose();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.Destroyed);
    group = null;
    TestUtils.PrepareArea(map, TestArea(vehicleDef), vehicleDef);
  }

  [Test]
  public void Spawn()
  {
    Assert.IsTrue(group.vehicle.Spawned, "Spawned");
    Expect.IsTrue(EntityRect.All(PositionClaimed), "Spawn");
  }

  [Test]
  public void DeSpawn()
  {
    group.vehicle.DeSpawn();
    
    Expect.IsTrue(!EntityRect.Any(PositionClaimed), "Spawn");
  }

  [Test]
  public void SetPosition()
  {
    int maxSize = Mathf.Max(group.vehicle.VehicleDef.Size.x, group.vehicle.VehicleDef.Size.z);
    IntVec3 reposition = root + new IntVec3(maxSize, 0, 0);
    group.vehicle.Position = reposition;
    Expect.IsTrue(EntityRect.All(PositionClaimed), "set_Position");
    group.vehicle.Position = root;
  }

  [Test]
  public void SetRotation()
  {
    group.vehicle.Rotation = Rot4.East;
    Expect.IsTrue(EntityRect.All(PositionClaimed), "set_Rotation");
    group.vehicle.Rotation = Rot4.North;
  }
}