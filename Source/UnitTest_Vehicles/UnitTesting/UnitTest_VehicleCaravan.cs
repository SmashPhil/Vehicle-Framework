using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
	TestCategoryNames.TickBehavior,
	TestCategoryNames.WorldObject,
	TestCategoryNames.WorldPawnGC,
	TestCategoryNames.Caravaning
)]
[TestDescription("VehicleCaravan mechanics on the world map.")]
internal sealed class UnitTest_VehicleCaravan
{
	private static readonly MethodInfo MergeCaravansMethod =
		AccessTools.Method(typeof(CaravanMergeUtility), "MergeCaravans");

	private static readonly MethodInfo SplitCaravansMethod =
		AccessTools.Method(typeof(Dialog_SplitCaravan), "TrySplitCaravan");

	private static readonly AccessTools.FieldRef<Dialog_SplitCaravan, List<TransferableOneWay>>
		TransferablesFieldRef =
			AccessTools.FieldRefAccess<Dialog_SplitCaravan, List<TransferableOneWay>>("transferables");

	private static readonly List<Caravan> TmpCaravans = [];

	private static bool CaravansAt(PlanetTile tile)
	{
		using ClearOnDispose<Caravan> slr = new(TmpCaravans);
		Find.WorldObjects.GetPlayerControlledCaravansAt(tile, TmpCaravans);
		return TmpCaravans.Count > 0;
	}

	[Test]
	private void GetCaravan()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
		Assert.AreEqual(group.pawns.Count, 1);

