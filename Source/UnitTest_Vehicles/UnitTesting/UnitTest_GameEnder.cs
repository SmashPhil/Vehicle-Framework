using System;
using System.Collections;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Priority = DevTools.Testing.Priority;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestDescription("Vehicles with passengers are checked for game ending conditions.")]
internal sealed class UnitTest_GameEnder
{
	// GameEnder only applies after first 300 ticks to allow starter pods to land
	private const int GameTicksBuffer = 300;

	private PawnAnchorer anchorer;

	[SetUp, ExecutionPriority(Priority.First)]
	private void KillEverything()
	{
		anchorer = new PawnAnchorer();
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
	}

	[SetUp]
	private void GameInit()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		// Go through and make sure all game ending disablers are invalid before we start testing if
		// vehicles cause game ending events.
		Assert.IsTrue(Find.TickManager.TicksGame >= GameTicksBuffer);
		Assert.IsFalse(ShipCountdown.CountingDown);
		Assert.IsTrue(!ModsConfig.OdysseyActive ||
			!WorldComponent_GravshipController.CutsceneInProgress);
		Assert.IsNull(Find.CurrentGravship);
		Assert.IsTrue(
			!ModsConfig.AnomalyActive || !DeathRefusalUtility.PlayerHasCorpseWithDeathRefusal());
		Assert.IsTrue(Find.WorldObjects.CaravansCount == 0);
		Assert.IsTrue(Find.WorldObjects.TravellingTransporters.Count == 0);
		Assert.IsTrue(QuestUtility.TotalBorrowedColonistCount() == 0);
		Game game = Current.Game;
		Assert.IsNotNull(game);
		GameEnder gameEnder = game.gameEnder;
		Assert.IsNotNull(gameEnder);
		Assert.IsFalse(gameEnder.gameEnding);

