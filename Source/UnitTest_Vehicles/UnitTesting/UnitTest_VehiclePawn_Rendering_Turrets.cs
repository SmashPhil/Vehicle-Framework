using DevTools.UnitTesting;
using RimWorld;
using UnityEngine.Assertions;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
  TestCategoryNames.VehiclePawn,
  TestCategoryNames.ParallelRenderer,
  TestCategoryNames.VehicleTurret,
  TestCategoryNames.Events
)]
[TestDescription("Vehicle rendering with the parallel renderer system.")]
internal sealed class UnitTest_VehiclePawn_Rendering_Turrets
{
  // The actual texture is not important, it just needs something so it can call Graphic.Init
  // and register in ParallelRenderer
  private const string TexPath = "Things/Item/Chunk/ChunkStone/RockLowA";

  private VehicleGroup.MockSettings turretSettings;
  private VehicleGroup.MockSettings childTurretSettings;
  private VehicleGroup.MockSettings turretUpgradeSettings;
  private VehicleGroup.MockSettings childTurretUpgradeSettings;

  [SetUp]
  private void CacheSettings()
  {
    turretSettings = new VehicleGroup.MockSettings
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
    childTurretSettings = new VehicleGroup.MockSettings
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
    turretUpgradeSettings = new VehicleGroup.MockSettings
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
    childTurretUpgradeSettings = new VehicleGroup.MockSettings
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

  [TearDown]
  private void ClearSettings()
  {
    turretSettings = null;
    childTurretSettings = null;
    turretUpgradeSettings = null;
    childTurretUpgradeSettings = null;
  }

  [Test]
  private void VehicleTurret()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(turretSettings);
    group.Spawn();
    CompVehicleTurrets compTurrets = group.vehicle.CompVehicleTurrets;
    Assert.IsNotNull(compTurrets);
    Assert.AreEqual(compTurrets.Turrets.Count, 1);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], compTurrets.Turrets[0]);
  }

  [Test]
  private void VehicleTurretSpawnDeSpawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(turretSettings);
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
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(childTurretSettings);
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
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(childTurretSettings);
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
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(turretUpgradeSettings);
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
    Assert.AreEqual(upgrade.turrets.Count, 1);

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
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(childTurretUpgradeSettings);
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
    Assert.AreEqual(upgrade.turrets.Count, 2);

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
}