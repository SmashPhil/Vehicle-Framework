using System.Linq;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.VehiclePawn,
  TestCategoryNames.ParallelRenderer,
  TestCategoryNames.VehicleTurret,
  TestCategoryNames.Events
)]
[TestDescription("Vehicle rendering with the parallel renderer system.")]
internal sealed class Test_VehiclePawn_Rendering_Turrets
{
  // The actual texture is not important, it just needs something so it can call Graphic.Init
  // and register in ParallelRenderer
  private const string TexPath = "Things/Item/Chunk/ChunkStone/RockLowA";

  [Test]
  private void VehicleTurret()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetTurretSettings());
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void TurretUiMaterialMatchesParentColor()
  {
    if (!VehicleMod.settings.main.useCustomShaders)
    {
      Test.Skip("Can't test shader property copying when useCustomShaders is disabled.");
      return;
    }
    VehicleGroup.MockSettings settings = GetTurretSettings();
    VehicleTurretDef turretDef = ((CompProperties_VehicleTurrets)settings.comps[0]).turrets[0].def;
    turretDef.matchParentColor = true;
    turretDef.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;
    turretDef.graphicData.color = Color.blue;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    group.Spawn();
    VehicleTurret turret = group.vehicle.CompVehicleTurrets.Turrets[0];

    group.vehicle.DrawColor = Color.red;
    Material material = GetTurretUiMaterial(turret, group.vehicle);
    Expect.AreEqual(Color.red, material.GetColor(AdditionalShaderPropertyIDs.ColorOne));

    group.vehicle.DrawColor = Color.green;
    material = GetTurretUiMaterial(turret, group.vehicle);
    Expect.AreEqual(Color.green, material.GetColor(AdditionalShaderPropertyIDs.ColorOne));
  }

  [Test]
  private void TurretUiMaterialKeepsOwnColor()
  {
    if (!VehicleMod.settings.main.useCustomShaders)
    {
      Test.Skip("Can't test shader property copying when useCustomShaders is disabled.");
      return;
    }
    VehicleGroup.MockSettings settings = GetTurretSettings();
    VehicleTurretDef turretDef = ((CompProperties_VehicleTurrets)settings.comps[0]).turrets[0].def;
    turretDef.matchParentColor = false;
    turretDef.graphicData.shaderType = VehicleShaderTypeDefOf.CutoutComplexRGB;
    turretDef.graphicData.color = Color.blue;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings);
    group.Spawn();
    VehicleTurret turret = group.vehicle.CompVehicleTurrets.Turrets[0];

    group.vehicle.DrawColor = Color.red;
    Material material = GetTurretUiMaterial(turret, group.vehicle);
    Expect.AreEqual(Color.blue, material.GetColor(AdditionalShaderPropertyIDs.ColorOne));

    group.vehicle.DrawColor = Color.green;
    material = GetTurretUiMaterial(turret, group.vehicle);
    Expect.AreEqual(Color.blue, material.GetColor(AdditionalShaderPropertyIDs.ColorOne));
  }

  [Test]
  private void VehicleTurretSpawnDeSpawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetTurretSettings());
    group.Spawn();
    group.DeSpawn();
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void VehicleTurretChild()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetChildTurretSettings());
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 2);
    Assert.IsNull(compTurrets.Turrets[0].attachedTo);
    Assert.IsNotNull(compTurrets.Turrets[1].attachedTo);
    Assert.IsTrue(ReferenceEquals(compTurrets.Turrets[1].attachedTo, compTurrets.Turrets[0]));
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void VehicleTurretChildSpawnDeSpawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetChildTurretSettings());
    group.Spawn();
    group.DeSpawn();
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 2);
    Assert.IsNull(compTurrets.Turrets[0].attachedTo);
    Assert.IsNotNull(compTurrets.Turrets[1].attachedTo);
    Assert.IsTrue(ReferenceEquals(compTurrets.Turrets[1].attachedTo, compTurrets.Turrets[0]));
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void TurretUpgrade()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetUpgradeSettings());
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    CompUpgradeTree compUpgradeTree = group.vehicle.CompUpgradeTree;
    Assert.IsNotNull(compUpgradeTree);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Assert.AreEqual(compUpgradeTree.Props.def.nodes.Count, 1);
    UpgradeNode node = compUpgradeTree.Props.def.nodes[0];
    Assert.AreEqual(node.upgrades.Count, 1);
    TurretUpgrade upgrade = node.upgrades[0] as TurretUpgrade;
    Assert.IsNotNull(upgrade);
    Assert.AreEqual(upgrade!.turrets.Count, 1);

    compUpgradeTree.FinishUnlock(node);
    Assert.IsTrue(compUpgradeTree.NodeUnlocked(node));
    Assert.AreEqual(compTurrets.Turrets.Count, 2);
    Assert.IsNull(compTurrets.Turrets[1].attachedTo);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 3);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[2], compTurrets.Turrets[1]);

    compUpgradeTree.ResetUnlock(node);
    Assert.IsFalse(compUpgradeTree.NodeUnlocked(node));
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[0], group.vehicle.DrawTracker.renderer);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void ChildTurretUpgrade()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetChildUpgradeSettings());
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    CompUpgradeTree compUpgradeTree = group.vehicle.CompUpgradeTree;
    Assert.IsNotNull(compUpgradeTree);
    Assert.AreEqual(compTurrets.Turrets.Count, 2);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Assert.AreEqual(compUpgradeTree.Props.def.nodes.Count, 1);
    UpgradeNode node = compUpgradeTree.Props.def.nodes[0];
    Assert.AreEqual(node.upgrades.Count, 1);
    TurretUpgrade upgrade = node.upgrades[0] as TurretUpgrade;
    Assert.IsNotNull(upgrade);
    Assert.AreEqual(upgrade!.turrets.Count, 2);

    compUpgradeTree.FinishUnlock(node);
    Assert.IsTrue(compUpgradeTree.NodeUnlocked(node));
    Assert.AreEqual(compTurrets.Turrets.Count, 4);
    Assert.IsNull(compTurrets.Turrets[2].attachedTo);
    Assert.IsNotNull(compTurrets.Turrets[3].attachedTo);
    Assert.IsTrue(ReferenceEquals(compTurrets.Turrets[3].attachedTo, compTurrets.Turrets[2]));
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 3);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[2], compTurrets.Turrets[2]);

    compUpgradeTree.ResetUnlock(node);
    Assert.IsFalse(compUpgradeTree.NodeUnlocked(node));
    Assert.AreEqual(compTurrets.Turrets.Count, 2);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[0], group.vehicle.DrawTracker.renderer);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  private static Material GetTurretUiMaterial(VehicleTurret turret, VehiclePawn vehicle)
  {
    BlitRequest request = BlitRequest.For(vehicle);
    RenderData renderData = ((IBlitTarget)turret)
     .GetRenderData(new Rect(0, 0, 64, 64), request)
     .First();
    Material material = (Material)AccessTools.Field(typeof(RenderData), "material").GetValue(renderData);
    Assert.IsNotNull(material);
    return material;
  }

  private static VehicleGroup.MockSettings GetTurretSettings()
  {
    return new VehicleGroup.MockSettings
    {
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurret",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockTurret"
            }
          ]
        }
      ]
    };
  }

  private static VehicleGroup.MockSettings GetChildTurretSettings()
  {
    return new VehicleGroup.MockSettings
    {
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurret",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockTurret",
            },
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockChildTurret",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockChildTurret",
              parentKey = "MockTurret"
            }
          ]
        }
      ]
    };
  }

  private static VehicleGroup.MockSettings GetUpgradeSettings()
  {
    UpgradeTreeDef treeDef = new()
    {
      defName = "MockUpgradeTree",
      nodes =
      [
        new UpgradeNode
        {
          key = "MockUpgradeNode",
          upgrades =
          [
            new TurretUpgrade
            {
              turrets =
              [
                new VehicleTurret
                {
                  key = "MockTurret",
                  def = new VehicleTurretDef
                  {
                    defName = "MockTurret",
                    graphicData = new GraphicDataRGB
                    {
                      texPath = TexPath,
                      graphicClass = typeof(Graphic_Turret),
                      shaderType = ShaderTypeDefOf.Cutout
                    }
                  }
                }
              ]
            }
          ]
        }
      ]
    };
    treeDef.ResolveReferences();
    return new VehicleGroup.MockSettings
    {
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurretPreexisting",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockTurretPreexisting"
            }
          ]
        },
        new CompProperties_UpgradeTree
        {
          compClass = typeof(CompUpgradeTree),
          def = treeDef
        }
      ]
    };
  }

  private static VehicleGroup.MockSettings GetChildUpgradeSettings()
  {
    UpgradeTreeDef childTreeDef = new()
    {
      defName = "MockUpgradeTree",
      nodes =
      [
        new UpgradeNode
        {
          key = "MockUpgradeNode",
          upgrades =
          [
            new TurretUpgrade
            {
              turrets =
              [
                new VehicleTurret
                {
                  key = "MockTurret",
                  def = new VehicleTurretDef
                  {
                    defName = "MockTurret",
                    graphicData = new GraphicDataRGB
                    {
                      texPath = TexPath,
                      graphicClass = typeof(Graphic_Turret),
                      shaderType = ShaderTypeDefOf.Cutout
                    }
                  }
                },
                new VehicleTurret
                {
                  key = "MockChildTurret",
                  parentKey = "MockTurret",
                  def = new VehicleTurretDef
                  {
                    defName = "MockChildTurret",
                    graphicData = new GraphicDataRGB
                    {
                      texPath = TexPath,
                      graphicClass = typeof(Graphic_Turret),
                      shaderType = ShaderTypeDefOf.Cutout
                    }
                  }
                }
              ]
            }
          ]
        }
      ]
    };
    childTreeDef.ResolveReferences();
    return new VehicleGroup.MockSettings
    {
      comps =
      [
        new CompProperties_VehicleTurrets
        {
          compClass = typeof(CompVehicleTurrets),
          turrets =
          [
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockTurretPreexisting",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockTurretPreexisting",
            },
            new VehicleTurret
            {
              def = new VehicleTurretDef
              {
                defName = "MockChildTurretPreexisting",
                graphicData = new GraphicDataRGB
                {
                  texPath = TexPath,
                  graphicClass = typeof(Graphic_Turret),
                  shaderType = ShaderTypeDefOf.Cutout
                }
              },
              key = "MockChildTurretPreexisting",
              parentKey = "MockTurretPreexisting"
            }
          ]
        },
        new CompProperties_UpgradeTree
        {
          compClass = typeof(CompUpgradeTree),
          def = childTreeDef
        }
      ]
    };
  }
}
