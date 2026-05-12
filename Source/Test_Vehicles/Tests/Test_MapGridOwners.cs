using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_MapGridOwners
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
    groupA = CreateGroup(0);
    groupB = CreateGroup(1);
    pathing = new MockPathingManager(map, [VehicleDefA, VehicleDefB]);
    pathing.GridOwners.TransferOwnership(VehicleDefA);
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
  private void IsOwnerByVehicleDef()
  {
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB));
  }

  [Test]
  private void IsOwnerById()
  {
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
  }

  [Test]
  private void GetOwner()
  {
    Expect.ReferencesAreEqual(VehicleDefA, pathing.GridOwners.GetOwner(VehicleDefA));
    Expect.ReferencesAreEqual(VehicleDefA, pathing.GridOwners.GetOwner(VehicleDefB));
  }

  [Test]
  private void AllPiggies()
  {
    var piggies = pathing.GridOwners.AllPiggies.ToList();
    Expect.AreEqual(expected: 1, piggies.Count);
    Expect.ReferencesAreEqual(VehicleDefB, piggies[0]);
  }

  [Test]
  private void GetPiggies()
  {
    var piggies = pathing.GridOwners.GetPiggies(VehicleDefA).ToList();
    Assert.AreEqual(expected: 1, piggies.Count);
    Expect.ReferencesAreEqual(VehicleDefB, piggies[0]);
  }

  [Test]
  private void TransferOwnershipToPiggy()
  {
    Assert.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Assert.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
    pathing.GridOwners.TransferOwnership(VehicleDefB);
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
  }

  [Test]
  private void TransferOwnershipExistingOwner()
  {
    Assert.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Assert.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
    pathing.GridOwners.TransferOwnership(VehicleDefA);
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
  }

  [Test]
  private void TryForfeitOwnershipToPiggy()
  {
    Assert.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Assert.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
    Assert.IsTrue(pathing.GridOwners.TryForfeitOwnership(VehicleDefA));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
  }

  [Test]
  private void TryForfeitOwnershipNoPiggy()
  {
    Assert.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Assert.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
    pathing.Release(VehicleDefB);
    Assert.IsFalse(pathing.GetPathGrid(VehicleDefB).Enabled);
    // Vehicle A will still retain ownership, but forfeiting ownership is usually followed up with
    // disabling the path grid / region grid, so it will transfer ownership whenever a piggy
    // enables its path grid.
    Assert.IsFalse(pathing.GridOwners.TryForfeitOwnership(VehicleDefA));
    Expect.IsTrue(pathing.GridOwners.IsOwner(VehicleDefA.DefIndex));
    Expect.IsFalse(pathing.GridOwners.IsOwner(VehicleDefB.DefIndex));
  }

  private class MockPathingManager : IPathingManager
  {
    private readonly Map map;

    public MockPathingManager(Map map, List<VehicleDef> vehicleDefs)
    {
      this.map = map;

      PathGrids = new VehiclePathGrid[vehicleDefs.Count];
      RegionData = new RegionData(this, vehicleDefs[0], pathFinder: null);
      foreach (VehicleDef vehicleDef in vehicleDefs)
      {
        PathGrids[vehicleDef.DefIndex] = new VehiclePathGrid(this, vehicleDef, new MockCalculator());
      }

      GridOwners = new MapGridOwners(this, vehicleDefs);

      RegionData.PostInit();
      foreach (VehicleDef vehicleDef in vehicleDefs)
      {
        PathGrids[vehicleDef.DefIndex].RecalculateAllPerceivedPathCosts();
        RegionData.regionAndRoomUpdater.Init();
        RegionData.regionAndRoomUpdater.RebuildAllVehicleRegions();
      }
    }

    private RegionData RegionData { get; }

    private VehiclePathGrid[] PathGrids { get; }

    Map IPathingManager.Map => map;

    public MapGridOwners GridOwners { get; }

    public void Release(VehicleDef vehicleDef)
    {
      GetPathGrid(vehicleDef).Release();
      if (GridOwners.IsOwner(vehicleDef) && !GridOwners.TryForfeitOwnership(vehicleDef))
      {
        RegionData.regionAndRoomUpdater.Release();
      }
    }

    public VehiclePathGrid GetPathGrid(VehicleDef def)
    {
      return PathGrids[def.DefIndex];
    }

    VehicleRegionGridManager IPathingManager.GetRegionGridManager(VehicleDef def)
    {
      return RegionData.regionGridManager;
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