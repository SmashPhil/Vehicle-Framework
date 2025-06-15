using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.WorldPawnGC)]
internal sealed class UnitTest_StashedVehicle
{
  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void DestroyAllVehicles()
  {
    TestUtils.EmptyWorldAndMapOfVehicles();
  }

  private static VehiclePawn GetTransientVehicleWithPawns(out Pawn colonist, out Pawn animal)
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef($"VehicleDef_STASH_{Rand.Int}");
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

  [Test]
  private void Recovery()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    World world = Find.World;
    Assert.IsNotNull(world);

    VehiclePawn vehicle = GetTransientVehicleWithPawns(out Pawn colonist, out Pawn animal);
    Assert.IsNotNull(vehicle);

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([vehicle], Faction.OfPlayer, map.Tile, true);
    vehicleCaravan.Tile = map.Tile;

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);

    Expect.IsTrue(stashedVehicle.Vehicles.Contains(vehicle), "Vehicle Stashed");

    Assert.IsNotNull(caravan);
    Expect.IsTrue(caravan.PawnsListForReading.Contains(colonist), "Passenger Transferred");
    Expect.IsTrue(caravan.PawnsListForReading.Contains(animal), "Animal Transferred");
    Expect.IsEmpty(vehicle.AllPawnsAboard, "Vehicle DisembarkAll");
    Expect.IsFalse(vehicle.inventory.innerContainer.Contains(animal), "Animal Not Itemized");

    VehicleCaravan mergedVehicleCaravan = stashedVehicle.Notify_CaravanArrived(caravan);
    Assert.IsNotNull(mergedVehicleCaravan);
    Expect.IsTrue(caravan.Destroyed, "Caravan Destroyed");
    Expect.IsTrue(stashedVehicle.Destroyed, "StashedVehicle Destroyed");
    Expect.IsTrue(mergedVehicleCaravan.ContainsPawn(vehicle), "Vehicle Merged Into Caravan");

    mergedVehicleCaravan.Destroy();
    Assert.IsTrue(mergedVehicleCaravan.Destroyed);
  }

  [Test]
  private void WorldPawnGC()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    World world = Find.World;
    Assert.IsNotNull(world);

    VehiclePawn vehicle = GetTransientVehicleWithPawns(out _, out _);
    Assert.IsNotNull(vehicle);

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([vehicle], Faction.OfPlayer, map.Tile, true);
    vehicleCaravan.Tile = map.Tile;

    StashedVehicle stashedVehicle = StashedVehicle.Create(vehicleCaravan, out Caravan caravan);

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

    mergedVehicleCaravan.Destroy();

    // Vehicle should already be removed from WorldPawns immediately upon destruction
    Assert.IsFalse(Find.WorldPawns.Contains(vehicle));
  }
}