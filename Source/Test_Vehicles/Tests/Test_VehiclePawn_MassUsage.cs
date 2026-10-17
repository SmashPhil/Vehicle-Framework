using System.Collections.Generic;
using DevTools.Testing;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Caravaning)]
[TestDescription("Vehicle mass usage calculations.")]
internal sealed class Test_VehiclePawn_MassUsage
{
  [Test]
  private void PawnInsideVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      passengers = 1,
      destroyInventory = true
    });
    Assert.AreEqual(group.pawns.Count, 1);
    Pawn pawn = group.pawns[0];
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    steel.stackCount = 3;
    Assert.IsTrue(pawn.inventory.innerContainer.TryAdd(steel));
    Assert.IsTrue(MassUtility.InventoryMass(pawn) > 0);

    group.BoardAll();
    Assert.IsTrue(pawn.InVehicle());
    List<ThingCount> things = [new(group.vehicle, 1), new(pawn, 1)];

    Expect.AreApproximatelyEqual(0,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.DontIgnore));
    Expect.AreApproximatelyEqual(0,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.Ignore));
  }

  [Test]
  private void PawnOutsideVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      passengers = 1,
      destroyInventory = true
    });
    Assert.AreEqual(expected: 1, group.pawns.Count);
    Pawn pawn = group.pawns[0];
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    steel.stackCount = 3;
    Assert.IsTrue(pawn.inventory.innerContainer.TryAdd(steel));
    Assert.IsFalse(pawn.InVehicle());

    float gearMass = MassUtility.GearMass(pawn);
    float inventoryMass = steel.GetStatValue(StatDefOf.Mass) * steel.stackCount;
    Assert.IsTrue(inventoryMass > 0);
    List<ThingCount> things = [new(group.vehicle, 1), new(pawn, 1)];

    Expect.AreApproximatelyEqual(gearMass + inventoryMass,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.DontIgnore));
    Expect.AreApproximatelyEqual(gearMass,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.Ignore));
  }

  [Test]
  private void CorpseInsideVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      passengers = 1,
      destroyInventory = true
    });
    Assert.AreEqual(group.pawns.Count, 1);
    Pawn pawn = group.pawns[0];
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    steel.stackCount = 3;
    Assert.IsTrue(pawn.inventory.innerContainer.TryAdd(steel));
    Assert.IsTrue(MassUtility.InventoryMass(pawn) > 0);

    float mass = pawn.GetStatValue(StatDefOf.Mass);
    Assert.IsTrue(mass > 0);
    pawn.health.SetDead();
    Corpse corpse = pawn.MakeCorpse(assignedGrave: null, currentBed: null);
    Assert.IsNotNull(corpse);
    using ScopeEntity scopeCorpse = new(corpse);
    Assert.IsTrue(pawn.Dead);
    Assert.IsTrue(group.vehicle.inventory.innerContainer.TryAdd(corpse));
    Assert.AreEqual(corpse.ParentHolder, group.vehicle.inventory);
    List<ThingCount> things = [new(group.vehicle, 1)];

    Expect.AreApproximatelyEqual(mass,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.DontIgnore));
    Expect.AreApproximatelyEqual(0,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.Ignore));
  }

  [Test]
  private void SpawnedCorpseIgnoresGearAndInventory()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      passengers = 1,
      destroyInventory = true
    });
    Assert.AreEqual(expected: 1, group.pawns.Count);
    Pawn pawn = group.pawns[0];
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    steel.stackCount = 3;
    Assert.IsTrue(pawn.inventory.innerContainer.TryAdd(steel));
    Assert.IsTrue(MassUtility.InventoryMass(pawn) > 0);

    float gearAndInventoryMass = MassUtility.GearAndInventoryMass(pawn);
    float mass = pawn.GetStatValue(StatDefOf.Mass) - gearAndInventoryMass;
    Assert.IsTrue(mass > 0);
    pawn.health.SetDead();
    Corpse corpse = pawn.MakeCorpse(assignedGrave: null, currentBed: null);
    Assert.IsNotNull(corpse);
    using ScopeEntity scopeCorpse = new(corpse);
    GenSpawn.Spawn(corpse, CellFinder.RandomSpawnCellForPawnNear(map.Center, map), map);
    Assert.IsTrue(corpse.Spawned);
    List<ThingCount> things = [new(corpse, 1)];

    Expect.AreApproximatelyEqual(mass,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.DontIgnore,
        includePawnsMass: false, ignoreSpawnedCorpsesGearAndInventory: true));
  }

  [Test]
  private void ThingInsideVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    steel.stackCount = 5;
    Assert.IsTrue(group.vehicle.inventory.innerContainer.TryAdd(steel));
    Assert.AreEqual(steel.ParentHolder, group.vehicle.inventory);

    float mass = steel.GetStatValue(StatDefOf.Mass) * steel.stackCount;
    Assert.IsTrue(mass > 0);
    List<ThingCount> things = [new(group.vehicle, 1)];

    Expect.AreApproximatelyEqual(mass,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.DontIgnore));
    Expect.AreApproximatelyEqual(0,
      CollectionsMassCalculator.MassUsage(things, IgnorePawnsInventoryMode.Ignore));
  }
}
