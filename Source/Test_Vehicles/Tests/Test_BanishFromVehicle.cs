using System;
using System.Collections.Generic;
using System.Text;
using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using Vehicles.World;
using Verse;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.WorldObject,
  TestCategoryNames.Caravaning,
  TestCategoryNames.VehiclePawn
)]
[TestDescription("Pawns banished while inside a vehicle properly disembark and banish like base game behavior.")]
internal class Test_BanishFromVehicle
{
  [Test]
  private void BanishVehicleSpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1
    });
    Pawn banishee = group.pawns[0];
    group.Spawn();
    PawnBanishUtility.Banish(banishee, giveThoughts: false);
    Expect.AreNotEqual(Faction.OfPlayer, banishee.Faction);
    Expect.IsFalse(banishee.InVehicle());
    Expect.IsTrue(banishee.Spawned);
  }

  [Test]
  private void BanishVehicleUnspawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1
    });
    Pawn banishee = group.pawns[0];
    group.BoardAll();
    PawnBanishUtility.Banish(banishee, giveThoughts: false);
    Expect.AreNotEqual(Faction.OfPlayer, banishee.Faction);
    Expect.IsFalse(banishee.InVehicle());
    Expect.IsFalse(banishee.Spawned);
  }

  [Test]
  private void BanishInVehicleCaravan()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1
    });
    Pawn banishee = group.pawns[0];
    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1, addToWorldPawnsIfNotAlready: true);
    using ScopeWorldObject swo = new(vehicleCaravan);
    PawnBanishUtility.Banish(banishee, giveThoughts: false);
    Expect.AreNotEqual(Faction.OfPlayer, banishee.Faction);
    Expect.IsFalse(banishee.InVehicle());
    Expect.IsFalse(banishee.Spawned);
    Expect.IsTrue(banishee.IsWorldPawn());
  }

  [Test]
  private void BanishInAerialVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      passengers = 1
    });
    Pawn banishee = group.pawns[0];
    group.BoardAll();
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    using ScopeWorldObject swo = new(aerialVehicle);
    PawnBanishUtility.Banish(banishee, giveThoughts: false);
    Expect.AreNotEqual(Faction.OfPlayer, banishee.Faction);
    Expect.IsFalse(banishee.InVehicle());
    Expect.IsFalse(banishee.Spawned);
    Expect.IsTrue(banishee.IsWorldPawn());
  }
}
