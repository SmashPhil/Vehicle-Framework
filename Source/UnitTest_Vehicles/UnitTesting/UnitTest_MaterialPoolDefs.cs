using System.Collections.Generic;
using DevTools.UnitTesting;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal sealed class UnitTest_MaterialPoolDefs : UnitTest_VehicleDefTest
{
  private const string HotReloadSuffix = "_HotReloadedThrowaway";

  [Test]
  private void VehicleDefs()
  {
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      // Skip tests for hot reload's transient defs. They will have entries in cache for vehicle defs
      // but not for turrets and overlays. This is a side effect of Ludeon's implementation for hot
      // reloading and their lack of event handles for suppressing material caching in VehicleDef.
      if (vehicleDef.defName.EndsWith(HotReloadSuffix))
        continue;

      using Test.Group group = new(vehicleDef.defName);

      if (vehicleDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
      {
        Expect.IsTrue(RGBMaterialPool.TargetCached(vehicleDef), "Target Cached");
        Expect.AreEqual(RGBMaterialPool.GetAll(vehicleDef)?.Length, vehicleDef.MaterialCount,
          "Materials Allocated");
      }

      // Overlays
      if (vehicleDef.drawProperties != null && !vehicleDef.drawProperties.overlays.NullOrEmpty())
      {
        foreach (GraphicOverlay overlay in vehicleDef.drawProperties.overlays)
        {
          if (!overlay.data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
            continue;

          Expect.IsTrue(RGBMaterialPool.TargetCached(overlay),
            $"Overlay[{overlay.data.graphicData.texPath}] Target Cached");
          Expect.AreEqual(RGBMaterialPool.GetAll(overlay)?.Length, overlay.MaterialCount,
            $"Overlay[{overlay.data.graphicData.texPath}] Materials Allocated");
        }
      }

      // Turrets
      CompProperties_VehicleTurrets compTurrets =
        vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>();
      if (compTurrets is not null && !compTurrets.turrets.NullOrEmpty())
      {
        foreach (VehicleTurret turret in compTurrets.turrets)
        {
          if (!turret.NoGraphic &&
            turret.def.graphicData.shaderType.Shader.SupportsRGBMaskTex())
          {
            Expect.IsTrue(RGBMaterialPool.TargetCached(turret),
              $"{turret.key ?? turret.def.defName} Target Cached");
            Expect.AreEqual(RGBMaterialPool.GetAll(turret)?.Length, turret.MaterialCount,
              $"{turret.key ?? turret.def.defName} Materials Allocated");
          }

          if (!turret.TurretGraphics.NullOrEmpty())
          {
            foreach (VehicleTurret.TurretDrawData drawData in turret.TurretGraphics)
            {
              if (drawData.graphicData.shaderType.Shader.SupportsRGBMaskTex())
              {
                Expect.IsTrue(RGBMaterialPool.TargetCached(drawData),
                  $"{turret.key ?? turret.def.defName} DrawData Target Cached");
                Expect.AreEqual(RGBMaterialPool.GetAll(drawData)?.Length, drawData.MaterialCount,
                  $"{turret.key ?? turret.def.defName} Materials Allocated");
              }
            }
          }
        }
      }

      // Upgrades
      CompProperties_UpgradeTree compUpgrade =
        vehicleDef.GetCompProperties<CompProperties_UpgradeTree>();
      if (compUpgrade?.def != null && !compUpgrade.def.nodes.NullOrEmpty())
      {
        foreach (UpgradeNode node in compUpgrade.def.nodes)
        {
          List<GraphicOverlay> overlays = compUpgrade.TryGetOverlays(node);
          if (!overlays.NullOrEmpty())
          {
            foreach (GraphicOverlay overlay in overlays)
            {
              if (!overlay.data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
                continue;

              Expect.IsTrue(RGBMaterialPool.TargetCached(overlay), $"{node.key} Target Cached");
              Expect.AreEqual(RGBMaterialPool.GetAll(overlay)?.Length, overlay.MaterialCount,
                $"{node.key} Materials Allocated");
            }
          }
        }
      }
    }
  }

  [Test]
  private void PatternDefs()
  {
    foreach (PatternDef patternDef in DefDatabase<PatternDef>.AllDefsListForReading)
    {
      using Test.Group group = new(patternDef.defName);
      Expect.IsTrue(RGBMaterialPool.TargetCached(patternDef), "Target Cached");
      Expect.AreEqual(RGBMaterialPool.GetAll(patternDef)?.Length, patternDef.MaterialCount,
        "Materials Allocated");
    }
  }

  [Test]
  private void Total()
  {
    int count = 0;
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      // Base Vehicle
      if (vehicleDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
        count += vehicleDef.MaterialCount;

      // Overlays
      if (vehicleDef.drawProperties != null && !vehicleDef.drawProperties.overlays.NullOrEmpty())
      {
        foreach (GraphicOverlay overlay in vehicleDef.drawProperties.overlays)
        {
          if (overlay.data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
            count += overlay.MaterialCount;
        }
      }

      // Turrets
      CompProperties_VehicleTurrets compTurrets =
        vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>();
      if (compTurrets is not null && !compTurrets.turrets.NullOrEmpty())
      {
        foreach (VehicleTurret turret in compTurrets.turrets)
        {
          if (!turret.NoGraphic &&
            turret.def.graphicData.shaderType.Shader.SupportsRGBMaskTex())
          {
            count += turret.MaterialCount;
          }
          if (!turret.TurretGraphics.NullOrEmpty())
          {
            foreach (VehicleTurret.TurretDrawData drawData in turret.TurretGraphics)
            {
              if (drawData.graphicData.shaderType.Shader.SupportsRGBMaskTex())
                count += drawData.MaterialCount;
            }
          }
        }
      }

      CompProperties_UpgradeTree compUpgrade =
        vehicleDef.GetCompProperties<CompProperties_UpgradeTree>();
      if (compUpgrade?.def != null && !compUpgrade.def.nodes.NullOrEmpty())
      {
        foreach (UpgradeNode node in compUpgrade.def.nodes)
        {
          List<GraphicOverlay> overlays = compUpgrade.TryGetOverlays(node);
          if (overlays.NullOrEmpty())
            continue;

          foreach (GraphicOverlay overlay in overlays)
          {
            if (overlay.data.graphicData.shaderType.Shader.SupportsRGBMaskTex())
              count += overlay.MaterialCount;
          }
        }
      }
    }

    foreach (PatternDef patternDef in DefDatabase<PatternDef>.AllDefsListForReading)
    {
      count += patternDef.MaterialCount;
    }

    Expect.AreEqual(count, RGBMaterialPool.TotalMaterials, "MaterialPool Total Count");
  }
}