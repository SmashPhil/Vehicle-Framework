using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_TerrainGrid : UnitTest_MapTest
{
	protected override CellRect TestArea(VehicleDef vehicleDef)
	{
		return CellRect.CenteredOn(root, 5);
	}

	[Test]
	private void TerrainGrid()
	{
		foreach (VehiclePawn vehicle in vehicles)
		{
			using VehicleTestCase vtc = new(vehicle, map, TestArea(vehicle.VehicleDef));

			VehicleDef vehicleDef = vehicle.VehicleDef;
			VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
			VehiclePathingSystem.VehiclePathData pathData = mapping[vehicleDef];

			CellRect testArea = TestArea(vehicleDef);
			CellRect terrainArea = testArea.ContractedBy(vehicleDef.SizePadding);
			DebugHelper.DestroyArea(testArea.ExpandedBy(vehicleDef.SizePadding), map);

			TerrainDef terrainOrig = map.terrainGrid.TerrainAt(root);
			TerrainDef passableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
			 .FirstOrDefault(def =>
					def != terrainOrig && VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));
			TerrainDef impassableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
			 .FirstOrDefault(def =>
					def != terrainOrig && !VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));

			Assert.IsNotNull(terrainOrig);
			Assert.IsNotNull(passableTerrain);
			Assert.IsNotNull(impassableTerrain);

			// VehiclePathGrid costs should take terrain into account
			VehiclePathGrid pathGrid = pathData.VehiclePathGrid;

			// Terrain cost updates
			DebugHelper.DestroyArea(terrainArea, map, replaceTerrain: passableTerrain);
			Expect.IsTrue(AreaCost(vehicleDef, pathGrid, in terrainArea, passableTerrain),
				"PathGrid Updated");

			// Terrain becomes impassable
			DebugHelper.DestroyArea(terrainArea, map, replaceTerrain: impassableTerrain);
			Expect.IsTrue(AreaCost(vehicleDef, pathGrid, in terrainArea, impassableTerrain),
				"PathGrid Updated");
			Expect.IsFalse(VehiclePathGrid.PassableTerrainCost(vehicleDef, impassableTerrain, out _),
				"PathGrid Impassable");
		}
	}

	[Test]
	private void DirtyRegions()
	{
		using SetTerrainOnDispose stod = new(Find.CurrentMap, TerrainDefOf.PackedDirt, VehicleRegion.ChunkAt(root));
		foreach (VehiclePawn vehicle in vehicles)
		{
			VehicleDef vehicleDef = vehicle.VehicleDef;
			VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
			VehicleRegionGrid regionGrid = mapping[vehicleDef].VehicleRegionGrid;

			if (!PathingHelper.ShouldCreateRegions(vehicleDef) || !mapping.GridOwners.IsOwner(vehicleDef))
				continue;

			using VehicleTestCase vtc = new(vehicle, map, TestArea(vehicle.VehicleDef));
			TerrainDef passableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
			 .FirstOrDefault(def => VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));
			TerrainDef impassableTerrain = DefDatabase<TerrainDef>.AllDefsListForReading
			 .FirstOrDefault(def => !VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));

			CellRect testArea = TestArea(vehicleDef);
			CellRect terrainArea = testArea.ContractedBy(vehicleDef.SizePadding + 2);

			// Impassable terrain invalidates regions
			DebugHelper.DestroyArea(testArea, map, replaceTerrain: impassableTerrain);
			Expect.IsTrue(Regions(regionGrid, in terrainArea, false), "RegionGrid Updated");
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");

			// Impassable terrain removal invalidates regions
			DebugHelper.DestroyArea(testArea, map, replaceTerrain: passableTerrain);
			Expect.IsTrue(Regions(regionGrid, in terrainArea, true), "RegionGrid Updated");
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");
		}
	}

	private static bool AreaCost(VehicleDef vehicleDef, VehiclePathGrid pathGrid,
		ref readonly CellRect cellRect, TerrainDef terrainDef)
	{
		int expected = VehiclePathGrid.TerrainCostAt(vehicleDef, terrainDef);
		foreach (IntVec3 cell in cellRect)
		{
			if (pathGrid.CalculatedCostAt(cell) != expected)
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