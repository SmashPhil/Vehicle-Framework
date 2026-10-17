using DevTools.Testing;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.ParallelRenderer, TestCategoryNames.Memory)]
[TestDescription("Material pool lifetime when vehicles are spawned and destroyed.")]
internal sealed class Test_MaterialPool_Lifetime
{
  private const string TexPath = "Things/Item/Chunk/ChunkStone/RockLowA";

  [Test]
  private void VehicleGraphic()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(VehicleMod.settings.main.useCustomShaders);

    VehicleGroup.MockSettings settings = new();
    VehicleDef vehicleDef = CreateVehicleDef(settings);
    vehicleDef.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;

    using MaterialPoolWatcher vehicleMats = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    VehiclePawn vehicle = group.vehicle;
    using VehicleTestCase vtc = new(vehicle, map, TestUtils.GetTestArea(map.Center, vehicleDef.Size));
    group.Spawn();

    // Force graphic to be cached
    _ = vehicle.VehicleGraphic;

    Expect.IsTrue(RGBMaterialPool.TargetCached(vehicle));
    Expect.AreEqual(expected: vehicle.MaterialCount, RGBMaterialPool.GetAll(vehicle)?.Length);
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    Expect.AreEqual(expected: vehicle.MaterialCount, vehicleMats.MaterialsAllocated);

