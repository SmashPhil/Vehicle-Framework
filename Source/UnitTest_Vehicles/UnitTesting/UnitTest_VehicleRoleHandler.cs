using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using TickerTypeRollback = SmashTools.ScopedValueRollback<Verse.TickerType>;

namespace Vehicles.UnitTesting;

// Validation of vehicle functionality needs to occur before
[UnitTest(TestType.Playing), ExecutionPriority(Priority.AboveNormal)]
[TestCategory(TestCategoryNames.TickBehavior)]
[TestDescription("VehicleRoleHandler behavior and all logic surrounding board and disembark.")]
internal sealed class UnitTest_VehicleRoleHandler
{
  [Test]
  private void BoardingUnboarding()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 2,
      passengers = 2,
      animals = 2,
    });

    TestUtils.ForceSpawn(group.vehicle);
    Assert.IsTrue(group.vehicle.Spawned);

    // Colonists can board
    for (int i = 0; i < group.pawns.Count; i++)
    {
      Pawn pawn = group.pawns[i];
      Expect.IsTrue(group.vehicle.TryAddPawn(pawn), $"Boarded {i + 1}/{group.pawns.Count}");
    }
    Assert.IsTrue(group.pawns.All(pawn => pawn.InVehicle() && !pawn.Spawned));

    // Colonist cannot board full vehicle
    Pawn failColonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    Assert.IsNotNull(failColonist);
    Assert.AreEqual(failColonist.Faction, Faction.OfPlayer);
    Expect.IsFalse(group.vehicle.TryAddPawn(failColonist));

    failColonist.Destroy();

    if (ModsConfig.BiotechActive)
    {
      group.DisembarkAll();
      Pawn mechanoid =
        PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, Faction.OfPlayer);
      Assert.IsNotNull(mechanoid);
      Assert.AreEqual(mechanoid.Faction, Faction.OfPlayer);
      Expect.IsTrue(group.vehicle.TryAddPawn(mechanoid));
      group.vehicle.DisembarkPawn(mechanoid);
      mechanoid.Destroy();
    }
  }

  [Test]
  private void PawnListCaching()
  {
    const int Drivers = 2;
    const int Passengers = 2;
    const int Animals = 2;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = Drivers,
      passengers = Passengers,
      animals = Animals,
    });
    TestUtils.ForceSpawn(group.vehicle);
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsTrue(group.vehicle.AllPawnsAboard.Count == 0);
    Assert.IsTrue(group.vehicle.AllColonistsAboard.Count == 0);
    Assert.AreEqual(group.pawns.Count, Drivers + Passengers + Animals);

    Pawn colonist = group.pawns.FirstOrDefault(pawn => pawn.IsColonist);
    Assert.IsNotNull(colonist);
    Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
    Expect.IsTrue(group.vehicle.AllPawnsAboard.Count == 1);
    Expect.IsTrue(group.vehicle.AllColonistsAboard.Count == 1);
    Pawn animal = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
    Assert.IsNotNull(animal);
    Assert.IsTrue(group.vehicle.TryAddPawn(animal));
    Expect.IsTrue(group.vehicle.AllPawnsAboard.Count == 2);
    Expect.IsTrue(group.vehicle.AllColonistsAboard.Count == 1);
    group.BoardAll();
    Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, Drivers + Passengers + Animals);
    Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, Drivers + Passengers);
  }

  [Test]
  private void BoardingUnboardingCaravan()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 2,
      passengers = 2,
      animals = 2,
    });

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    Assert.AreEqual(group.vehicle.GetVehicleCaravan(), vehicleCaravan);

    // Colonists can board
    for (int i = 0; i < group.pawns.Count; i++)
    {
      Pawn pawn = group.pawns[i];
      Expect.IsTrue(group.vehicle.TryAddPawn(pawn), $"Boarded {i + 1}/{group.pawns.Count}");
    }
    Assert.IsTrue(group.pawns.All(pawn =>
      pawn.InVehicle() && !pawn.Spawned && pawn.InVehicleCaravan()));

    // Colonist cannot board full vehicle
    Pawn failColonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    Assert.IsNotNull(failColonist);
    Assert.AreEqual(failColonist.Faction, Faction.OfPlayer);
    Expect.IsFalse(group.vehicle.TryAddPawn(failColonist));

    failColonist.Destroy();

    if (ModsConfig.BiotechActive)
    {
      group.DisembarkAll();
      Assert.IsTrue(group.pawns.All(pawn =>
        !pawn.InVehicle() && !pawn.Spawned && pawn.InVehicleCaravan()));

      Pawn mechanoid =
        PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, Faction.OfPlayer);
      Assert.IsNotNull(mechanoid);
      Assert.AreEqual(mechanoid.Faction, Faction.OfPlayer);
      Expect.IsTrue(group.vehicle.TryAddPawn(mechanoid));
      Expect.IsTrue(mechanoid.InVehicle());
      Expect.IsTrue(mechanoid.InVehicleCaravan());
      group.vehicle.DisembarkPawn(mechanoid);
      Expect.IsFalse(mechanoid.InVehicle());
      Expect.IsTrue(mechanoid.InVehicleCaravan());
      vehicleCaravan.RemovePawn(mechanoid);
      Assert.IsFalse(mechanoid.InVehicleCaravan());
      mechanoid.Destroy();
    }
    vehicleCaravan.RemoveAllPawns();
  }

  // TODO
  [Test]
  private void ReservationChecks()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1,
      animals = 1,
    });

    TestUtils.ForceSpawn(group.vehicle);
    Assert.IsTrue(group.vehicle.Spawned);

    VehicleReservationManager reservationMgr =
      group.vehicle.Map.GetCachedMapComponent<VehicleReservationManager>();
    Assert.IsNotNull(reservationMgr);
    VehicleHandlerReservation reservation =
      reservationMgr.GetReservation<VehicleHandlerReservation>(group.vehicle);
    Assert.IsNull(reservation);
  }

  [Test]
  private void RoleTicking()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });

    group.Spawn();

    // Vehicle parent
    using (TickObserver<VehiclePawn> observer = new(group.vehicle))
    {
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    Pawn pawn = group.pawns.First();
    Assert.IsFalse(pawn.Spawned);
    Assert.IsTrue(pawn.InVehicle());
    // Internal roles
    using (TickObserver<Pawn> observer = new(pawn))
    {
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }
  }

  [Test]
  private void VariableTickRate()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });

    group.Spawn();

    // Vehicle VTR's with no pawns onboard
    group.DisembarkAll();
    group.vehicle.ignition.Drafted = false;
    Assert.IsTrue(group.vehicle.AllPawnsAboard.Count == 0);
    Assert.IsFalse(group.vehicle.Drafted);
    Expect.AreEqual(group.vehicle.UpdateRateTicks, VehiclePawn.MaxTickInterval);
    group.BoardAll();
    Expect.AreEqual(group.vehicle.UpdateRateTicks, GenTicks.GetCameraUpdateRate(group.vehicle));
  }

  [Test]
  private void RoleTickingCaravan()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 2,
      passengers = 2,
      animals = 2
    });
    group.BoardAll();

    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    Assert.AreEqual(group.vehicle.GetVehicleCaravan(), vehicleCaravan);
    Assert.IsTrue(vehicleCaravan.PawnsListForReading.ContainsAllOf(group.pawns));

    using (TickObserver<VehiclePawn> observer = new(group.vehicle))
    {
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    Pawn pawn = group.vehicle.AllPawnsAboard.FirstOrDefault();
    Assert.IsNotNull(pawn);
    Assert.IsTrue(pawn.InVehicle());
    using (TickObserver<Pawn> observer = new(pawn))
    {
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    Pawn dismountedPawn = group.DisembarkOne();
    Assert.IsNotNull(dismountedPawn);
    Assert.IsFalse(dismountedPawn.InVehicle());
    using (TickObserver<Pawn> observer = new(dismountedPawn))
    {
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    ThingWithComps food = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
    food.stackCount = 1;
    group.vehicle.inventory.TryAddAndUnforbid(food);
    Assert.AreEqual(food.ParentHolder, group.vehicle.inventory);
    using (TickObserver<ThingWithComps> observer = new(food))
    {
      using TickerTypeRollback rb = new(ref food.def.tickerType);
      food.def.tickerType = TickerType.Normal;
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    vehicleCaravan.RemoveAllPawns();
  }

  [Test] // TODO
  private void RoleTickingAerialVehicle()
  {
  }
}