		using (new GameEnderBlock(gameEnder))
		{
			gameEnder.CheckOrUpdateGameOver();
			Expect.IsTrue(gameEnder.gameEnding);
		}
	}

	[TearDown]
	private void SpawnAnchorPawn()
	{
		anchorer?.Dispose();
		anchorer = null;
	}

	[Test]
	[TestDescription("Verify spawning vehicle automatically checks game ender condition.")]
	private void Spawn()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});

		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);

		group.Spawn();
		Expect.IsFalse(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify despawning vehicle automatically checks game ender condition.")]
	private void Destroy()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		VehicleGroup.MockSettings settings = new()
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		};
		using (VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(settings))
		{
			group.Spawn();
			gameEnder.CheckOrUpdateGameOver();
			Expect.IsFalse(gameEnder.gameEnding);
		}
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify vehicle with passengers prevents game ender event.")]
	private void Manual()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});
		group.Spawn();

		// Vehicle spawned with pawns on map
		group.DisembarkAll();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		// Vehicle spawned with no pawns in map, has passengers
		group.BoardAll();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		// Vehicle spawned with no pawns in map, no passengers
		group.DisembarkAll();
		group.DeSpawnPawns();
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify autonomous vehicles do not prevent game ender event if no pawns are onboard.")]
	private void Autonomous()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Autonomous,
			passengers = 1
		});

		// Autonomous vehicle spawned with no pawns in map, no passengers
		group.Spawn();
		group.DisembarkAll();
		group.DeSpawnPawns();
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.IsTrue(group.vehicle.AllPawnsAboard.Count == 0);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);

		// Autonomous vehicle spawned with passengers
		group.BoardAll();
		Assert.IsTrue(group.vehicle.Spawned);
		Assert.IsTrue(group.vehicle.AllPawnsAboard.Count == group.pawns.Count);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		// Autonomous vehicle doesn't prevent game ender globally
		group.DeSpawn();
		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify vehicle caravan prevents game ender event.")]
	private void Caravan()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});

		// Vehicle in caravan with passengers
		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);
		Assert.IsTrue(caravan.Spawned);
		Assert.IsFalse(caravan.Destroyed);
		Assert.AreEqual(caravan.PawnsListForReading.Count, group.pawns.Count + 1);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		// Empty vehicle caravan
		caravan.RemoveAllPawns();
		Assert.IsTrue(caravan.pawns.InnerListForReading.NullOrEmpty());
		Assert.IsTrue(caravan.PawnsListForReading.Count == 0);
		Assert.IsTrue(caravan.Vehicles.NullOrEmpty());
		Assert.IsTrue(caravan.Destroyed);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify aerial vehicle prevents game ender event.")]
	private void AerialVehicle()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});

		CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
		Assert.IsNotNull(compLauncher);
		// Aerial vehicle with passengers
		group.BoardAll();
		AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 0);
		aerialVehicle.OrderFlyToTiles([new FlightNode(0), new FlightNode(1)],
			new ArrivalAction_LandToCaravan(group.vehicle));
		Assert.IsNotNull(aerialVehicle);
		using ScopeWorldObject swo = new(aerialVehicle);
		Assert.IsTrue(aerialVehicle.Spawned);
		Assert.IsFalse(aerialVehicle.Destroyed);
		Assert.IsTrue(compLauncher.inFlight);
		Assert.AreEqual(aerialVehicle.Vehicle.AllPawnsAboard.Count, group.pawns.Count);

		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		aerialVehicle.ClearAndDestroy();
		Assert.IsTrue(aerialVehicle.Destroyed);
		Expect.IsNull(aerialVehicle.Vehicle);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}


	[Test]
	[TestDescription("Verify outgoing skyfaller prevents game ender event.")]
	private void VehicleSkyfaller_Leaving()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		group.BoardAll();
		VehicleSkyfaller_Leaving skyfaller =
			(VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(
				group.vehicle.CompVehicleLauncher.Props.skyfallerLeaving, group.vehicle);
		Assert.IsNotNull(skyfaller);
		using ScopeEntity se = new(skyfaller);
		GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		skyfaller.Destroy();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify incoming skyfaller prevents game ender event.")]
	private void VehicleSkyfaller_Arriving()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		group.BoardAll();
		VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(
			group.vehicle.CompVehicleLauncher.Props.skyfallerIncoming, group.vehicle);
		Assert.IsNotNull(skyfaller);
		using ScopeEntity se = new(skyfaller);
		GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		skyfaller.Destroy();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify crashing skyfaller prevents game ender.")]
	private void VehicleSkyfaller_Crashing()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		group.BoardAll();
		VehicleSkyfaller_Crashing skyfaller =
			(VehicleSkyfaller_Crashing)VehicleSkyfallerMaker.MakeSkyfaller(
				group.vehicle.CompVehicleLauncher.Props.skyfallerCrashing, group.vehicle);
		Assert.IsNotNull(skyfaller);
		using ScopeEntity se = new(skyfaller);
		GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);

		skyfaller.Destroy();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify landing targeter disables game over condition.")]
	private void TargetedLanding()
	{
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		group.BoardAll();
		Assert.IsFalse(group.vehicle.Spawned);

		LandingTargeter.Instance.BeginTargeting(group.vehicle, action: NoOpTargeterAction);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);
		LandingTargeter.Instance.StopTargeting();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
		return;

		// NOTE - Old Targeter system determines active state with non-null action, we must provide one.
		static void NoOpTargeterAction(LocalTargetInfo t, Rot4 r)
		{
		}
	}

	[Test]
	[TestDescription("Verify transition from map generation to landing targeter catches game ender events.")]
	private IEnumerator TargetedLandingGenerated()
	{
		using GenStepWarningDisabler gswd = new();
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
		Assert.IsNotNull(compLauncher);
		// Aerial vehicle with passengers
		group.BoardAll();

		Faction enemyFaction =
			Find.FactionManager.AllFactionsListForReading.FirstOrDefault(faction => faction.HostileTo(Faction.OfPlayer));
		PlanetTile baseTile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface, enemyFaction);
		Settlement enemySettlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
		Assert.IsNotNull(enemySettlement);
		using ScopeWorldObject ses = new(enemySettlement);
		enemySettlement.Tile = baseTile;
		enemySettlement.SetFaction(enemyFaction);
		Find.WorldObjects.Add(enemySettlement);
		Assert.IsNotNull(Find.WorldObjects.MapParentAt(baseTile));
		AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 0);
		aerialVehicle.OrderFlyToTiles([new FlightNode(enemySettlement)],
			new ArrivalAction_AttackSettlement(group.vehicle, AerialVehicleArrivalModeDefOf.TargetedLanding));
		Assert.IsNotNull(aerialVehicle);
		using ScopeWorldObject swo = new(aerialVehicle);
		Assert.IsTrue(aerialVehicle.Spawned);
		Assert.IsFalse(aerialVehicle.Destroyed);
		Assert.IsTrue(compLauncher.inFlight);
		Assert.AreEqual(aerialVehicle.Vehicle.AllPawnsAboard.Count, group.pawns.Count);
		aerialVehicle.ArriveAtTile(baseTile);
		aerialVehicle.flightPath.ConsumeNode();

		// Wait for map to finish generating
		while (LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState == ProgramState.MapInitializing)
			yield return null;

		// Check status immediately after map gen, Pawn spawning will trigger checks before targeter starts
		Expect.IsFalse(gameEnder.gameEnding);
		Assert.IsTrue(aerialVehicle.Destroyed);
		Assert.IsTrue(LandingTargeter.Instance.IsTargeting);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);
		LandingTargeter.Instance.StopTargeting();
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsTrue(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify edge landing skyfaller updates game ender during map generation.")]
	private IEnumerator EdgeLandingGenerated()
	{
		using GenStepWarningDisabler gswd = new();
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
		Assert.IsNotNull(compLauncher);
		// Aerial vehicle with passengers
		group.BoardAll();

		Faction enemyFaction =
			Find.FactionManager.AllFactionsListForReading.FirstOrDefault(faction => faction.HostileTo(Faction.OfPlayer));
		PlanetTile baseTile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface, enemyFaction);
		Settlement enemySettlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
		Assert.IsNotNull(enemySettlement);
		using ScopeWorldObject ses = new(enemySettlement);
		enemySettlement.Tile = baseTile;
		enemySettlement.SetFaction(enemyFaction);
		Find.WorldObjects.Add(enemySettlement);
		Assert.IsNotNull(Find.WorldObjects.MapParentAt(baseTile));
		AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 0);
		aerialVehicle.OrderFlyToTiles([new FlightNode(enemySettlement)],
			new ArrivalAction_AttackSettlement(group.vehicle, AerialVehicleArrivalModeDefOf.EdgeDrop));
		Assert.IsNotNull(aerialVehicle);
		using ScopeWorldObject swo = new(aerialVehicle);
		Assert.IsTrue(aerialVehicle.Spawned);
		Assert.IsFalse(aerialVehicle.Destroyed);
		Assert.IsTrue(compLauncher.inFlight);
		Assert.AreEqual(aerialVehicle.Vehicle.AllPawnsAboard.Count, group.pawns.Count);
		aerialVehicle.ArriveAtTile(baseTile);
		aerialVehicle.flightPath.ConsumeNode();

		// Wait for map to finish generating
		while (LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState == ProgramState.MapInitializing)
			yield return null;

		// Check status immediately after map gen, Pawn spawning will trigger checks before targeter starts
		Expect.IsFalse(gameEnder.gameEnding);
		Assert.IsTrue(aerialVehicle.Destroyed);
		Assert.IsFalse(LandingTargeter.Instance.IsTargeting);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);
	}

	[Test]
	[TestDescription("Verify center landing skyfaller updates game ender during map generation.")]
	private IEnumerator CenterLandingGenerated()
	{
		using GenStepWarningDisabler gswd = new();
		using MockGameTicks gameTicks = new(GameTicksBuffer);
		GameEnder gameEnder = Current.Game.gameEnder;
		using GameEnderBlock endBlock = new(gameEnder);

		gameEnder.CheckOrUpdateGameOver();
		Assert.IsTrue(gameEnder.gameEnding);

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			comps = [CompGenerator.CompPropertiesVehicleLauncher]
		});
		CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
		Assert.IsNotNull(compLauncher);
		// Aerial vehicle with passengers
		group.BoardAll();

		Faction enemyFaction =
			Find.FactionManager.AllFactionsListForReading.FirstOrDefault(faction => faction.HostileTo(Faction.OfPlayer));
		PlanetTile baseTile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface, enemyFaction);
		Settlement enemySettlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
		Assert.IsNotNull(enemySettlement);
		using ScopeWorldObject ses = new(enemySettlement);
		enemySettlement.Tile = baseTile;
		enemySettlement.SetFaction(enemyFaction);
		Find.WorldObjects.Add(enemySettlement);
		Assert.IsNotNull(Find.WorldObjects.MapParentAt(baseTile));
		AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 0);
		aerialVehicle.OrderFlyToTiles([new FlightNode(enemySettlement)],
			new ArrivalAction_AttackSettlement(group.vehicle, AerialVehicleArrivalModeDefOf.CenterDrop));
		Assert.IsNotNull(aerialVehicle);
		using ScopeWorldObject swo = new(aerialVehicle);
		Assert.IsTrue(aerialVehicle.Spawned);
		Assert.IsFalse(aerialVehicle.Destroyed);
		Assert.IsTrue(compLauncher.inFlight);
		Assert.AreEqual(aerialVehicle.Vehicle.AllPawnsAboard.Count, group.pawns.Count);
		aerialVehicle.ArriveAtTile(baseTile);
		aerialVehicle.flightPath.ConsumeNode();

		// Wait for map to finish generating
		while (LongEventHandler.AnyEventNowOrWaiting || Current.ProgramState == ProgramState.MapInitializing)
			yield return null;

		// Check status immediately after map gen, Pawn spawning will trigger checks before targeter starts
		Expect.IsFalse(gameEnder.gameEnding);
		Assert.IsTrue(aerialVehicle.Destroyed);
		Assert.IsFalse(LandingTargeter.Instance.IsTargeting);
		gameEnder.CheckOrUpdateGameOver();
		Expect.IsFalse(gameEnder.gameEnding);
	}

	private readonly struct GameEnderBlock : IDisposable
	{
		private static readonly FieldInfo TicksToGameOverField;

		private readonly GameEnder gameEnder;

		static GameEnderBlock()
		{
			TicksToGameOverField = AccessTools.Field(typeof(GameEnder), "ticksToGameOver");
			Assert.IsNotNull(TicksToGameOverField);
		}

		public GameEnderBlock(GameEnder gameEnder)
		{
			this.gameEnder = gameEnder;
			gameEnder.gameEnding = false;
			TicksToGameOverField.SetValue(gameEnder, 0);
		}

		void IDisposable.Dispose()
		{
			gameEnder.gameEnding = false;
			TicksToGameOverField.SetValue(gameEnder, 0);
		}
	}
}