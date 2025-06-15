using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestDescription("Vehicle is handled correctly with world pawn list.")]
[TestCategory(TestCategoryNames.WorldPawnGC)]
internal sealed class UnitTest_WorldPawns
{
  [Test]
  private void DestroyVehicleSpawned()
  {
    VehicleGroup group = CreateTransientGroup();

    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Pawn pawn = group.DisembarkOne();

    group.vehicle.DestroyVehicleAndPawns();
    Expect.IsTrue(group.vehicle.Destroyed);
    Expect.IsFalse(Find.WorldPawns.Contains(group.vehicle));
    foreach (Pawn innerPawn in group.pawns)
    {
      if (innerPawn == pawn)
      {
        Expect.IsTrue(pawn.Spawned);
        Expect.IsFalse(pawn.Destroyed);
        Expect.IsFalse(pawn.Discarded);
      }
      else
      {
        Expect.IsFalse(innerPawn.Spawned);
        Expect.IsTrue(innerPawn.Destroyed);
        Expect.IsFalse(innerPawn.Discarded);
        Expect.IsTrue(Find.WorldPawns.Contains(innerPawn));
      }
    }
    pawn.Destroy();
  }

  [Test]
  private void DestroyVehicleUnspawned()
  {
    VehicleGroup group = CreateTransientGroup();

    group.BoardAll();
    Assert.IsFalse(group.vehicle.Spawned);
    group.vehicle.DestroyVehicleAndPawns();
    Expect.IsTrue(group.vehicle.Destroyed);
    Expect.IsFalse(Find.WorldPawns.Contains(group.vehicle));
    foreach (Pawn pawn in group.pawns)
    {
      Expect.IsFalse(pawn.Spawned);
      Expect.IsTrue(pawn.Destroyed);
      Expect.IsFalse(pawn.Discarded);
      Expect.IsTrue(Find.WorldPawns.Contains(pawn));
    }
  }

  [Test]
  private void DestroyVehicleDeSpawned()
  {
    VehicleGroup group = CreateTransientGroup();

    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    foreach (Pawn pawn in group.pawns)
    {
      Expect.IsFalse(pawn.Spawned);
      Expect.IsFalse(pawn.Destroyed);
      Expect.IsTrue(pawn.IsInVehicle());
      Expect.IsFalse(pawn.IsWorldPawn());
    }
    group.vehicle.DeSpawn();
    Assert.IsFalse(group.vehicle.Spawned);
    Expect.IsFalse(group.vehicle.IsWorldPawn());
    foreach (Pawn pawn in group.pawns)
    {
      Expect.IsFalse(pawn.Spawned);
      Expect.IsFalse(pawn.Destroyed);
      Expect.IsFalse(pawn.Discarded);
      Expect.IsTrue(pawn.IsInVehicle());
      Expect.IsFalse(pawn.IsWorldPawn());
    }

    group.vehicle.DestroyVehicleAndPawns();
    Assert.IsTrue(group.vehicle.Destroyed);
    Expect.IsFalse(Find.WorldPawns.Contains(group.vehicle));
    foreach (Pawn pawn in group.pawns)
    {
      Expect.IsFalse(pawn.Spawned);
      Expect.IsFalse(pawn.Discarded);
      Expect.IsTrue(pawn.Destroyed);
      Expect.IsTrue(Find.WorldPawns.Contains(pawn));
    }
  }

  [Test]
  private void DestroyVehicleCaravan()
  {
    VehicleGroup group = CreateTransientGroup();
    group.BoardAll();
    Assert.IsFalse(group.vehicle.Spawned);

    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 0, true);
    Assert.IsTrue(group.vehicle.IsWorldPawn());
    foreach (Pawn pawn in group.pawns)
    {
      // Internal pawns are held and ticked by their vehicle
      Assert.IsFalse(pawn.IsWorldPawn());
    }
    // PawnsListForReading property is patched to return pawns inside vehicles, but internal pawn list
    // still only contains the vehicle. This is for mechanical reasons with how vehicle caravans are ticked
    // and this was the least destructive way to do it at the time of writing.
    Assert.AreEqual(caravan.PawnsListForReading.Count, group.pawns.Count + 1 /* +1 for vehicle */);
    Assert.AreEqual(caravan.pawns.Count, 1);

    Pawn survivor = group.DisembarkOne();
    Expect.IsFalse(survivor.Spawned);
    Expect.IsFalse(survivor.Discarded);
    Expect.IsFalse(survivor.Destroyed);
    Expect.IsTrue(Find.WorldPawns.Contains(survivor));
    Expect.IsTrue(survivor.InVehicleCaravan());
    Expect.IsFalse(survivor.IsInVehicle());

    caravan.Destroy();
    Expect.IsTrue(group.vehicle.Destroyed);
    Expect.IsFalse(Find.WorldPawns.Contains(group.vehicle));
    foreach (Pawn pawn in group.pawns)
    {
      // Preserve and keep them in world pawns to at least be recoverable
      Expect.IsFalse(pawn.Spawned);
      Expect.IsFalse(pawn.Discarded);
      Expect.IsFalse(pawn.Destroyed);
      Expect.IsTrue(Find.WorldPawns.Contains(pawn));
      Expect.IsFalse(pawn.IsInVehicle());
    }
  }

  [Test]
  private void DestroyVehicleAerialVehicle()
  {
    VehicleGroup group = CreateTransientGroup();

    group.BoardAll();
    Assert.IsFalse(group.vehicle.Spawned);
    group.vehicle.DestroyVehicleAndPawns();
    Expect.IsTrue(group.vehicle.Destroyed);
    Expect.IsFalse(Find.WorldPawns.Contains(group.vehicle));
    foreach (Pawn pawn in group.pawns)
    {
      Expect.IsFalse(pawn.Spawned);
      Expect.IsTrue(pawn.Destroyed);
      Expect.IsTrue(Find.WorldPawns.Contains(pawn));
    }
  }

  private static VehicleGroup CreateTransientGroup()
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef("VehicleDef_ForDestruction");
    vehicleDef.vehicleStats =
    [
      new VehicleStatModifier
      {
        statDef = VehicleStatDefOf.MoveSpeed,
        value = 10
      }
    ];

    vehicleDef.properties.roles =
    [
      new VehicleRole
      {
        key = "Passenger",
        slots = 2
      },
      new VehicleRole
      {
        key = "Driver",
        slots = 2,
        slotsToOperate = 2,

        handlingTypes = HandlingType.Movement
      }
    ];
    // VehicleDef needs to be complete by this point for PostGeneration events
    VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
    VehicleGroup group = new(vehicle);
    for (int i = 0; i < vehicle.handlers.Sum(handler => handler.role.Slots); i++)
    {
      Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
      Assert.IsNotNull(colonist);
      group.pawns.Add(colonist);
    }
    return group;
  }
}