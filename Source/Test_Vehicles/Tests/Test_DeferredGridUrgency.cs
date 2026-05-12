using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using TestType = DevTools.Testing.TestType;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
internal sealed class Test_DeferredGridUrgency
{
  private VehiclePathingSystem mapping;

  private VehicleGroup group;
  private PathData pathData;

  [OneTimeSetUp]
  private void SetUpMap()
  {
    Assert.IsNotNull(Find.CurrentMap);
    mapping = Find.CurrentMap.GetCachedMapComponent<VehiclePathingSystem>();
    Assert.IsNotNull(mapping.deferredGridGeneration);
  }

  [OneTimeTearDown]
  private void CleanUpMap()
  {
    mapping.deferredGridGeneration.DoPassExpectClear();
    mapping.RegenerateGrids(deferment: VehiclePathingSystem.GridDeferment.Forced);
    mapping = null;
  }

  [SetUp]
  private void CreateVehicle()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    Assert.IsTrue(PathingHelper.ShouldCreateRegions(group.vehicle.VehicleDef));
    pathData = mapping[group.vehicle.VehicleDef];

    mapping.deferredGridGeneration.DoPassExpectClear();
    Assert.IsTrue(pathData.Suspended);
  }

  [TearDown]
  private void DestroyVehicle()
  {
    group.Dispose();
    group = null;
  }

  [Test]
  [TestDescription("Player Faction vehicles use deferred generation in player maps.")]
  private void PlayerOnPlayerFactionMap()
  {
    using ScopedReferenceRollback<MapParent, Faction> srr = new(mapping.map.Parent, "factionInt", Faction.OfPlayer);
    Assert.IsTrue(group.vehicle.Faction.IsPlayer);
    Expect.AreEqual(DeferredGridGeneration.Urgency.Deferred, DeferredGridGeneration.UrgencyFor(group.vehicle));
  }

  [Test]
  [TestDescription("Player Faction vehicles use urgent region generation in npc maps.")]
  private void PlayerOnNpcFactionMap()
  {
    // Non-player factions must generate urgently, this will happen on event maps regardless of generation tick
    using ScopedReferenceRollback<MapParent, Faction> srr = new(mapping.map.Parent, "factionInt", Faction.OfPirates);
    Expect.AreEqual(DeferredGridGeneration.Urgency.Urgent, DeferredGridGeneration.UrgencyFor(mapping.map));
  }

  [Test]
  [TestDescription("Newly generated maps use urgent generation for player vehicles.")]
  private void PlayerOnMapGenerated()
  {
    using MockGameTicks mgt = new(mapping.map.generationTick);
    Expect.AreEqual(DeferredGridGeneration.Urgency.Urgent, DeferredGridGeneration.UrgencyFor(mapping.map));
  }

  [Test]
  [TestDescription("Pre-existing maps use deferred generation for player vehicles.")]
  private void PlayerOnExistingMap()
  {
    // We can allow deferred generation as the spawn position should already be chosen and no path or region grids
    // need immediate access to the grid.
    using MockGameTicks mgt = new(mapping.map.generationTick + 1);
    Expect.AreEqual(DeferredGridGeneration.Urgency.Deferred, DeferredGridGeneration.UrgencyFor(mapping.map));
  }
}