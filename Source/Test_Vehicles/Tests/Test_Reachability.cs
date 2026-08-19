using DevTools.Testing;
using UnityEngine.Assertions;
using Verse;
using Priority = DevTools.Testing.Priority;

namespace Vehicles.Testing;

[Disabled] // TODO VF-391
[TestFixture(TestType.Playing)]
[TestDescription("Vehicle reachability across regions.")]
internal sealed class Test_Reachability([ParametersSource("VehicleSizes")] IntVec2 size) : Test_MapTest
{
  private VehicleGroup group;
  
  private MockPathingManager pathing;
  private RegionData regionData;

  private VehicleRegionGrid RegionGrid => regionData.regionGridManager[RegionGridType.Normal];

  private VehicleReachability RegionDirtyer => regionData.reachability;

  protected override CellRect TestArea(VehicleDef vehicleDef)
  {
    return VehicleRegion.ChunkAt(root).ContractedBy(vehicleDef.SizePadding);
  }

  [SetUp, ExecutionPriority(Priority.First)]
  private void SetUp()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      size = size
    });
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    Assert.IsTrue(PathingHelper.ShouldCreateRegions(vehicleDef));
    pathing = new MockPathingManager(map, vehicleDef);
    regionData = pathing.RegionData;
  }

  [TearDown]
  private void TearDown()
  {
    group.Dispose();
  }

  [Test]
  private void CanReach()
  {

  }

  private class MockCalculator(Map map) : IPathGridCalculator
  {
    private readonly BoolGrid impassableGrid = new(map);

    public ushort PathCostAt(Map map, IntVec3 cell, VehicleDef vehicleDef)
    {
      return (ushort)(impassableGrid[cell] ? VehiclePathGrid.ImpassableCost : 1);
    }

    public void SetPassable(IntVec3 cell, bool passable)
    {
      impassableGrid[cell] = !passable;
    }
  }

  private class MockPathingManager : IPathingManager
  {
    private readonly Map map;
    private readonly VehicleDef vehicleDef;

    public MockPathingManager(Map map, VehicleDef vehicleDef)
    {
      this.map = map;
      this.vehicleDef = vehicleDef;

      Calculator = new MockCalculator(map);
      PathGrid = new VehiclePathGrid(this, vehicleDef, Calculator);
      RegionData = new RegionData(this, vehicleDef, pathFinder: null);
      RegionData.PostInit();

      PathGrid.RecalculateAllPerceivedPathCosts();
      RegionData.regionAndRoomUpdater.Init();
      RegionData.regionAndRoomUpdater.RebuildAllVehicleRegions();
    }

    public RegionData RegionData { get; }

    public VehiclePathGrid PathGrid { get; }

    public MockCalculator Calculator { get; }

    Map IPathingManager.Map => map;

    MapGridOwners IPathingManager.GridOwners => throw new System.NotImplementedException();

    VehiclePathGrid IPathingManager.GetPathGrid(VehicleDef def)
    {
      Assert.AreEqual(vehicleDef, def);
      return PathGrid;
    }

    VehicleRegionGridManager IPathingManager.GetRegionGridManager(VehicleDef def)
    {
      Assert.AreEqual(vehicleDef, def);
      return RegionData.regionGridManager;
    }

    bool IPathingManager.IsPathDataSuspended(VehicleDef def)
    {
      Assert.AreEqual(vehicleDef, def);
      return false;
    }
  }
}
