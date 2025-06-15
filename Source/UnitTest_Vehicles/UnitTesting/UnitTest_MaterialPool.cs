using DevTools.UnitTesting;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
internal sealed class UnitTest_MaterialPool : UnitTest_MapTest
{
  [Test]
  private void LifetimeManagement()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);
      // VehicleGraphic
      using MaterialPoolWatcher vehicleMats = new();

      GenSpawn.Spawn(vehicle, root, map);
      Assert.IsTrue(vehicle.Spawned);

      int targets = 0;
      int materialCount = 0;

      // Turrets
      // NOTE - CompVehicleTurrets initializes all turrets and their graphics PostSpawn, so
      // material allocations will be tracked alongside main body graphic. We can still check
      // allocations before calling VehicleGraphic since they won't be cached until the first
      // VehicleGraphic invocation.
      if (vehicle.CompVehicleTurrets != null && !vehicle.CompVehicleTurrets.Turrets.NullOrEmpty())
      {
        int cacheTargetsPreForceRegen = vehicleMats.CacheTargets;
        foreach (VehicleTurret turret in vehicle.CompVehicleTurrets.Turrets)
        {
          // Turret graphic is created in ctor, we need to force regenerate to
          // log results in MaterialPoolWatcher and track material lifetime.
          int matsPreForceRegen = vehicleMats.MaterialsAllocated;
          turret.ResolveGraphics(vehicle.patternData, forceRegen: true);
          Expect.AreEqual(matsPreForceRegen, vehicleMats.MaterialsAllocated);
          if (!turret.NoGraphic &&
            turret.def.graphicData.shaderType.Shader.SupportsRGBMaskTex())
          {
            targets++;
            materialCount += turret.MaterialCount;
          }

          if (!turret.TurretGraphics.NullOrEmpty())
          {
            foreach (VehicleTurret.TurretDrawData drawData in turret.TurretGraphics)
            {
              if (!drawData.graphicData.shaderType.Shader.SupportsRGBMaskTex())
                continue;

              targets++;
              materialCount += turret.MaterialCount;
            }
          }
          Expect.AreEqual(cacheTargetsPreForceRegen, vehicleMats.CacheTargets,
            "Precached Turret CacheTarget");
          Expect.AreEqual(matsPreForceRegen, vehicleMats.MaterialsAllocated,
            "Precached Turret Materials");
        }
        Expect.AreEqual(vehicleMats.CacheTargets, targets, "Add Turret CacheTarget");
        Expect.AreEqual(vehicleMats.MaterialsAllocated, materialCount, "Materials Allocated");
      }

      if (vehicle.VehicleDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
      {
        targets++; // Only 1 vehicle instance
        materialCount += vehicle.MaterialCount;
      }

      _ = vehicle.VehicleGraphic; // Force graphic to be cached before any upgrade calls
      Expect.AreEqual(vehicleMats.CacheTargets, targets, "Add CacheTarget");
      Expect.AreEqual(vehicleMats.MaterialsAllocated, materialCount, "Materials Allocated");

      // Overlays
      if (vehicle.DrawTracker.overlayRenderer.Overlays.Count > 0)
      {
        using MaterialPoolWatcher overlayMats = new();
        int overlayMaterialCount = 0;
        int overlayTargets = 0;
        foreach (GraphicOverlay overlay in vehicle.DrawTracker.overlayRenderer.Overlays)
        {
          if (overlay.data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
          {
            overlayTargets++;
            overlayMaterialCount += overlay.MaterialCount;
          }

          _ = overlay.Graphic;
        }

        Expect.AreEqual(overlayMats.CacheTargets, overlayTargets, "Add Overlay CacheTarget");
        Expect.AreEqual(overlayMats.MaterialsAllocated, overlayMaterialCount,
          "Materials Allocated");
      }

      // UpgradeTree
      if (vehicle.CompUpgradeTree != null)
      {
        // Upgrades can add overlays and turrets from any upgrade type including types
        // from other mods, so tracking expected material allocations will depend on the
        // vehicles being tested with. Instead, we track target and material count before
        // and after the upgrade to ensure they're all freed when upgrades are reset.
        foreach (UpgradeNode node in vehicle.CompUpgradeTree.Props.def.nodes)
        {
          using MaterialPoolWatcher upgradeMats = new();
          vehicle.CompUpgradeTree.FinishUnlock(node);
          vehicle.CompUpgradeTree.ResetUnlock(node);
          Expect.IsTrue(upgradeMats.AllocationsEqualized, "Upgrade Materials Destroyed");
        }

        // Unlock again so we can test cleanup with vehicle destroy
        foreach (UpgradeNode node in vehicle.CompUpgradeTree.Props.def.nodes)
        {
          vehicle.CompUpgradeTree.FinishUnlock(node);
        }
      }

      // Final cleanup of vehicle, any unhandled material allocations should still be
      // handled here. It's imperative that all materials are destroyed once the vehicle
      // is destroyed. There won't be any further attempts so the materials will persist.
      vehicle.Destroy();
      Expect.IsTrue(vehicleMats.AllFree, "All Materials Destroyed");
    }
  }
}