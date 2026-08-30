using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Priority = DevTools.Testing.Priority;
using TickerTypeRollback = CoreLib.ScopedValueRollback<Verse.TickerType>;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.WorldObject,
  TestCategoryNames.WorldPawnGC,
  TestCategoryNames.Caravaning
)]
[TestDescription(
  "VehicleCaravan mechanics for stashing and recovering a vehicle on the world map.")]
internal sealed class Test_StashedVehicle
{
  private static readonly MethodInfo ThingUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.UpdateRateTicks));

  private static readonly MethodInfo VehicleUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.UpdateRateTicks));

  private static readonly MethodInfo CheckAnyNonWorldPawns =
    AccessTools.Method(typeof(Caravan), "CheckAnyNonWorldPawns");

  private static readonly MethodInfo OverrideMethod =
    AccessTools.Method(typeof(Test_VehicleCaravan_Tick), nameof(OverrideUpdateRateTicks));

  private static void OverrideUpdateRateTicks(out int __result)
  {
    __result = 1;
  }

  private static VehiclePawn GetTransientVehicleWithPawns(out Pawn colonist, out Pawn animal)
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef($"VehicleDef_STASH_{Rand.Int}", null);
    vehicleDef.properties.roles =
    [
      new VehicleRole
      {
        key = "Passenger",
        slots = 1
      },
      new VehicleRole
      {
        key = "Driver",
        slots = 1,
        slotsToOperate = 1,

        handlingTypes = HandlingType.Movement
      }
    ];
    VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
    colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    Assert.IsNotNull(colonist);
    Assert.IsTrue(colonist.Faction == Faction.OfPlayer);
    animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
    Assert.IsNotNull(animal);
    Assert.IsTrue(animal.Faction == Faction.OfPlayer);

    VehicleRoleHandler handler = vehicle.handlers.FirstOrDefault();
    Assert.IsNotNull(handler);
    Assert.IsTrue(vehicle.TryAddPawn(colonist, handler));
    Assert.IsTrue(
      vehicle.inventory.innerContainer.TryAddOrTransfer(animal,
        canMergeWithExistingStacks: false));
    Assert.IsFalse(vehicle.Destroyed);
    Assert.IsFalse(vehicle.Discarded);
    return vehicle;
  }

  [TearDown]
  private void ClearSaveOfStashedVehicle()
  {
    SaveTester.Write();
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  private void Create()
  {
    const int Drivers = 2;
    const int Passengers = 2;
    const int Animals = 1;

    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = Drivers,
      passengers = Passengers,
      animals = Animals
    });
    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, map.Tile, true);
    Assert.IsNotNull(vehicleCaravan);
    Expect.AreEqual(vehicleCaravan, group.vehicle.ParentHolder);
    using ScopeWorldObject swo = new(vehicleCaravan);
    group.DisembarkOne();

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    using ScopeWorldObject swc = new(stashedVehicle);
    using ScopeWorldObject sc = new(caravan);

    Expect.AreEqual(stashedVehicle, group.vehicle.ParentHolder);
    Expect.IsTrue(stashedVehicle.Vehicles.Contains(group.vehicle));
    CheckAnyNonWorldPawns.Invoke(caravan, null);
    Expect.AreEqual(caravan.PawnsListForReading.Count, Drivers + Passengers + Animals);
    List<WorldObject> caravansAtTile = Find.WorldObjects.ObjectsAt(stashedVehicle.Tile).Where(obj => obj is Caravan).ToList();
    Expect.AreEqual(caravansAtTile.Count, 1);
  }

  [Test]
  private void Recover()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    VehiclePawn vehicle = GetTransientVehicleWithPawns(out Pawn colonist, out Pawn animal);
    Assert.IsNotNull(vehicle);
    using ScopeEntity se = new(vehicle);

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([vehicle], Faction.OfPlayer, map.Tile, true);
    Assert.IsNotNull(vehicleCaravan);
    Expect.AreEqual(vehicleCaravan, vehicle.ParentHolder);
    using ScopeWorldObject svc = new(vehicleCaravan);
    vehicleCaravan.Tile = map.Tile;

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    Assert.IsNotNull(stashedVehicle);
    Assert.IsNotNull(caravan);
    using ScopeWorldObject ssv = new(stashedVehicle);
    using ScopeWorldObject sc = new(caravan);

    Expect.AreEqual(stashedVehicle, vehicle.ParentHolder);
    Expect.IsTrue(stashedVehicle.Vehicles.Contains(vehicle), "Vehicle Stashed");

    Assert.IsNotNull(caravan);
    Expect.IsTrue(caravan.PawnsListForReading.Contains(colonist), "Passenger Transferred");
    Expect.IsTrue(caravan.PawnsListForReading.Contains(animal), "Animal Transferred");
    Expect.IsEmpty(vehicle.AllPawnsAboard, "Vehicle DisembarkAll");
    Expect.IsFalse(vehicle.inventory.innerContainer.Contains(animal), "Animal Not Itemized");

    VehicleCaravan mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
    using ScopeWorldObject smvc = new(mergedVehicleCaravan);
    Assert.IsNotNull(mergedVehicleCaravan);
    Expect.IsTrue(mergedVehicleCaravan.PawnsListForReading.Contains(colonist), "Passenger Transferred");
    Expect.IsTrue(mergedVehicleCaravan.PawnsListForReading.Contains(animal), "Animal Transferred");
    Expect.IsTrue(mergedVehicleCaravan.ContainsPawn(vehicle), "Vehicle Merged Into Caravan");
    Expect.IsTrue(caravan.Destroyed, "Caravan Destroyed");
    Expect.IsTrue(stashedVehicle.Destroyed, "StashedVehicle Destroyed");
    List<WorldObject> caravansAtTile = Find.WorldObjects.ObjectsAt(mergedVehicleCaravan.Tile).Where(obj => obj is Caravan).ToList();
    Expect.AreEqual(caravansAtTile.Count, 1);
  }

  [Test]
  private void RecoverWithExcess()
  {
    const int ExcessPawnCount = 3;

    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1,
      animals = 1,
      prisoners = 1
    });
    group.BoardAll();

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, map.Tile, true);
    Assert.IsNotNull(vehicleCaravan);
    Expect.AreEqual(vehicleCaravan, group.vehicle.ParentHolder);
    using ScopeWorldObject svc = new(vehicleCaravan);
    vehicleCaravan.Tile = map.Tile;

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    Assert.IsNotNull(stashedVehicle);
    Assert.IsNotNull(caravan);
    Expect.AreEqual(stashedVehicle, group.vehicle.ParentHolder);
    using ScopeWorldObject ssv = new(stashedVehicle);
    using ScopeWorldObject sc = new(caravan);

    List<Pawn> excessPawns = [];
    for (int i = 0; i < ExcessPawnCount; i++)
    {
      Pawn excessPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
        Faction.OfPlayer, fixedBiologicalAge: 30, forceNoBackstory: true));
      caravan.AddPawn(excessPawn, false);
      Find.WorldPawns.PassToWorld(excessPawn);
      group.pawns.Add(excessPawn);
      excessPawns.Add(excessPawn);
    }

    VehicleCaravan mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
    Expect.AreEqual(mergedVehicleCaravan, group.vehicle.ParentHolder);
    using ScopeWorldObject smvc = new(mergedVehicleCaravan);
    mergedVehicleCaravan.RecacheVehicles();
    // +1 for vehicle
    Expect.AreEqual(mergedVehicleCaravan.PawnsListForReading.Count, group.pawns.Count + 1);
    Expect.All(excessPawns, pawn => mergedVehicleCaravan.PawnsListForReading.Contains(pawn));
    // +1 for animal, recovering stash only boards roles and send animals to cargo.
    Expect.AreEqual(mergedVehicleCaravan.DismountedPawnsListForReading.Count, ExcessPawnCount + 1);
  }

  [Test]
  [TestDescription("Verify that saving with created and recovered vehicle stash does not log scribe warnings.")]
  private void Save()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1,
      animals = 1
    });

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, map.Tile, true);
    Expect.AreEqual(vehicleCaravan, group.vehicle.ParentHolder);
    vehicleCaravan.Tile = map.Tile;
    using ScopeWorldObject scopeCaravan = new(vehicleCaravan);
    SaveTester.Write();

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    Expect.AreEqual(stashedVehicle, group.vehicle.ParentHolder);
    Assert.IsTrue(stashedVehicle.Vehicles.Contains(group.vehicle), "Vehicle Stashed");
    Assert.IsNotNull(caravan);
    using ScopeWorldObject scopeStash = new(stashedVehicle);
    SaveTester.Write();

    VehicleCaravan mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
    Expect.AreEqual(mergedVehicleCaravan, group.vehicle.ParentHolder);
    Assert.IsNotNull(mergedVehicleCaravan);
    using ScopeWorldObject scopeMerge = new(mergedVehicleCaravan);
    SaveTester.Write();
  }

  [Test]
  private void StashTick()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1,
      animals = 1
    });

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, map.Tile, true);
    Expect.AreEqual(vehicleCaravan, group.vehicle.ParentHolder);
    vehicleCaravan.Tile = map.Tile;
    using ScopeWorldObject scopeCaravan = new(vehicleCaravan);

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    Expect.AreEqual(stashedVehicle, group.vehicle.ParentHolder);
    Assert.IsTrue(stashedVehicle.Vehicles.Contains(group.vehicle), "Vehicle Stashed");
    Assert.IsNotNull(caravan);
    Assert.IsFalse(stashedVehicle.GetDirectlyHeldThings().dontTickContents);
    ThingWithComps beer = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.Beer);
    beer.stackCount = 1;
    Assert.AreEqual(stashedVehicle.GetDirectlyHeldThings().TryAddOrTransfer(beer, beer.stackCount), beer.stackCount);
    using ScopeWorldObject scopeStash = new(stashedVehicle);

    using (new ScopedMethodHook(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod)))
    {
      using TickObserver<VehiclePawn> to = new(group.vehicle);
      Assert.AreEqual(group.vehicle.UpdateRateTicks, 1);
      Assert.IsFalse(group.vehicle.IsWorldPawn());
      stashedVehicle.DoTick();
      Expect.AreEqual(to.TickCount, 1);
    }

    using (new ScopedMethodHook(ThingUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod)))
    {
      using TickObserver<ThingWithComps> to = new(beer);
      using TickerTypeRollback ttr = new(ref beer.def.tickerType);
      beer.def.tickerType = TickerType.Normal;
      Assert.AreEqual(beer.UpdateRateTicks, 1);
      Assert.IsTrue(stashedVehicle.GetDirectlyHeldThings().Contains(beer));
      stashedVehicle.DoTick();
      Expect.AreEqual(to.TickCount, 1);
    }
  }

  [Test]
  private void WorldPawnGC()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);

    VehiclePawn vehicle = GetTransientVehicleWithPawns(out _, out _);
    Assert.IsNotNull(vehicle);

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([vehicle], Faction.OfPlayer, map.Tile, true);
    Expect.AreEqual(vehicleCaravan, vehicle.ParentHolder);
    using ScopeWorldObject scopeCaravan = new(vehicleCaravan);
    vehicleCaravan.Tile = map.Tile;

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);
    using ScopeWorldObject scopeStash = new(stashedVehicle);

    Expect.AreEqual(stashedVehicle, vehicle.ParentHolder);
    Expect.IsTrue(stashedVehicle.Vehicles.Contains(vehicle), "Vehicle Stashed");

    Find.WorldPawns.gc.CancelGCPass();
    _ = Find.WorldPawns.gc.PawnGCPass();

    // Ensure vehicle is not destroyed by GC in stashed vehicle WorldObject
    Expect.IsFalse(vehicle.Destroyed, "Vehicle GC Destroyed");
    Expect.IsFalse(vehicle.Discarded, "Vehicle GC Discarded");

    // Sanity check with vanilla caravan and any lingering pawn references that could lead
    // to unintended pawn destruction from GC
    foreach (Pawn pawn in caravan.PawnsListForReading)
    {
      Expect.IsFalse(pawn.Destroyed, "Passenger GC Destroyed");
      Expect.IsFalse(pawn.Discarded, "Passenger GC Discarded");
    }

    VehicleCaravan mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
    using ScopeWorldObject scopeMergeCaravan = new(mergedVehicleCaravan);

    Expect.AreEqual(mergedVehicleCaravan, vehicle.ParentHolder);
    Assert.IsNotNull(mergedVehicleCaravan);
    Assert.IsTrue(mergedVehicleCaravan.ContainsPawn(vehicle), "Vehicle Merged Into Caravan");

    Find.WorldPawns.gc.CancelGCPass();
    _ = Find.WorldPawns.gc.PawnGCPass();

    // Reclaiming stashed vehicle and transforming to VehicleCaravan should still not invoke cleanup
    // from WorldPawnGC.
    Expect.IsFalse(vehicle.Destroyed, "Vehicle GC Destroyed");
    Expect.IsFalse(vehicle.Discarded, "Vehicle GC Discarded");

    foreach (Pawn pawn in caravan.PawnsListForReading)
    {
      Expect.IsFalse(pawn.Destroyed, "Passenger GC Destroyed");
      Expect.IsFalse(pawn.Discarded, "Passenger GC Discarded");
    }
  }
}