    // Final cleanup of vehicle, any unhandled material allocations should still be
    // handled here. It's imperative that all materials are destroyed once the vehicle
    // is destroyed. There won't be any further attempts so the materials will persist.
    vehicle.Destroy();
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after vehicle was destroyed.");
  }

  [Test]
  private void VehicleTurretGraphics()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(VehicleMod.settings.main.useCustomShaders);

    VehicleTurretDef turretDef = new()
    {
      defName = "MockTurret",
      graphicData = CreateGraphicData(typeof(Graphic_Turret)),
      graphics =
      [
        new VehicleTurretRenderData
        {
          graphicData = CreateGraphicData(typeof(Graphic_Turret))
        }
      ]
    };
    VehicleGroup.MockSettings settings = new()
    {
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          turrets = [new VehicleTurret { def = turretDef, key = "MockTurret" }]
        }
      ]
    };
    VehicleDef vehicleDef = CreateVehicleDef(settings);
    turretDef.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;
    turretDef.graphics[0].graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;

    using MaterialPoolWatcher vehicleMats = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    VehiclePawn vehicle = group.vehicle;
    using VehicleTestCase vtc = new(vehicle, map, TestUtils.GetTestArea(map.Center, vehicleDef.Size));
    group.Spawn();

    CompVehicleTurrets compTurrets = vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    VehicleTurret turret = compTurrets.Turrets[0];

    // NOTE - CompVehicleTurrets initializes all turrets and their graphics PostSpawn, so
    // material allocations will be tracked alongside main body graphic. We can still check
    // allocations before calling VehicleGraphic since they won't be cached until the first
    // VehicleGraphic invocation.
    int cacheTargetsPreForceRegen = vehicleMats.CacheTargets;
    int matsPreForceRegen = vehicleMats.MaterialsAllocated;
    // Turret graphic is created in ctor, we need to force regenerate to log results in
    // MaterialPoolWatcher and track material lifetime.
    turret.ResolveGraphics(vehicle.patternData, forceRegen: true);
    Expect.AreEqual(vehicleMats.CacheTargets, cacheTargetsPreForceRegen);
    Expect.AreEqual(vehicleMats.MaterialsAllocated, matsPreForceRegen);

    Assert.IsNotNull(turret.TurretGraphics);
    Assert.AreEqual(turret.TurretGraphics.Count, 1);
    VehicleTurret.TurretDrawData drawData = turret.TurretGraphics[0];
    Expect.IsTrue(RGBMaterialPool.TargetCached(turret), "Turret graphics was not cached.");
    Expect.IsTrue(RGBMaterialPool.TargetCached(drawData), "TurretDrawData graphic was not cached.");
    Expect.AreEqual(RGBMaterialPool.GetAll(turret)?.Length, turret.MaterialCount);
    Expect.AreEqual(RGBMaterialPool.GetAll(drawData)?.Length, drawData.MaterialCount);
    Expect.AreEqual(vehicleMats.CacheTargets, 2);
    Expect.AreEqual(vehicleMats.MaterialsAllocated, turret.MaterialCount + drawData.MaterialCount);

    vehicle.Destroy();
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after turret was destroyed.");
  }

  [Test]
  private void OverlayRenderer()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(VehicleMod.settings.main.useCustomShaders);

    GraphicDataOverlay overlayData = new()
    {
      graphicData = CreateGraphicData(typeof(Graphic_Vehicle))
    };
    VehicleGroup.MockSettings settings = new()
    {
      drawProperties = new VehicleDrawProperties
      {
        graphicOverlays = [overlayData]
      }
    };
    VehicleDef vehicleDef = CreateVehicleDef(settings);
    overlayData.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;

    using MaterialPoolWatcher vehicleMats = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    VehiclePawn vehicle = group.vehicle;
    using VehicleTestCase vtc = new(vehicle, map, TestUtils.GetTestArea(map.Center, vehicleDef.Size));
    group.Spawn();

    // Overlays
    Assert.AreEqual(vehicle.DrawTracker.overlayRenderer.Overlays.Count, 1);
    GraphicOverlay overlay = vehicle.DrawTracker.overlayRenderer.Overlays[0];
    _ = overlay.Graphic;
    Expect.IsTrue(RGBMaterialPool.TargetCached(overlay), "Overlay graphic was not cached.");
    Expect.AreEqual(RGBMaterialPool.GetAll(overlay)?.Length, overlay.MaterialCount);
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    Expect.AreEqual(vehicleMats.MaterialsAllocated, overlay.MaterialCount);

    vehicle.Destroy();
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after overlay was destroyed.");
  }

  [Test]
  private void UpgradeTreeOverlay()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(VehicleMod.settings.main.useCustomShaders);

    GraphicDataOverlay overlayData = new()
    {
      graphicData = CreateGraphicData(typeof(Graphic_Vehicle))
    };
    UpgradeNode node = new()
    {
      key = "MockUpgradeNode",
      graphicOverlays = [overlayData]
    };
    UpgradeTreeDef treeDef = new()
    {
      defName = "MockUpgradeTree",
      nodes = [node]
    };
    treeDef.ResolveReferences();
    VehicleGroup.MockSettings settings = new()
    {
      comps = [new CompProperties_UpgradeTree { def = treeDef }]
    };
    VehicleDef vehicleDef = CreateVehicleDef(settings);
    overlayData.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;

    using MaterialPoolWatcher vehicleMats = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    VehiclePawn vehicle = group.vehicle;
    using VehicleTestCase vtc = new(vehicle, map, TestUtils.GetTestArea(map.Center, vehicleDef.Size));
    group.Spawn();

    CompUpgradeTree compUpgradeTree = vehicle.CompUpgradeTree;
    Assert.IsNotNull(compUpgradeTree);
    Assert.AreEqual(expected: 0, vehicleMats.CacheTargets);

    compUpgradeTree.FinishUnlock(node);
    Assert.AreEqual(expected: 1, vehicle.DrawTracker.overlayRenderer.ExtraOverlays.Count);
    GraphicOverlay overlay = vehicle.DrawTracker.overlayRenderer.ExtraOverlays[0];
    _ = overlay.Graphic;
    Expect.IsTrue(RGBMaterialPool.TargetCached(overlay));
    Expect.AreEqual(RGBMaterialPool.GetAll(overlay)?.Length, overlay.MaterialCount);
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    Expect.AreEqual(vehicleMats.MaterialsAllocated, overlay.MaterialCount);

    compUpgradeTree.ResetUnlock(node);
    Expect.IsTrue(vehicleMats.AllocationsEqualized);
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after upgrade was removed.");

    // Unlock again so we can test cleanup with vehicle destroy
    compUpgradeTree.FinishUnlock(node);
    Assert.AreEqual(expected: 1, vehicle.DrawTracker.overlayRenderer.ExtraOverlays.Count);
    Expect.IsTrue(RGBMaterialPool.TargetCached(vehicle.DrawTracker.overlayRenderer.ExtraOverlays[0]));
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    vehicle.Destroy();
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after upgrade was destroyed.");
  }

  [Test]
  private void UpgradeTreeTurret()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    Assert.IsTrue(VehicleMod.settings.main.useCustomShaders);

    VehicleTurretDef turretDef = new()
    {
      defName = "MockUpgradeTurret",
      graphicData = CreateGraphicData(typeof(Graphic_Turret))
    };
    UpgradeNode node = new()
    {
      key = "MockUpgradeNode",
      upgrades =
      [
        new TurretUpgrade
        {
          turrets = [new VehicleTurret { def = turretDef, key = "MockUpgradeTurret" }]
        }
      ]
    };
    UpgradeTreeDef treeDef = new()
    {
      defName = "MockUpgradeTree",
      nodes = [node]
    };
    treeDef.ResolveReferences();
    VehicleGroup.MockSettings settings = new()
    {
      comps =
      [
        new CompProperties_VehicleTurrets(),
        new CompProperties_UpgradeTree { def = treeDef }
      ]
    };
    VehicleDef vehicleDef = CreateVehicleDef(settings);
    turretDef.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;

    using MaterialPoolWatcher vehicleMats = new();
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    VehiclePawn vehicle = group.vehicle;
    using VehicleTestCase vtc = new(vehicle, map, TestUtils.GetTestArea(map.Center, vehicleDef.Size));
    group.Spawn();

    CompUpgradeTree compUpgradeTree = vehicle.CompUpgradeTree;
    CompVehicleTurrets compTurrets = vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compUpgradeTree);
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(expected: 0, vehicleMats.CacheTargets);

    compUpgradeTree.FinishUnlock(node);
    Assert.AreEqual(expected: 1, compTurrets.Turrets.Count);
    VehicleTurret turret = compTurrets.Turrets[0];
    Expect.IsTrue(RGBMaterialPool.TargetCached(turret));
    Expect.AreEqual(RGBMaterialPool.GetAll(turret)?.Length, turret.MaterialCount);
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    Expect.AreEqual(vehicleMats.MaterialsAllocated, turret.MaterialCount);

    compUpgradeTree.ResetUnlock(node);
    Expect.IsTrue(vehicleMats.AllocationsEqualized);
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after turret upgrade was removed.");

    // Unlock again so we can test cleanup with vehicle destroy
    compUpgradeTree.FinishUnlock(node);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Expect.IsTrue(RGBMaterialPool.TargetCached(compTurrets.Turrets[0]));
    Expect.AreEqual(expected: 1, vehicleMats.CacheTargets);
    vehicle.Destroy();
    Expect.IsTrue(vehicleMats.AllFree, "Materials left over after turret upgrade was destroyed.");
  }

  private static VehicleDef CreateVehicleDef(VehicleGroup.MockSettings settings)
  {
    VehicleDef vehicleDef = VehicleGroup.CreateVehicleDef(settings);
    settings.vehicleDef = vehicleDef;
    return vehicleDef;
  }

  private static GraphicDataRGB CreateGraphicData(System.Type graphicClass)
  {
    Assert.IsNotNull(ContentFinder<UnityEngine.Texture2D>.Get(TexPath, false));
    return new GraphicDataRGB
    {
      texPath = TexPath,
      graphicClass = graphicClass,
      shaderType = ShaderTypeDefOf.Cutout
    };
  }
}