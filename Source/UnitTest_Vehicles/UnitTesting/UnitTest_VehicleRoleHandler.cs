using System.Linq;
using DevTools.Testing;
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
	[TestDescription("Colonists successfully board into the vehicle with no handler specified.")]
	private void BoardingColonists()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			drivers = 1,
			passengers = 1
		});

		TestUtils.ForceSpawn(group.vehicle);

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
		using ScopeEntity se = new(failColonist);
		Assert.AreEqual(failColonist.Faction, Faction.OfPlayer);
		Expect.IsFalse(group.vehicle.TryAddPawn(failColonist));
	}

	[TestDescription("Animals successfully board into the vehicle inventory.")]
	private void BoardingAnimals()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			animals = 1
		});
		Assert.AreEqual(group.pawns.Count, 1);

		TestUtils.ForceSpawn(group.vehicle);
		Pawn mechanoid = group.pawns[0];
		Expect.IsTrue(group.vehicle.TryAddPawn(mechanoid));

		// Boarding vehicle with no roles still works for mechs since they go to inventory
		Pawn failAnimal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, Faction.OfPlayer);
		Assert.IsNotNull(failAnimal);
		using ScopeEntity se = new(failAnimal);
		Assert.AreEqual(failAnimal.Faction, Faction.OfPlayer);
		Expect.IsTrue(group.vehicle.TryAddPawn(failAnimal));
		group.vehicle.DisembarkPawn(failAnimal);
	}

	[Test, LoadIfBiotechActive]
	[TestDescription("Mechanoids successfully board into the vehicle inventory.")]
	private void BoardingMechanoids()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			mechanoids = 1
		});
		Assert.AreEqual(group.pawns.Count, 1);

		TestUtils.ForceSpawn(group.vehicle);
		Pawn mechanoid = group.pawns[0];
		Expect.IsTrue(group.vehicle.TryAddPawn(mechanoid));

		// Boarding vehicle with no roles still works for mechs since they go to inventory
		Pawn failMechanoid = PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, Faction.OfPlayer);
		Assert.IsNotNull(failMechanoid);
		using ScopeEntity se = new(failMechanoid);
		Assert.AreEqual(failMechanoid.Faction, Faction.OfPlayer);
		Expect.IsTrue(group.vehicle.TryAddPawn(failMechanoid));
	}

	[Test]
	[TestDescription("Colonists successfully disembark from the vehicle.")]
	private void DisembarkColonistsSpawned()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			drivers = 1,
			passengers = 1
		});
		group.Spawn();

		Pawn driver = group.pawns[0];
		Assert.IsFalse(driver.Spawned);
		Assert.IsTrue(driver.InVehicle());
		Pawn passenger = group.pawns[1];
		Assert.IsFalse(passenger.Spawned);
		Assert.IsTrue(passenger.InVehicle());

		group.vehicle.DisembarkPawn(driver);
		Expect.IsTrue(driver.Spawned);
		Expect.IsFalse(driver.InVehicle());
		group.vehicle.DisembarkPawn(passenger);
		Expect.IsTrue(passenger.Spawned);
		Expect.IsFalse(passenger.InVehicle());
	}

	[Test]
	[TestDescription("Animals successfully disembark from the vehicle.")]
	private void DisembarkAnimalsSpawned()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			animals = 1
		});
		group.Spawn();

		Pawn animal = group.pawns[0];
		Assert.IsFalse(animal.Spawned);
		Assert.IsTrue(animal.InVehicle());

		group.vehicle.DisembarkPawn(animal);
		Expect.IsTrue(animal.Spawned);
		Expect.IsFalse(animal.InVehicle());
	}

	[Test, LoadIfBiotechActive]
	[TestDescription("Mechanoids successfully disembark from the vehicle.")]
	private void DisembarkMechanoidsSpawned()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			mechanoids = 1
		});
		group.Spawn();

		Pawn mechanoid = group.pawns[0];
		Assert.IsFalse(mechanoid.Spawned);
		Assert.IsTrue(mechanoid.InVehicle());

		group.vehicle.DisembarkPawn(mechanoid);
		Expect.IsTrue(mechanoid.Spawned);
		Expect.IsFalse(mechanoid.InVehicle());
	}

	[Test]
	[TestDescription("Verify that drivers and passengers are recached in vehicle lists.")]
	private void PawnListCaching()
	{
		const int Drivers = 1;
		const int Passengers = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			drivers = Drivers,
			passengers = Passengers
		});
		TestUtils.ForceSpawn(group.vehicle);
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Assert.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
		Assert.AreEqual(group.pawns.Count, Drivers + Passengers);

		Pawn colonist = group.pawns.FirstOrDefault(pawn => pawn.IsColonist);
		Assert.IsNotNull(colonist);
		Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, 1);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 1);

		group.BoardAll();
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, Drivers + Passengers);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, Drivers + Passengers);
	}

	[Test]
	[TestDescription("Verify that animals are not cached in vehicle lists as they are added to inventory")]
	private void PawnListCachingAnimals()
	{
		const int Animals = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			animals = Animals
		});
		TestUtils.ForceSpawn(group.vehicle);
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Assert.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
		Assert.AreEqual(group.pawns.Count, Animals);

		Pawn animal = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
		Assert.IsNotNull(animal);
		Assert.IsTrue(group.vehicle.TryAddPawn(animal));
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
		group.BoardAll();
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
	}

	[Test, LoadIfBiotechActive]
	[TestDescription("Verify that mechanoids are not cached in vehicle lists as they are added to inventory")]
	private void PawnListCachingMechs()
	{
		const int Mechanoids = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			mechanoids = Mechanoids
		});
		TestUtils.ForceSpawn(group.vehicle);
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Assert.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
		Assert.AreEqual(group.pawns.Count, Mechanoids);

		Pawn mech = group.pawns.FirstOrDefault(pawn => pawn.IsColonyMech);
		Assert.IsNotNull(mech);
		Assert.IsTrue(group.vehicle.TryAddPawn(mech));
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
		group.BoardAll();
		Expect.AreEqual(group.vehicle.AllPawnsAboard.Count, 0);
		Expect.AreEqual(group.vehicle.AllColonistsAboard.Count, 0);
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