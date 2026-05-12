using DevTools.Testing;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_TerrainGrid : Test_MapTest
{
  private VehicleGroup group;

  private TerrainDef terrainOrig;
  private TerrainDef passableTerrain;
  private TerrainDef impassableTerrain;

  [SetUp]
  public void SetUp()
  {
    // Tests running through the region generation need xml-loaded defs. Mock defs won't have path data
    // cached in VehiclePathingSystem.
    VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading
      .FirstOrDefault(PathingHelper.ShouldCreateRegions);
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1
    });
    TestUtils.PrepareArea(map, TestArea(vehicleDef), vehicleDef);

    terrainOrig = map.terrainGrid.TerrainAt(root);
    passableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
      .FirstOrDefault(def =>
        def != terrainOrig && VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));
    impassableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
      .FirstOrDefault(def =>
        def != terrainOrig && !VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));

    Assert.IsNotNull(terrainOrig);
    Assert.IsNotNull(passableTerrain);
    Assert.IsNotNull(impassableTerrain);
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
  private void TerrainPathCost()
  {
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
    PathData pathData = mapping[vehicleDef];
    CellRect terrainArea = TestArea(vehicleDef).ContractedBy(vehicleDef.SizePadding);
    VehiclePathGrid pathGrid = pathData.VehiclePathGrid;

    DebugHelper.DestroyArea(terrainArea, map, replaceTerrain: passableTerrain);
    Expect.IsTrue(AreaCost(vehicleDef, pathGrid, in terrainArea, passableTerrain));
  }

  [Test]
  private void TerrainPassability()
  {
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
    PathData pathData = mapping[vehicleDef];
    CellRect terrainArea = TestArea(vehicleDef).ContractedBy(vehicleDef.SizePadding);
    VehiclePathGrid pathGrid = pathData.VehiclePathGrid;

    DebugHelper.DestroyArea(terrainArea, map, replaceTerrain: impassableTerrain);
    Expect.IsTrue(AreaCost(vehicleDef, pathGrid, in terrainArea, impassableTerrain),
      "PathGrid Updated");
    Expect.IsFalse(VehiclePathGrid.PassableTerrainCost(vehicleDef, impassableTerrain, out _),
      "PathGrid Impassable");
  }

  [Test]
  private void DirtyRegions()
  {
    using SetTerrainOnDispose stod = new(Find.CurrentMap, TerrainDefOf.PackedDirt, VehicleRegion.ChunkAt(root));
    VehicleDef vehicleDef = group.vehicle.VehicleDef;
    Assert.IsTrue(PathingHelper.ShouldCreateRegions(vehicleDef));
    VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
    CellRect testArea = TestArea(vehicleDef);
    CellRect terrainArea = testArea.ContractedBy(vehicleDef.SizePadding);
    VehicleRegionGrid regionGrid = mapping[vehicleDef].VehicleRegionGridManager[RegionGridType.Normal];

    DebugHelper.DestroyArea(testArea, map, replaceTerrain: impassableTerrain);
    Expect.IsTrue(Regions(regionGrid, in terrainArea, valid: false));
    Expect.IsFalse(regionGrid.AnyInvalidRegions);

    DebugHelper.DestroyArea(testArea, map, replaceTerrain: passableTerrain);
    Expect.IsTrue(Regions(regionGrid, in terrainArea, valid: true));
    Expect.IsFalse(regionGrid.AnyInvalidRegions);
  }

	private static bool AreaCost(VehicleDef vehicleDef, VehiclePathGrid pathGrid,
		ref readonly CellRect cellRect, TerrainDef terrainDef)
	{
		int weatherDusting = vehicleDef.properties.customWeatherCosts.TryGetValue(WeatherBuildupCategory.None);
		int expected = VehiclePathGrid.TerrainCostAt(vehicleDef, terrainDef);
		foreach (IntVec3 cell in cellRect)
		{
			// TODO - Fix when refactoring with mock vehicles. Lazy workaround since
			// these tests should be using mock vehicles anyway.
			int actualCost = pathGrid.CalculatedCostAt(cell);
			if (actualCost != VehiclePathGrid.ImpassableCost)
				actualCost -= weatherDusting;

			if (actualCost != expected)
				return false;
		}
		return true;
	}

	private static bool Regions(VehicleRegionGrid regionGrid, ref readonly CellRect cellRect,
		bool valid)
	{
		foreach (IntVec3 cell in cellRect)
		{
			VehicleRegion region = regionGrid.GetValidRegionAt(cell);
			if ((region is not null) != valid)
				return false;
		}
		return true;
	}
}