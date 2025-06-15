using System;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using Priority = DevTools.UnitTesting.Priority;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestDescription("Vehicles with passengers are checked for game ending conditions.")]
internal sealed class UnitTest_GameEnder
{
  private VehicleGroup manualVehicle;
  private VehicleGroup autonomousVehicle;

  [SetUp]
  private void GenerateVehicle()
  {
    manualVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.DriverNeeded,
      drivers = 1,
      passengers = 1
    });
    autonomousVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Autonomous,
      passengers = 1
    });
  }

  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void DestroyAll()
  {
    manualVehicle.Dispose();
    autonomousVehicle.Dispose();

    TestUtils.EmptyWorldAndMapOfVehicles();
  }

  [Test]
  private void GameOverCondition()
  {
    using PawnAnchorer anchorer = new();

    const int GameTicksBuffer = 300;

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    // GameEnder only applies after first 300 ticks to allow starter pods to land
    using MockGameTicks gameTicks = new(GameTicksBuffer);
    Game game = Current.Game;
    Assert.IsNotNull(game);
    GameEnder gameEnder = game.gameEnder;
    Assert.IsNotNull(gameEnder);
    Assert.IsFalse(gameEnder.gameEnding);

    // Go through and make sure all game ending disablers are invalid before we start testing if
    // vehicles cause game ending events.
    Assert.IsTrue(Find.TickManager.TicksGame >= 300);
    Assert.IsFalse(ShipCountdown.CountingDown);
    Assert.IsTrue(!ModsConfig.OdysseyActive ||
      !WorldComponent_GravshipController.CutsceneInProgress);
    Assert.IsNull(Find.CurrentGravship);
    Assert.IsTrue(
      !ModsConfig.AnomalyActive || !DeathRefusalUtility.PlayerHasCorpseWithDeathRefusal());
    Assert.IsTrue(Find.WorldObjects.CaravansCount == 0);
    Assert.IsTrue(Find.WorldObjects.TravellingTransporters.Count == 0);
    Assert.IsTrue(QuestUtility.TotalBorrowedColonistCount() == 0);

    // Kill everything
    foreach (Map map in Find.Maps)
    {
      for (int i = map.mapPawns.AllPawnsSpawned.Count - 1; i >= 0; i--)
      {
        Pawn pawn = map.mapPawns.AllPawnsSpawned[i];
        if (pawn.carryTracker is { CarriedThing: Pawn { IsFreeColonist: true } })
          pawn.carryTracker.DestroyCarriedThing();
        pawn.Destroy();
      }

      if (ModsConfig.AnomalyActive)
      {
        for (int i = map.mapPawns.AllPawnsUnspawned.Count - 1; i >= 0; i--)
        {
          Pawn pawn = map.mapPawns.AllPawnsUnspawned[i];
          if (pawn is { IsColonist: true, HostFaction: null, ParentHolder: CompDevourer })
            pawn.Destroy();
        }
      }
    }
    using (new GameEnderBlock(gameEnder))
    {
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsTrue(gameEnder.gameEnding);
    }

    manualVehicle.Spawn();
    Assert.IsTrue(manualVehicle.vehicle.Spawned);

    // Vehicle spawned with pawns on map
    using (new GameEnderBlock(gameEnder))
    {
      manualVehicle.DisembarkAll();
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsFalse(gameEnder.gameEnding);
    }

    // Vehicle spawned with no pawns in map, has passengers
    using (new GameEnderBlock(gameEnder))
    {
      manualVehicle.BoardAll();
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsFalse(gameEnder.gameEnding);
    }

    using (new GameEnderBlock(gameEnder))
    {
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsTrue(gameEnder.gameEnding);
    }

    manualVehicle.DeSpawn();
    Assert.IsFalse(manualVehicle.vehicle.Spawned);

    // Autonomous Vehicle spawned with no pawns in map, no passengers
    using (new GameEnderBlock(gameEnder))
    {
      autonomousVehicle.Spawn();
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsTrue(autonomousVehicle.vehicle.AllPawnsAboard.Count == 0);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsTrue(gameEnder.gameEnding);

      autonomousVehicle.BoardAll();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsTrue(autonomousVehicle.vehicle.AllPawnsAboard.Count ==
        autonomousVehicle.pawns.Count);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsFalse(gameEnder.gameEnding);

      autonomousVehicle.DeSpawn();
      gameEnder.CheckOrUpdateGameOver();
      Assert.IsTrue(gameEnder.gameEnding);
    }

    // Vehicle in caravan with passengers
    using (new GameEnderBlock(gameEnder))
    {
      manualVehicle.BoardAll();
      VehicleCaravan caravan =
        CaravanHelper.MakeVehicleCaravan([manualVehicle.vehicle], Faction.OfPlayer, 0, true);
      Assert.IsTrue(caravan.Spawned);
      Assert.IsFalse(caravan.Destroyed);
      Assert.AreEqual(caravan.PawnsListForReading.Count, manualVehicle.pawns.Count + 1);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsFalse(gameEnder.gameEnding);

      caravan.RemoveAllPawns();
      Assert.IsTrue(caravan.pawns.InnerListForReading.NullOrEmpty());
      Assert.IsTrue(caravan.PawnsListForReading.NullOrEmpty());
      Assert.IsTrue(caravan.Vehicles.NullOrEmpty());
      Assert.IsTrue(caravan.Destroyed);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsTrue(gameEnder.gameEnding);
    }

    // Aerial vehicle with passengers
    using (new GameEnderBlock(gameEnder))
    {
      manualVehicle.BoardAll();
      AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(manualVehicle.vehicle, 0);
      Assert.IsTrue(aerialVehicle.Spawned);
      Assert.IsFalse(aerialVehicle.Destroyed);
      Assert.AreEqual(aerialVehicle.vehicle.AllPawnsAboard.Count, manualVehicle.pawns.Count);
      Assert.IsNotNull(aerialVehicle);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsFalse(gameEnder.gameEnding);

      aerialVehicle.vehicle = null;
      aerialVehicle.innerContainer.Clear();
      Assert.IsNull(aerialVehicle.vehicle);
      Assert.IsTrue(aerialVehicle.innerContainer.Count == 0);
      aerialVehicle.Destroy();
      Assert.IsTrue(aerialVehicle.Destroyed);
      gameEnder.CheckOrUpdateGameOver();
      Expect.IsTrue(gameEnder.gameEnding);
    }
  }

  private readonly struct GameEnderBlock : IDisposable
  {
    private static readonly FieldInfo ticksToGameOverField;

    private readonly GameEnder gameEnder;

    static GameEnderBlock()
    {
      ticksToGameOverField = AccessTools.Field(typeof(GameEnder), "ticksToGameOver");
      Assert.IsNotNull(ticksToGameOverField);
    }

    public GameEnderBlock(GameEnder gameEnder)
    {
      this.gameEnder = gameEnder;
      gameEnder.gameEnding = false;
      ticksToGameOverField.SetValue(gameEnder, 0);
    }

    void IDisposable.Dispose()
    {
      gameEnder.gameEnding = false;
      ticksToGameOverField.SetValue(gameEnder, 0);
    }
  }
}