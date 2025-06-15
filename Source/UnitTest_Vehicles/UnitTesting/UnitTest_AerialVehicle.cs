using System.Collections.Generic;
using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.WorldPawnGC)]
internal sealed class UnitTest_AerialVehicle
{
  private readonly List<AerialVehicleInFlight> aerialVehicles = [];

  [SetUp]
  private void GenerateVehicles()
  {
    World world = Find.World;
    Assert.IsNotNull(world);
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);

    aerialVehicles.Clear();

    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      if (vehicleDef.type != VehicleType.Air)
        continue;
      if (!vehicleDef.properties.roles.NotNullAndAny(role => role.SlotsToOperate > 0))
        continue;

      VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
      AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(vehicle, map.Tile);
      aerialVehicles.Add(aerialVehicle);
    }
  }

  [Test, ExecutionPriority(Priority.First)]
  private void AerialVehicleInit()
  {
    foreach (AerialVehicleInFlight aerialVehicle in aerialVehicles)
    {
      using Test.Group group = new(aerialVehicle.vehicle.def.defName);
      VehiclePawn vehicle = aerialVehicle.vehicle;
      Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
      Assert.IsNotNull(colonist);
      Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
      Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
      Assert.IsNotNull(animal);
      Assert.AreEqual(animal.Faction, Faction.OfPlayer);

      VehicleRoleHandler handler = vehicle.handlers.FirstOrDefault();
      Assert.IsNotNull(handler, "Testing with aerial vehicle which has no roles");
      Expect.IsTrue(vehicle.TryAddPawn(colonist, handler), "TryAddPawn");
      Expect.IsTrue(
        vehicle.inventory.innerContainer.TryAddOrTransfer(animal,
          canMergeWithExistingStacks: false), "Inventory TryAddOrTransfer");
      Expect.IsFalse(vehicle.Destroyed, "Vehicle destroyed.");
      Expect.IsFalse(vehicle.Discarded, "Vehicle discarded.");
    }
  }

  [Test]
  private void AerialVehicleGC()
  {
    foreach (AerialVehicleInFlight aerialVehicle in aerialVehicles)
    {
      using Test.Group group = new(aerialVehicle.vehicle.def.defName);

      VehiclePawn vehicle = aerialVehicle.vehicle;

      // Pass vehicle and passengers to world
      Find.WorldPawns.PassToWorld(vehicle);
      foreach (Pawn pawn in vehicle.AllPawnsAboard)
      {
        Expect.IsFalse(pawn.Destroyed, "Passenger destroyed.");
        Expect.IsFalse(pawn.Discarded, "Passenger discarded.");
        if (!pawn.IsWorldPawn())
        {
          Find.WorldPawns.PassToWorld(pawn);
        }
      }
      // Pass inventory pawns to world
      foreach (Thing thing in vehicle.inventory.innerContainer)
      {
        if (thing is Pawn pawn && !pawn.IsWorldPawn())
        {
          Expect.IsFalse(pawn.Destroyed, "Inventory pawn destroyed.");
          Expect.IsFalse(pawn.Discarded, "Inventory pawn discarded.");
          Find.WorldPawns.PassToWorld(pawn);
        }
      }
      Expect.ReferencesAreEqual(vehicle.ParentHolder, aerialVehicle, "Vehicle ParentHolder");
      Expect.All(vehicle.AllPawnsAboard,
        pawn => pawn.ParentHolder is VehicleRoleHandler handler && handler.vehicle == vehicle,
        "Passenger ParentHolder");
      Expect.All(vehicle.inventory.innerContainer, pawn => ThingInVehicle(vehicle, pawn),
        "Inventory pawn ParentHolder");

      Find.WorldPawns.gc.CancelGCPass();
      _ = Find.WorldPawns.gc.PawnGCPass();

      Find.WorldPawns.gc.PawnGCDebugResults();
      Expect.IsFalse(vehicle.Destroyed, "Vehicle GC destroyed.");
      Expect.IsFalse(vehicle.Discarded, "Vehicle GC discarded.");
      Expect.None(vehicle.AllPawnsAboard, pawn => pawn.Destroyed, "Passenger GC destroyed.");
      Expect.None(vehicle.AllPawnsAboard, pawn => pawn.Discarded, "Passenger GC discarded.");
      Expect.None(vehicle.inventory.innerContainer, thing => thing.Destroyed,
        "Inventory GC destroyed.");
      Expect.None(vehicle.inventory.innerContainer, thing => thing.Discarded,
        "Inventory GC discarded.");

      aerialVehicle.Destroy();
      Expect.IsFalse(Find.WorldPawns.Contains(aerialVehicle.vehicle));
    }
    return;

    static bool ThingInVehicle(VehiclePawn vehicle, Thing thing)
    {
      if (thing is Pawn pawn)
      {
        return pawn.ParentHolder is Pawn_InventoryTracker inventoryTracker &&
          inventoryTracker.pawn == vehicle;
      }
      // ReSharper disable PossibleUnintendedReferenceComparison
      return thing.ParentHolder == vehicle.inventory;
    }
  }

  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void RemoveAllVehicleWorldPawns()
  {
    TestUtils.EmptyWorldAndMapOfVehicles();
    aerialVehicles.Clear();
  }
}