		group.BoardAll();
		VehicleCaravan vehicleCaravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(vehicleCaravan);
		Assert.AreEqual(vehicleCaravan, group.vehicle.GetVehicleCaravan());
		Assert.AreEqual(vehicleCaravan, group.pawns[0].GetVehicleCaravan());
		Expect.AreEqual(vehicleCaravan, group.vehicle.GetCaravan());
		Assert.AreEqual(vehicleCaravan, group.pawns[0].GetCaravan());
	}

	[Test]
	private void BoardToCaravan()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			prisoners = 1
		});
		Dictionary<Pawn, Faction> factions = group.pawns.ToDictionary(pawn => pawn, pawn => pawn.Faction);

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle, .. group.pawns], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(caravan);
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		group.BoardOne();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		group.BoardAll();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		return;

		bool FactionDidNotChange(Pawn pawn)
		{
			return pawn.Faction == factions[pawn];
		}
	}

	[Test]
	private void DisembarkToCaravan()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			prisoners = 1
		});
		Dictionary<Pawn, Faction> factions = group.pawns.ToDictionary(pawn => pawn, pawn => pawn.Faction);

		group.BoardOne();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		group.BoardAll();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(caravan);
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		group.DisembarkOne();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);

		group.DisembarkAll();
		Assert.IsTrue(group.pawns.All(FactionDidNotChange));
		Expect.All(group.pawns, FactionDidNotChange);
		return;

		bool FactionDidNotChange(Pawn pawn)
		{
			return pawn.Faction == factions[pawn];
		}
	}

	[Test]
	private void AllInventoryItems()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});
		group.BoardAll();
		VehicleCaravan vehicleCaravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(vehicleCaravan);
		Pawn dismounted = group.DisembarkOne();
		Pawn mounted = group.pawns.FirstOrDefault(Ext_Vehicles.InVehicle);
		Assert.AreNotEqual(dismounted, mounted);

		List<Thing> inventoryItems = CaravanInventoryUtility.AllInventoryItems(vehicleCaravan);
		ThingWithComps beer = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.Beer);
		beer.stackCount = 1;
		using ScopeEntity scopeBeer = new(beer);
		ThingWithComps mealPack = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
		mealPack.stackCount = 1;
		using ScopeEntity scopeMealPack = new(mealPack);
		ThingWithComps yayo = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.Yayo);
		yayo.stackCount = 1;
		using ScopeEntity scopeYayo = new(yayo);
		ThingWithComps mealSimple = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
		mealSimple.stackCount = 1;
		using ScopeEntity scopeMealSimple = new(mealSimple);

		Assert.IsFalse(inventoryItems.Contains(beer));
		Assert.IsFalse(inventoryItems.Contains(mealPack));
		Assert.IsFalse(inventoryItems.Contains(yayo));
		Assert.IsFalse(inventoryItems.Contains(mealSimple));
		mounted.inventory.TryAddAndUnforbid(beer);
		dismounted.inventory.TryAddAndUnforbid(mealPack);
		group.vehicle.inventory.TryAddAndUnforbid(yayo);
		vehicleCaravan.AddPawnOrItem(mealSimple, true);
		inventoryItems = CaravanInventoryUtility.AllInventoryItems(vehicleCaravan);
		Expect.IsTrue(inventoryItems.Contains(beer));
		Expect.IsTrue(inventoryItems.Contains(mealPack));
		Expect.IsTrue(inventoryItems.Contains(yayo));
		Expect.IsTrue(inventoryItems.Contains(mealSimple));
	}

	[Test]
	private void VanillaVisibility()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 5,
			properties = new VehicleProperties
			{
				visibilityWeight = 6
			}
		});
		Assert.AreEqual(group.pawns.Count, 6);

		// Base game caravans should behave as expected
		Caravan caravan = CaravanMaker.MakeCaravan(group.pawns, Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(caravan);

		float visibility = CaravanVisibilityCalculator.Visibility(caravan);
		// weight = 6
		Assert.AreApproximatelyEqual(caravan.Visibility, CaravanVisibilityCalculator.NotMovingFactor);
		Assert.AreApproximatelyEqual(visibility, CaravanVisibilityCalculator.NotMovingFactor);

		// Remove group pawns first or else we'll be testing with destroyed group pawns.
		caravan.RemoveAllPawns();
		caravan.Destroy();
		Assert.IsTrue(caravan.Destroyed);
	}

	[Test]
	private void Visibility()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 5,
			properties = new VehicleProperties
			{
				visibilityWeight = 6
			}
		});
		Assert.AreEqual(group.pawns.Count, 6);

		group.BoardAll();
		VehicleCaravan vehicleCaravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(vehicleCaravan);

		Assert.IsFalse(vehicleCaravan.vehiclePather.MovingNow);
		Assert.AreEqual(vehicleCaravan.pawns.Count, 1);
		Assert.AreEqual(vehicleCaravan.pawns[0], group.vehicle);
		float visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);

		// weight = 6
		Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
			1 * CaravanVisibilityCalculator.NotMovingFactor);
		Expect.AreApproximatelyEqual(visibility, 1 * CaravanVisibilityCalculator.NotMovingFactor);
		group.vehicle.DisembarkAll();
		Assert.AreEqual(vehicleCaravan.pawns.Count, 7);

		// weight = 12
		visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);
		Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
			1.12f * CaravanVisibilityCalculator.NotMovingFactor);
		Expect.AreApproximatelyEqual(visibility, 1.12f * CaravanVisibilityCalculator.NotMovingFactor);

		// Moving
		visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1.12f);
		visibility =
			CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1.12f);
		group.BoardAll();
		visibility =
			CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1);

		// Pawns inside vehicles (returned by getter) should not count in visibility
		visibility =
			CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1);

		// Visibility is capped at 112%
		group.vehicle.VehicleDef.properties.visibilityWeight = 999;
		visibility =
			CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1.12f);
		group.DisembarkAll();
		visibility =
			CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
		Expect.AreApproximatelyEqual(visibility, 1.12f);
	}

	[Test, Disabled] // TODO
	private void Moving()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
	}

	[Test, Disabled] // TODO
	private void MovingNow()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
	}

	[Test, Disabled] // TODO
	private void ShouldRest()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
	}

	[Test]
	[TestDescription("Appends pawns in vehicles to property for key caravan mechanics.")]
	private void PawnsListForReading()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			extraSlots = 999
		});

		group.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(caravan);

		// 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
		Expect.AreEqual(caravan.pawns.Count, 1);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 2);

		Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
			Faction.OfPlayer, fixedBiologicalAge: 30));
		using ScopeEntity se = new(colonist);
		Assert.IsNotNull(colonist);
		Assert.AreEqual(colonist.Faction, Faction.OfPlayer);

		// Adding pawn to caravan recaches
		// 1 vehicle, 1 onboard, 1 dismounted (2 in caravan, 1 implicit)
		caravan.AddPawn(colonist, true);
		Expect.AreEqual(caravan.pawns.Count, 2);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

		// Removing pawn to caravan recaches as well
		// 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
		caravan.RemovePawn(colonist);
		Expect.AreEqual(caravan.pawns.Count, 1);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 2);

		// Adding to vehicle directly recaches
		// 1 vehicle, 2 onboard (1 in caravan, 2 implicit)
		Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
		Expect.AreEqual(caravan.pawns.Count, 1);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

		// Disembarking from vehicle recaches
		// 1 vehicle, 1 onboard, 1 dismounted (2 in caravan, 1 implicit)
		group.vehicle.DisembarkPawn(colonist);
		Assert.IsFalse(colonist.InVehicle());
		Assert.IsTrue(colonist.InVehicleCaravan());
		Expect.AreEqual(caravan.pawns.Count, 2);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

		// Removing from vehicle recaches and does NOT add them to caravan
		// 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
		Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
		Assert.IsTrue(colonist.InVehicle());
		Assert.IsTrue(colonist.InVehicleCaravan());
		group.vehicle.RemovePawn(colonist);
		Expect.AreEqual(caravan.pawns.Count, 1);
		Expect.AreEqual(caravan.PawnsListForReading.Count, 2);
		Expect.IsFalse(colonist.InVehicle());
		Expect.IsFalse(colonist.InVehicleCaravan());
	}

	[Test]
	[TestDescription("Adding vehicle to normal caravan should create VehicleCaravan and move all pawns.")]
	private void UpgradeToVehicleCaravan()
	{
		const int Drivers = 1;
		const int Passengers = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = Drivers,
			passengers = Passengers
		});
		Caravan caravan = CaravanMaker.MakeCaravan(group.pawns, Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);
		Assert.IsFalse(caravan.pawns.InnerListForReading.Exists(pawn => pawn is VehiclePawn));

		caravan.AddPawn(group.vehicle, addCarriedPawnToWorldPawnsIfAny: true);
		Assert.IsTrue(caravan.Destroyed);
		VehicleCaravan vehicleCaravan = group.vehicle.GetVehicleCaravan();
		Assert.IsNotNull(vehicleCaravan);
		using ScopeWorldObject svc = new(vehicleCaravan);

		Expect.ReferencesAreEqual(vehicleCaravan, Find.WorldObjects.PlayerControlledCaravanAt(1) as VehicleCaravan);
		// Caravan transition automatically boards as many pawns as possible.
		Expect.AreEqual(vehicleCaravan.VehiclesListForReading.Count, 1);
		Expect.AreEqual(vehicleCaravan.DismountedPawnsListForReading.Count, 0);
		Expect.AreEqual(vehicleCaravan.PawnsListForReading.Count, Drivers + Passengers + 1 /* for Vehicle */);
	}

	[Test]
	[TestDescription("Removing vehicle from VehicleCaravan should create vanilla Caravan and move all pawns.")]
	private void DowngradeToVehicleCaravan()
	{
		const int Drivers = 1;
		const int Passengers = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = Drivers,
			passengers = Passengers
		});
		group.BoardAll();

		VehicleCaravan vehicleCaravan = CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(vehicleCaravan);
		using ScopeWorldObject svc = new(vehicleCaravan);
		Assert.IsTrue(vehicleCaravan.VehiclesListForReading.Count == 1);
		group.vehicle.DisembarkAll();
		Assert.AreEqual(vehicleCaravan.DismountedPawnsListForReading.Count, group.pawns.Count);
		vehicleCaravan.RemovePawn(group.vehicle);

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Caravan caravan = group.pawns[0].GetCaravan();
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Expect.ReferencesAreEqual(caravan, Find.WorldObjects.PlayerControlledCaravanAt(1));
		Expect.IsTrue(caravan is not VehicleCaravan);
		// Caravan transition automatically boards as many pawns as possible.
		Expect.AreEqual(caravan.PawnsListForReading.Count, Drivers + Passengers);
	}

	[Test]
	private void SplitIntoVanillaCaravans()
	{
		const int PawnCount = 5;

		Assert.IsFalse(CaravansAt(1));

		List<Pawn> pawns = [];
		for (int j = 0; j < PawnCount; j++)
		{
			Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
				Faction.OfPlayer, fixedBiologicalAge: 30));
			Assert.IsNotNull(colonist);
			Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
			pawns.Add(colonist);
		}
		Caravan caravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, 1, true);
		using ScopeWorldObject scopeCaravan = new(caravan);

		Dialog_SplitCaravan splitCaravanDlg = new(caravan);
		using ScopeWindow sw = new(splitCaravanDlg);
		splitCaravanDlg.PreOpen();
		splitCaravanDlg.PostOpen();

		List<TransferableOneWay> transferables = TransferablesFieldRef.Invoke(splitCaravanDlg);
		for (int i = 0; i < transferables.Count; i++)
		{
			TransferableOneWay transferable = transferables[i];
			transferable.AdjustTo(i % 2 == 0 ? transferable.GetMaximumToTransfer() : 0);
		}
		SplitCaravansMethod.Invoke(splitCaravanDlg, null);
		splitCaravanDlg.Close();

		List<Caravan> caravans = [];
		Find.WorldObjects.GetPlayerControlledCaravansAt(1, caravans);
		Assert.IsFalse(caravan.Destroyed);
		Assert.AreEqual(caravans.Count, 2);

		Caravan otherCaravan = caravans.First(carvn => carvn != caravan);
		using ScopeWorldObject scopeOther = new(otherCaravan);
		foreach (TransferableOneWay transferable in transferables)
		{
			if (!transferable.HasAnyThing)
				continue;

			Caravan container = transferable.CountToTransfer > 0 ? otherCaravan : caravan;
			switch (transferable.AnyThing)
			{
				case Pawn pawn:
					Expect.IsTrue(container.ContainsPawn(pawn));
				break;
				case not null:
					Expect.IsTrue(container.AllThings.ContainsAllOf(transferable.things));
				break;
			}
		}
	}

	[Test]
	private void SplitIntoVehicleCaravans()
	{
		const int PawnCount = 5;

		Assert.IsFalse(CaravansAt(1));

		using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = PawnCount,
			destroyInventory = true
		});
		using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = PawnCount,
			destroyInventory = true
		});

		group1.BoardAll();
		group2.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group1.vehicle, group2.vehicle],
				Faction.OfPlayer, 1, true);
		using ScopeWorldObject scopeCaravan = new(caravan);
		group2.DisembarkOne();

		Dialog_SplitCaravan splitCaravanDlg = new(caravan);
		using ScopeWindow sw = new(splitCaravanDlg);
		splitCaravanDlg.PreOpen();
		splitCaravanDlg.PostOpen();

		bool transferVehicle = false;
		List<TransferableOneWay> transferables = TransferablesFieldRef.Invoke(splitCaravanDlg);
		for (int i = 0; i < transferables.Count; i++)
		{
			TransferableOneWay transferable = transferables[i];
			if (transferable.AnyThing is VehiclePawn)
			{
				// Ensure vehicles are distributed or we'll end up with mixed caravans
				transferVehicle = !transferVehicle;
				transferable.AdjustTo(transferVehicle ? transferable.GetMaximumToTransfer() : 0);
			}
			else
			{
				transferable.AdjustTo(i % 2 == 0 ? transferable.GetMaximumToTransfer() : 0);
			}
		}
		SplitCaravansMethod.Invoke(splitCaravanDlg, null);
		splitCaravanDlg.Close();

		List<Caravan> caravans = [];
		Find.WorldObjects.GetPlayerControlledCaravansAt(1, caravans);
		(Caravan ogCaravan, Caravan otherCaravan) = caravan == caravans[0] ?
			(caravans[0], caravans[1]) :
			(caravans[1], caravans[0]);
		using ScopeWorldObject scopeOther = new(otherCaravan);
		Assert.IsFalse(caravan.Destroyed);
		Assert.AreEqual(caravans.Count, 2);
		Assert.IsTrue(caravans.All(carvn => carvn is VehicleCaravan));
		Assert.IsFalse(ReferenceEquals(caravan, otherCaravan));
		Assert.IsTrue(ReferenceEquals(caravan, ogCaravan));
		foreach (TransferableOneWay transferable in transferables)
		{
			if (!transferable.HasAnyThing)
				continue;

			Caravan container = transferable.CountToTransfer > 0 ? otherCaravan : caravan;
			switch (transferable.AnyThing)
			{
				case Pawn pawn:
					Expect.IsTrue(container.ContainsPawn(pawn.InVehicle() ? pawn.GetVehicle() : pawn));
				break;
				case not null:
					Expect.IsTrue(container.AllThings.ContainsAllOf(transferable.things));
				break;
			}
		}
	}

	[Test]
	private void SplitIntoMixedCaravans()
	{
		const int PawnCount = 5;

		Assert.IsFalse(CaravansAt(1));

		using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = PawnCount,
			destroyInventory = true
		});
		using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = PawnCount,
			destroyInventory = true
		});

		group1.BoardAll();
		group2.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group1.vehicle, group2.vehicle],
				Faction.OfPlayer, 1, true);
		using ScopeWorldObject scopeCaravan = new(caravan);
		group2.DisembarkOne();

		Dialog_SplitCaravan splitCaravanDlg = new(caravan);
		using ScopeWindow sw = new(splitCaravanDlg);
		splitCaravanDlg.PreOpen();
		splitCaravanDlg.PostOpen();

		List<TransferableOneWay> transferables = TransferablesFieldRef.Invoke(splitCaravanDlg);
		for (int i = 0; i < transferables.Count; i++)
		{
			TransferableOneWay transferable = transferables[i];
			Assert.IsTrue(transferable.HasAnyThing);
			int count;
			if (transferable.AnyThing is VehiclePawn)
			{
				// All vehicles transfer to the same caravan
				count = transferable.GetMaximumToTransfer();
			}
			else if (CaravanInventoryUtility.GetOwnerOf(caravan, transferable.AnyThing) is { } pawn &&
				pawn.InVehicle())
			{
				count = 0;
			}
			else
			{
				count = i % 2 == 0 ? transferable.GetMaximumToTransfer() : 0;
			}
			transferable.AdjustTo(count);
		}
		SplitCaravansMethod.Invoke(splitCaravanDlg, null);
		splitCaravanDlg.Close();

		List<Caravan> caravans = [];
		Find.WorldObjects.GetPlayerControlledCaravansAt(1, caravans);
		(Caravan ogCaravan, Caravan otherCaravan) = caravan == caravans[0] ?
			(caravans[0], caravans[1]) :
			(caravans[1], caravans[0]);
		using ScopeWorldObject scopeOg = new(ogCaravan);
		using ScopeWorldObject scopeOther = new(otherCaravan);
		Assert.IsTrue(caravan.Destroyed);
		Assert.IsFalse(ReferenceEquals(caravan, otherCaravan));
		Assert.IsFalse(ReferenceEquals(caravan, ogCaravan));
		Assert.AreEqual(caravans.Count, 2);
		Assert.IsFalse(caravans.All(carvn => carvn is VehicleCaravan));
		// 1 VehicleCaravan, 1 vanilla Caravan
		Assert.IsTrue(ogCaravan is VehicleCaravan ^ otherCaravan is VehicleCaravan);
		foreach (TransferableOneWay transferable in transferables)
		{
			if (!transferable.HasAnyThing)
				continue;

			Caravan container = transferable.CountToTransfer > 0 ? otherCaravan : ogCaravan;
			switch (transferable.AnyThing)
			{
				case Pawn pawn:
					Expect.IsTrue(container.ContainsPawn(pawn.InVehicle() ? pawn.GetVehicle() : pawn));
				break;
				case not null:
					Expect.IsTrue(container.AllThings.ContainsAllOf(transferable.things));
				break;
			}
		}
	}

	[Test]
	private void MergeVanillaCaravans()
	{
		const int Caravans = 3;
		const int PawnsPerCaravan = 3;

		Assert.IsFalse(CaravansAt(1));

		List<Caravan> caravans = [];
		for (int i = 0; i < Caravans; i++)
		{
			List<Pawn> pawns = [];
			for (int j = 0; j < PawnsPerCaravan; j++)
			{
				Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
					Faction.OfPlayer, fixedBiologicalAge: 30));
				Assert.IsNotNull(colonist);
				Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
				pawns.Add(colonist);
			}
			caravans.Add(CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, 1, true));
		}
		MergeCaravansMethod.Invoke(null, [caravans]);

		Caravan mergedCaravan = Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOf.Caravan) as Caravan;
		Assert.IsNotNull(mergedCaravan);

		mergedCaravan.Destroy();
		Assert.IsTrue(mergedCaravan.Destroyed);
	}

	[Test]
	private void MergeVehicleCaravans()
	{
		Assert.IsFalse(CaravansAt(1));

		using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
		using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			animals = 1
		});
		using VehicleGroup group3 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 3
		});

		group1.BoardAll();
		group2.BoardAll();
		group3.BoardOne();

		VehicleCaravan vehicleCaravan1 =
			CaravanHelper.MakeVehicleCaravan([group1.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo1 = new(vehicleCaravan1);
		VehicleCaravan vehicleCaravan2 =
			CaravanHelper.MakeVehicleCaravan([group2.vehicle], Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo2 = new(vehicleCaravan2);
		VehicleCaravan vehicleCaravan3 =
			CaravanHelper.MakeVehicleCaravan(
				group3.pawns.Where(pawn => !pawn.InVehicle()).Concat(group3.vehicle), Faction.OfPlayer, 1,
				true);
		using ScopeWorldObject swo3 = new(vehicleCaravan3);
		List<Caravan> caravanList = [vehicleCaravan1, vehicleCaravan2, vehicleCaravan3];
		MergeCaravansMethod.Invoke(null, [caravanList]);

		VehicleCaravan mergedCaravan =
			Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOfVehicles.VehicleCaravan) as VehicleCaravan;
		Assert.IsNotNull(mergedCaravan);
		using ScopeWorldObject swoMerged = new(mergedCaravan);
		Expect.AreEqual(mergedCaravan.PawnsListForReading.Count,
			group1.pawns.Count + group2.pawns.Count + group3.pawns.Count + 3 /* for vehicles */);
		// Caravan3 was used as merge target since it has the most pawns
		Expect.IsTrue(vehicleCaravan1.Destroyed);
		Expect.IsTrue(vehicleCaravan2.Destroyed);
		Expect.IsFalse(vehicleCaravan3.Destroyed);
		Expect.ReferencesAreEqual(mergedCaravan, vehicleCaravan3);
	}

	[Test]
	private void MergeMixedCaravans()
	{
		const int PawnsInVanillaCaravan = 3;

		Assert.IsFalse(CaravansAt(1));

		using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});
		using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 3
		});

		// Vanilla
		List<Pawn> pawns = [];
		for (int j = 0; j < PawnsInVanillaCaravan; j++)
		{
			Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
				Faction.OfPlayer, fixedBiologicalAge: 30));
			Assert.IsNotNull(colonist);
			Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
			pawns.Add(colonist);
		}
		Caravan caravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, 1, true);
		using ScopeWorldObject swo = new(caravan);

		// All boarded
		group1.BoardAll();
		VehicleCaravan vehicleCaravan1 =
			CaravanHelper.MakeVehicleCaravan([group1.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(vehicleCaravan1);
		using ScopeWorldObject swo1 = new(vehicleCaravan1);
		Expect.AreEqual(vehicleCaravan1.PawnsListForReading.Count, group1.pawns.Count + 1);

		// Some Boarded, some disembarked
		group2.BoardOne();
		VehicleCaravan vehicleCaravan2 =
			CaravanHelper.MakeVehicleCaravan([group2.vehicle, .. group2.pawns], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(vehicleCaravan2);
		using ScopeWorldObject swo2 = new(vehicleCaravan2);
		Expect.AreEqual(vehicleCaravan2.PawnsListForReading.Count, group2.pawns.Count + 1);

		// Verify merge chooses caravan with the most pawns and correctly merges into VehicleCaravan object
		Expect.IsTrue(vehicleCaravan1.PawnsListForReading.Count < vehicleCaravan2.PawnsListForReading.Count);
		List<Caravan> caravanList = [vehicleCaravan1, vehicleCaravan2, caravan];
		MergeCaravansMethod.Invoke(null, [caravanList]);

		VehicleCaravan mergedCaravan =
			Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOfVehicles.VehicleCaravan) as VehicleCaravan;
		Assert.IsNotNull(mergedCaravan);

		Expect.AreEqual(mergedCaravan.PawnsListForReading.Count,
			group1.pawns.Count + group2.pawns.Count + PawnsInVanillaCaravan + 2 /* for vehicles */);
		// Caravan2 was used as merge target since it has the most pawns
		Expect.IsTrue(caravan.Destroyed);
		Expect.IsTrue(vehicleCaravan1.Destroyed);
		Expect.IsFalse(vehicleCaravan2.Destroyed);
		Expect.ReferencesAreEqual(mergedCaravan, vehicleCaravan2);
	}
}