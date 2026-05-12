using System.Collections.Generic;
using DevTools.Testing;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_RegionGridOwner
{
  private VehicleGroup groupA;
  private VehicleGroup groupB;
  private MockPathingManager pathing;

  private VehicleDef VehicleDefA => groupA.vehicle.VehicleDef;

  private VehicleDef VehicleDefB => groupB.vehicle.VehicleDef;

  [SetUp]
  private void CreateGridOwners()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    groupA = CreateGroup(defIndex: 0);
    groupB = CreateGroup(defIndex: 1);
    pathing = new MockPathingManager(map, [VehicleDefA, VehicleDefB]);
    pathing.GridOwners.TransferOwnership(VehicleDefA);
    pathing.GridOwners.OnOwnershipTransfer += SwapRegionManagerOwners;
    return;

    static VehicleGroup CreateGroup(int defIndex)
    {
      VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
      {
        permissions = VehiclePermissions.Mobile,
        passengers = 1,
        size = new IntVec2(1, 1)
      });
      VehicleDef vehicleDef = group.vehicle.VehicleDef;
      vehicleDef.DefIndex = defIndex;
      Assert.IsTrue(PathingHelper.ShouldCreateRegions(vehicleDef));
      return group;
    }
  }

  [TearDown]
  private void DestroyVehicleGroups()
  {
    groupA.Dispose();
    groupB.Dispose();
    groupA = null;
    groupB = null;
    pathing = null;
  }

  [Test]
  private void GridManagerUpdatesOwner()
  {
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB));
    RegionData regionData = pathing.PathDataContainer[VehicleDefA].RegionData;
    Assert.AreEqual(regionData, pathing.PathDataContainer[VehicleDefB].RegionData);
    foreach (VehicleGridManager gridManager in regionData.AllGridManagers)
    {
      Expect.ReferencesAreEqual(VehicleDefA, gridManager.CreatedFor,
        $"{gridManager.GetType().Name}::createdFor doesn't map to correct VehicleDef.");
    }
    pathing.GridOwners.TransferOwnership(VehicleDefB);
    foreach (VehicleGridManager gridManager in regionData.AllGridManagers)
    {
      Expect.ReferencesAreEqual(VehicleDefB, gridManager.CreatedFor,
        $"{gridManager.GetType().Name}::createdFor doesn't map to correct VehicleDef.");
    }
  }

  private void SwapRegionManagerOwners(VehicleDef from, VehicleDef to)
  {
    PathData pathData = pathing.PathDataContainer[from];
    // Should be same instance for ownership to be transferable.
    Assert.IsTrue(pathData.RegionData == pathing.PathDataContainer[to].RegionData);
    pathData.RegionData.ChangeOwner(to);
  }

  private class MockPathingManager : IPathingManager
  {
    private readonly Map map;

    public MockPathingManager(Map map, List<VehicleDef> vehicleDefs)
    {
      this.map = map;

      GridOwners = new MapGridOwners(this, vehicleDefs);
      PathDataContainer = new PathDataContainer(this, vehicleDefs);
      PathDataContainer.GenerateAllPathData(new MockCalculator());
    }

    public PathDataContainer PathDataContainer { get; }

    Map IPathingManager.Map => map;

    public MapGridOwners GridOwners { get; }
    
    public VehiclePathGrid GetPathGrid(VehicleDef def)
    {
      return PathDataContainer[def.DefIndex].VehiclePathGrid;
    }

    VehicleRegionGridManager IPathingManager.GetRegionGridManager(VehicleDef def)
    {
      return PathDataContainer[def.DefIndex].VehicleRegionGridManager;
    }

    bool IPathingManager.IsPathDataSuspended(VehicleDef def)
    {
      return false;
    }
  }

  private class MockCalculator : IPathGridCalculator
  {
    public ushort PathCostAt(Map map, IntVec3 cell, VehicleDef vehicleDef)
    {
      return 1;
    }
  }
}