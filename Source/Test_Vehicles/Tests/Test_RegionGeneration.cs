using System.Collections.Generic;
using CoreLib.Performance;
using DevTools.Testing;
using HarmonyLib;
using UnityEngine.Assertions;
using Verse;
using Priority = DevTools.Testing.Priority;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_RegionGeneration([ParametersSource("VehicleSizes")] IntVec2 size) : Test_MapTest
{
  private static readonly
    AccessTools.FieldRef<VehicleRegionMaker, ObjectPool<VehicleRegion>> RegionPoolRef;
  private static readonly
    AccessTools.FieldRef<VehicleRegionLinkDatabase, ObjectPool<VehicleRegionLink>> RegionLinkPoolRef;

  private VehicleGroup group;
  private readonly HashSet<VehicleRegion> regions = [];
  
  private MockPathingManager pathing;
  private RegionData regionData;

  static Test_RegionGeneration()
  {
    RegionPoolRef = AccessTools.FieldRefAccess<VehicleRegionMaker, ObjectPool<VehicleRegion>>("regionPool");
    RegionLinkPoolRef =
      AccessTools.FieldRefAccess<VehicleRegionLinkDatabase, ObjectPool<VehicleRegionLink>>("linkPool");
  }

  private VehicleRegionGrid RegionGrid => regionData.regionGridManager[RegionGridType.Normal];

  private VehicleRegionDirtyer RegionDirtyer => regionData.regionDirtyer;

  protected override CellRect TestArea(VehicleDef vehicleDef)
  {
    return VehicleRegion.ChunkAt(root).ContractedBy(vehicleDef.SizePadding);
  }

  [SetUp, ExecutionPriority(Priority.First)]
  public void SetUp()
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
  public void TearDown()
  {
    group.Dispose();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.Destroyed);
    group = null;
  }

  [SetUp]
  public void PreWarmObjectPool()
  {
    // Prewarm object pools, if spans change without invalidating the links, it will
    // not send the link to pool before requesting a new one. It will still utilize
    // the object pool, but the swap will fail the unit test if there are no objects
    // in the object pool before it occurs.
    const int PreWarmRegionCount = 9; // 3x3 grid surrounding root chunk
    const int PreWarmLinkCount = PreWarmRegionCount * 4; // 3x3 grid of 4 links each
    int grids = VehicleRegionGridManager.AllGridTypes.Length;
    RegionPoolRef.Invoke(regionData.regionMaker).PreWarm(PreWarmRegionCount * grids);
    RegionLinkPoolRef.Invoke(regionData.regionMaker.linkDatabase).PreWarm(PreWarmLinkCount * grids);
  }

  [Test]
  [TestDescription("Passability changes dirty regions and trigger region regeneration lazily.")]
  public void RegenerateChunk()
  {
    CellRect testArea = TestArea(group.vehicle.VehicleDef);
    FillArea(testArea);
    ClearArea(testArea);
    Assert.IsTrue(RegionDirtyer.AnyDirty);
    Expect.IsTrue(ValidateArea(RegionGrid, testArea, true));
    Expect.AreEqual(1, RegionsInArea(RegionGrid, TestArea(group.vehicle.VehicleDef)));
    // TODO VF-58: link validation needs fixing, verified this is working in-game but the test fails.
    //Expect.IsTrue(ValidateLinks(regionGrid, chunk), "RegionLinks Generated");
    Expect.IsFalse(RegionGrid.AnyInvalidRegions);
  }

  [Test]
  [TestDescription("Consecutive region updates use object pool instead of creating new regions.")]
  public void RegionPooling()
  {
    using var rc = new ObjectCountWatcher<VehicleRegion>();
    using var rlc = new ObjectCountWatcher<VehicleRegionLink>();

    // Full region regeneration
    CellRect testArea = TestArea(group.vehicle.VehicleDef);
    FillArea(testArea);
    Assert.IsTrue(RegionDirtyer.AnyDirty);
    _ = RegionGrid.GetValidRegionAt(testArea.CenterCell);
    Assert.IsFalse(RegionDirtyer.AnyDirty);
    Assert.AreEqual(0, RegionsInArea(RegionGrid, testArea));
    ClearArea(testArea);
    Assert.IsTrue(RegionDirtyer.AnyDirty);
    _ = RegionGrid.GetValidRegionAt(testArea.CenterCell);
    Assert.IsFalse(RegionDirtyer.AnyDirty);

    // Will always pass for non-debug builds since ObjectCounter will only increment for debug builds.
    // We really shouldn't add the overhead of counting object instantiations outside a dev environment.
    // TODO - Add separate compilation symbol for testing in release builds
    Expect.AreEqual(0, rc.Count, "Regions Instantiated when none were expected");
    Expect.AreEqual(0, rlc.Count, "RegionLinks Instantiated when none were expected");
  }

  [Test]
  [TestDescription("Full chunk filled with impassable entities leaves no invalid regions afterward.")]
  public void AllImpassable()
  {
    CellRect testArea = TestArea(group.vehicle.VehicleDef);
    FillArea(testArea);
    Expect.AreEqual(0, RegionsInArea(RegionGrid, testArea));
    Assert.IsFalse(RegionGrid.AnyInvalidRegions);
  }

  [Test, Disabled]
  public void Padding(/*[Parameters(1, 2, 3, 4)] int width*/)
  {
    CellRect testArea = TestArea(group.vehicle.VehicleDef);
    IntVec3 center = testArea.CenterCell;
    FillCell(center);
    _ = RegionGrid.GetValidRegionAt(center);

    Expect.IsNull(RegionGrid.GetValidRegionAt(center + new IntVec3(-1, 0, 0)));
    Expect.IsNull(RegionGrid.GetValidRegionAt(center + new IntVec3(-1, 0, 1)));
    Expect.IsNull(RegionGrid.GetValidRegionAt(center + new IntVec3(0, 0, 1)));
    Expect.IsNotNull(RegionGrid.GetValidRegionAt(center + new IntVec3(1, 0, 1)));
    Expect.IsNotNull(RegionGrid.GetValidRegionAt(center + new IntVec3(1, 0, 0)));
    Expect.IsNotNull(RegionGrid.GetValidRegionAt(center + new IntVec3(1, 0, -1)));
    Expect.IsNotNull(RegionGrid.GetValidRegionAt(center + new IntVec3(0, 0, -1)));
    Expect.IsNotNull(RegionGrid.GetValidRegionAt(center + new IntVec3(-1, 0, -1)));
  }

  [Test]
  public void LinksToNeighbors()
  {
    CellRect testArea = TestArea(group.vehicle.VehicleDef);
    IntVec3 center = testArea.CenterCell;
    CellRect chunkRect = VehicleRegion.ChunkAt(center);

    VehicleRegion region = RegionGrid.GetValidRegionAt(center);
    Assert.IsNotNull(region);

    VehicleRegion north = RegionGrid.GetValidRegionAt(new IntVec3(center.x, 0, chunkRect.maxZ + 1));
    VehicleRegion south = RegionGrid.GetValidRegionAt(new IntVec3(center.x, 0, chunkRect.minZ - 1));
    VehicleRegion east = RegionGrid.GetValidRegionAt(new IntVec3(chunkRect.maxX + 1, 0, center.z));
    VehicleRegion west = RegionGrid.GetValidRegionAt(new IntVec3(chunkRect.minX - 1, 0, center.z));

    Expect.ReferencesAreNotEqual(region, north);
    Expect.ReferencesAreNotEqual(region, south);
    Expect.ReferencesAreNotEqual(region, east);
    Expect.ReferencesAreNotEqual(region, west);

    var links = region.Links;
    Expect.AreEqual(4, links.items.Count, "Expected 4 region links to cardinal neighbors.");
    Expect.IsTrue(HasSharedLink(region, north), "North link generated.");
    Expect.IsTrue(HasSharedLink(region, south), "South link generated.");
    Expect.IsTrue(HasSharedLink(region, east), "East link generated.");
    Expect.IsTrue(HasSharedLink(region, west), "West link generated.");
    return;

    static bool HasSharedLink(VehicleRegion region, VehicleRegion neighbor)
    {
      using var regionLinks = region.Links;
      VehicleRegionLink regionLink = regionLinks.items.FirstOrDefault(link =>
        link.LinksRegions(region, neighbor));
      if (regionLink is null || !regionLink.IsValid)
        return false;

      using var neighborLinks = neighbor.Links;
      VehicleRegionLink neighborLink = neighborLinks.items.FirstOrDefault(link =>
        link.LinksRegions(region, neighbor));
      return regionLink == neighborLink;
    }
  }

  private void ClearCell(IntVec3 cell)
  {
    pathing.Calculator.SetPassable(cell, true);
    pathing.PathGrid.RecalculatePerceivedPathCostAt(cell);
  }

  private void ClearArea(CellRect cellRect)
  {
    foreach (IntVec3 cell in cellRect)
    {
      ClearCell(cell);
    }
  }

  private void FillCell(IntVec3 cell)
  {
    pathing.Calculator.SetPassable(cell, false);
    pathing.PathGrid.RecalculatePerceivedPathCostAt(cell);
    Assert.IsTrue(RegionDirtyer.AnyDirty);
  }

  private void FillArea(CellRect cellRect)
  {
    ClearArea(cellRect);
    foreach (IntVec3 cell in cellRect)
    {
      FillCell(cell);
    }
    Assert.IsTrue(RegionDirtyer.AnyDirty);
  }

  private int RegionsInArea(VehicleRegionGrid regionGrid, CellRect cellRect)
  {
    foreach (IntVec3 cell in cellRect)
    {
      VehicleRegion validRegion = regionGrid.GetValidRegionAt(cell);
      if (validRegion is not null)
      {
        regions.Add(validRegion);
      }
    }
    int count = regions.Count;
    regions.Clear();
    return count;
  }

  private static bool ValidateArea(VehicleRegionGrid regionGrid, CellRect cellRect, bool expected)
  {
    foreach (IntVec3 cell in cellRect)
    {
      VehicleRegion validRegion = regionGrid.GetValidRegionAt(cell);
      if (validRegion is not null != expected)
        return false;
    }
    return true;
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