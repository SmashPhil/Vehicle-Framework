using DevTools.Testing;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
  TestCategoryNames.VehiclePawn,
  TestCategoryNames.ParallelRenderer,
  TestCategoryNames.Events
)]
[TestDescription("Vehicle rendering with the parallel renderer system.")]
internal sealed class UnitTest_VehiclePawn_Rendering
{
  // The actual texture is not important, it just needs something so it can call Graphic.Init
  // and register in ParallelRenderer
  private const string TexPath = "Things/Item/Chunk/ChunkStone/RockLowA";

  // TODO - GraphicOverlay upgrades

  private VehicleGroup.MockSettings overlaySettings;

  [SetUp]
  private void CacheSettings()
  {
    overlaySettings = new VehicleGroup.MockSettings
    {
      drawProperties = new VehicleDrawProperties
      {
        graphicOverlays =
        [
          new GraphicDataOverlay
          {
            graphicData = new GraphicDataRGB
            {
              texPath = TexPath,
              graphicClass = typeof(Graphic_Single),
              shaderType = ShaderTypeDefOf.Cutout
            }
          }
        ]
      }
    };
  }

  [TearDown]
  private void ClearSettings()
  {
    overlaySettings = null;
  }

  [Test]
  private void BodyRenderer()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 1);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[0], group.vehicle.DrawTracker.renderer);
  }

  [Test]
  private void BodySpawnDeSpawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();
    group.DeSpawn();
    group.Spawn();
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 1);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[0], group.vehicle.DrawTracker.renderer);
  }

  [Test]
  private void TimedExplosion()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 1);
    TimedExplosion exploder = new(group.vehicle,
      new TimedExplosion.Data(IntVec2.Zero, 0, 1, DamageDefOf.EMP, 0));
    group.vehicle.AddTimedExplosion(exploder);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], exploder);
    group.vehicle.DoTick();
    Assert.IsFalse(exploder.Active);
    Expect.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 1);
  }

  [Test]
  private void GraphicOverlay()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(overlaySettings);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.AreEqual(group.vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading.Count, 1);
    GraphicOverlay overlay = group.vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading[0];
    Assert.IsNotNull(overlay);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], overlay);
  }

  [Test]
  private void GraphicOverlaySpawnDeSpawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(overlaySettings);
    group.Spawn();
    group.DeSpawn();
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.AreEqual(group.vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading.Count, 1);
    GraphicOverlay overlay = group.vehicle.DrawTracker.overlayRenderer.AllOverlaysListForReading[0];
    Assert.IsNotNull(overlay);
    Assert.AreEqual(group.vehicle.DrawTracker.ParallelRenderers.Count, 2);
    Expect.ReferencesAreEqual(group.vehicle.DrawTracker.ParallelRenderers[1], overlay);
  }

  [Test, Disabled]
  private void OverlayUpgrade()
  {
  }
}