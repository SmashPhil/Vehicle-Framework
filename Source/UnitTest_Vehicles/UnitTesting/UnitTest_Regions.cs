using System.Collections.Generic;
using DevTools.Testing;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_Regions : UnitTest_MapTest
{
	private const int MaxTestPadding = 4;

	private readonly HashSet<VehicleRegion> regions = [];

	protected override bool ShouldTest(VehicleDef vehicleDef)
	{
		// SizePadding will rarely be above 4, but there are mods out there adding incredibly large
		// vehicles, and region testing would be too expensive. Validating 4 and below should suffice.
		return vehicleDef.SizePadding <= MaxTestPadding && PathingHelper.ShouldCreateRegions(vehicleDef);
	}

	protected override CellRect TestArea(VehicleDef vehicleDef)
	{
		return VehicleRegion.ChunkAt(root).ContractedBy(vehicleDef.SizePadding);
	}

	[Test]
	private void Generation()
	{
		using SetTerrainOnDispose stod = new(Find.CurrentMap, TerrainDefOf.PackedDirt, VehicleRegion.ChunkAt(root));
		foreach (VehiclePawn vehicle in vehicles)
		{
			using VehicleTestCase vtc = new(vehicle, map, TestArea(vehicle.VehicleDef));

			VehicleDef vehicleDef = vehicle.VehicleDef;
			int padding = vehicleDef.SizePadding;

			CellRect testArea = TestArea(vehicleDef);
			IntVec3 center = testArea.CenterCell;
			//CellRect chunk = VehicleRegion.ChunkAt(root);
			TerrainDef terrainDef = DefDatabase<TerrainDef>.AllDefsListForReading
			 .FirstOrDefault(def => VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _));
			ThingDef testDef = ThingDefOf.Wall;
			if (!PathingHelper.IsRegionEffector(vehicleDef, testDef))
			{
				testDef = DefDatabase<ThingDef>.AllDefsListForReading.RandomOrDefault(def =>
					def.building != null &&
					def.Size == IntVec2.One && PathingHelper.IsRegionEffector(vehicleDef, def) &&
					def is not VehicleBuildDef &&
					PathingHelper.regionEffectors[def].Contains(vehicleDef));
			}
			Assert.IsTrue(VehiclePathGrid.ThingCostOf(vehicleDef, testDef) >=
				VehiclePathGrid.ImpassableCost);
			Assert.IsNotNull(testDef);

			VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
			VehiclePathingSystem.VehiclePathData pathData = mapping[vehicleDef];
			VehicleRegionGrid regionGrid = pathData.VehicleRegionGrid;
			VehicleRegionMaker regionMaker = pathData.VehicleRegionMaker;
			VehicleRegionDirtyer regionDirtyer = pathData.VehicleRegionDirtyer;
			Assert.IsFalse(mapping.ThreadAvailable);

			// Clear area region generation. The chunk should be completely empty, meaning
			// 1 region spanning the entirety of the chunk and there should be no neighboring
			// entities that might pad into the chunk we're testing.

			// NOTE - some even size vehicles will 'clip' corner regions, padding by 2 will remove that
			// edge case from needing to be accounted for.  We're only testing region dirtying / updating
			// here, edge cases should be tested for separately.
			DebugHelper.DestroyArea(testArea.ExpandedBy(padding + 2), map, replaceTerrain: terrainDef);
			Assert.AreEqual(RegionsInArea(regionGrid, testArea), 1);

			// Prewarm object pools, if spans change without invalidating the links, it will
			// not send the link to pool before requesting a new one. It will still utilize
			// the object pool, but the swap will fail the unit test if there are no objects
			// in the object pool before it occurs.
			const int PreWarmRegionCount = 9; // 3x3 grid surrounding root chunk
			const int PreWarmLinkCount = PreWarmRegionCount * 4; // 3x3 grid of 4 links each
			regionMaker.regionPool.PreWarm(PreWarmRegionCount);
			regionMaker.linkPool.PreWarm(PreWarmLinkCount);

			// Verify region is sent to pool and later retrieved when area is cleared
			// If Count is not 0, new objects were instantiated within this scope.
			using ObjectCountWatcher<VehicleRegion> ocwRegions = new();
			using ObjectCountWatcher<VehicleRegionLink> ocwLinks = new();

			// Full chunk filled with impassable entities leaves no invalid regions afterward
			FillArea(testArea, terrainDef);
			Expect.AreEqual(RegionsInArea(regionGrid, testArea), 0, "Set Impassable");
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");
			Expect.IsTrue(mapping[vehicleDef].VehiclePathGrid.Enabled, "PathGrid Enabled");

			// Clear
			ClearArea(terrainDef);
			Assert.IsTrue(regionDirtyer.AnyDirty);
			Expect.IsTrue(ValidateArea(regionGrid, testArea, true), "Clear Impassable");
			Expect.AreEqual(RegionsInArea(regionGrid, testArea), 1, "Unified Region");
			// TODO VF-58: link validation needs fixing, verified this is working in-game but the test fails.
			//Expect.IsTrue(ValidateLinks(regionGrid, chunk), "RegionLinks Generated");
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");

			// 1 Block
			ClearArea(terrainDef);
			VehicleRegion region = regionGrid.GetValidRegionAt(center);
			Assert.IsNotNull(region);
			CellRect singleCell = CellRect.SingleCell(center);
			FillArea(singleCell, terrainDef);
			if (vehicleDef.SizePadding == 0)
			{
				// If there's no padding, then test valid edge cells instead
				Expect.IsTrue(ValidateArea(regionGrid, singleCell, false), "1 Cell Removed From Region");
				if (vehicleDef.size.x % 2 == 0)
				{
					// Even-width vehicles will have 0 padding but impassable on top left corners (West, NorthWest, and North)
					// since the position of the vehicle will be 1 cell up from the impassable corner.
					Expect.IsFalse(ValidRegionAt(singleCell.CenterCell + new IntVec3(-1, 0, 0)));
					Expect.IsFalse(ValidRegionAt(singleCell.CenterCell + new IntVec3(-1, 0, 1)));
					Expect.IsFalse(ValidRegionAt(singleCell.CenterCell + new IntVec3(0, 0, 1)));
					Expect.IsTrue(ValidRegionAt(singleCell.CenterCell + new IntVec3(1, 0, 1)));
					Expect.IsTrue(ValidRegionAt(singleCell.CenterCell + new IntVec3(1, 0, 0)));
					Expect.IsTrue(ValidRegionAt(singleCell.CenterCell + new IntVec3(1, 0, -1)));
					Expect.IsTrue(ValidRegionAt(singleCell.CenterCell + new IntVec3(0, 0, -1)));
					Expect.IsTrue(ValidRegionAt(singleCell.CenterCell + new IntVec3(-1, 0, -1)));
				}
				else
				{
					Expect.All(singleCell.ExpandedBy(1).EdgeCells, ValidRegionAt, "No Padding Applied");
				}
			}
			else
			{
				CellRect paddedArea = CellRect.CenteredOn(center, vehicleDef.SizePadding);
				Expect.IsTrue(ValidateArea(regionGrid, paddedArea, false), "Padding Applied");
			}
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");
			Expect.IsFalse(regionDirtyer.AnyDirty);

			// Region Reused
			ClearArea(terrainDef);
			Expect.IsTrue(regionDirtyer.AnyDirty);
			Expect.ReferencesAreEqual(region, regionGrid.GetValidRegionAt(center), "Region Recycled");
			Expect.IsFalse(regionGrid.AnyInvalidRegions, "No Invalid Regions");

			// Will always pass for non-debug builds since ObjectCounter will only increment for debug builds.
			// We really shouldn't add the overhead of counting object instantiations outside of a dev environment.
			Expect.AreEqual(ocwRegions.Count, 0, "No Regions Instantiated");
			Expect.AreEqual(ocwLinks.Count, 0, "No RegionLinks Instantiated");
			continue;

			void ClearArea(TerrainDef terrain)
			{
				DebugHelper.DestroyArea(testArea, map, replaceTerrain: terrain);
			}

			void FillArea(CellRect cellRect, TerrainDef terrain)
			{
				ThingDef stuffDef = testDef.MadeFromStuff ? GenStuff.DefaultStuffFor(testDef) : null;
				ClearArea(terrain);
				foreach (IntVec3 cell in cellRect)
				{
					GenSpawn.Spawn(ThingMaker.MakeThing(testDef, stuffDef), cell, map);
				}
				Assert.IsTrue(regionDirtyer.AnyDirty);
			}

			bool ValidRegionAt(IntVec3 cell)
			{
				return regionGrid.GetValidRegionAt(cell) != null;
			}
		}
	}

	private int RegionsInArea(VehicleRegionGrid regionGrid, CellRect cellRect)
	{
		foreach (IntVec3 cell in cellRect)
		{
			VehicleRegion validRegion = regionGrid.GetValidRegionAt(cell);
			if (validRegion is not null)
				regions.Add(validRegion);
		}
		int count = regions.Count;
		regions.Clear();
		return count;
	}

	private static bool ValidateLinks(VehicleRegionGrid regionGrid, CellRect cellRect)
	{
		foreach (IntVec3 cell in cellRect.EdgeCells)
		{
			VehicleRegion validRegion = regionGrid.GetValidRegionAt(cell);
			if (validRegion is null)
				continue;

			// i = 0 would start at center, we want 4 cardinal neighbors
			for (int i = 1; i <= 4; i++)
			{
				IntVec3 cardinal = cell + GenRadial.ManualRadialPattern[i];
				VehicleRegion neighbor = regionGrid.GetValidRegionAt(cardinal);
				if (neighbor is null || neighbor == validRegion)
					continue;

				VehicleRegionLink regionLink = validRegion.Links.items.FirstOrDefault(link =>
					link.LinksRegions(validRegion, neighbor));
				VehicleRegionLink neighborLink = neighbor.Links.items.FirstOrDefault(link =>
					link.LinksRegions(validRegion, neighbor));

				if (regionLink is null || neighborLink is null)
					return false;
				if (regionLink != neighborLink)
					return false;
			}
		}
		return true;
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
}