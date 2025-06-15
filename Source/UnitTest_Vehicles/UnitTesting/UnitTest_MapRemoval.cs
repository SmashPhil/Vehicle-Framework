using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;
using Priority = DevTools.UnitTesting.Priority;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePermissions)]
[TestDescription("Maps account for vehicles when checking removal conditions.")]
internal sealed class UnitTest_MapRemoval
{
  private static readonly IntVec3 DefaultMapSize = new(50, 1, 50);

  private VehicleGroup manualVehicle;
  private VehicleGroup autonomousVehicle;
  private VehicleGroup aerialVehicle;

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
    aerialVehicle = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Autonomous | VehiclePermissions.Immobile,
      passengers = 1,
      comps =
      [
        new CompProperties_VehicleLauncher
        {
          compClass = typeof(CompVehicleLauncher),
          launchProtocol = new DefaultTakeoff()
        }
      ]
    });
  }

  [TearDown, ExecutionPriority(Priority.BelowNormal)]
  private void DestroyAll()
  {
    manualVehicle.Dispose();
    autonomousVehicle.Dispose();
    aerialVehicle.Dispose();

    TestUtils.EmptyWorldAndMapOfVehicles();
  }

  [TearDown]
  private void RefocusCamera()
  {
    Map map = Find.Maps.FirstOrDefault();
    if (map != null)
      CameraJumper.TryJump(map.Center, map);
  }

  private static PlanetTile FindValidTile(PlanetLayerDef layerDef)
  {
    PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
    return TileFinder.RandomSettlementTileFor(layer, Faction.OfPirates,
      extraValidator: ValidObjectTile);

    bool ValidObjectTile(PlanetTile tile)
    {
      return !Find.WorldObjects.AnyWorldObjectAt(tile);
    }
  }

  /// <summary>
  /// Player settlements should never be removed
  /// </summary>
  [Test]
  private void Settlement()
  {
    using GenStepWarningDisabler warningDisabler = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      Settlement settlement =
        (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
      settlement.Tile = tile;
      settlement.SetFaction(Faction.OfPlayer);
      Find.WorldObjects.Add(settlement);
      map = MapGenerator.GenerateMap(DefaultMapSize, settlement, settlement.MapGeneratorDef);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      aerialVehicle.DisembarkAll();
      aerialVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      autonomousVehicle.DeSpawn();
      Assert.IsFalse(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous aerial vehicle with passengers
      aerialVehicle.Spawn();
      Assert.IsTrue(aerialVehicle.vehicle.Spawned);
      aerialVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      aerialVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Autonomous aerial vehicle no passengers CanMove
      aerialVehicle.DisembarkAll();
      aerialVehicle.DeSpawnPawns();
      Assert.IsTrue(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.CanMove);
      Assert.IsTrue(aerialVehicle.vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out _));
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      aerialVehicle.DeSpawn();
      Assert.IsFalse(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }

  /// <summary>
  /// Only hold site open while a conscious player-controlled pawn exists or if an autonomous
  /// vehicle is still on the map.
  /// </summary>
  [Test]
  private void Site()
  {
    using GenStepWarningDisabler gswd = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
        WorldObjectDefOf.Site);
      Site site = map.Parent as Site;
      Assert.IsNotNull(site);
      Assert.IsFalse(map.Disposed);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsTrue(site.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));

      // Autonomous vehicle with passengers
      aerialVehicle.Spawn();
      Assert.IsTrue(aerialVehicle.vehicle.Spawned);
      aerialVehicle.DisembarkAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));
      aerialVehicle.BoardAll();
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      aerialVehicle.DisembarkAll();
      aerialVehicle.DeSpawnPawns();
      Assert.IsTrue(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.CanMove);
      Expect.IsFalse(site.ShouldRemoveMapNow(out _));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }

  /// <summary>
  /// Only hold camp open while a conscious player-controlled pawn exists or if an autonomous
  /// vehicle is still on the map.
  /// </summary>
  [Test]
  private void Camp()
  {
    using GenStepWarningDisabler gswd = new();
    using PawnAnchorer anchorer = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
        WorldObjectDefOf.Camp);
      map.Parent.SetFaction(Faction.OfPlayer);
      Camp camp = map.Parent as Camp;
      Assert.IsNotNull(camp);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsTrue(camp.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(camp.ShouldRemoveMapNow(out _));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      WorldObject dummyCamp =
        Find.WorldObjects.WorldObjectAt(tile, WorldObjectDefOf.AbandonedCamp);
      dummyCamp?.Destroy();
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }

  [Test]
  private void CaravansBattlefield()
  {
    using GenStepWarningDisabler gswd = new();
    using PawnAnchorer anchorer = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
        WorldObjectDefOf.AttackedNonPlayerCaravan);
      CaravansBattlefield battlefield = map.Parent as CaravansBattlefield;
      Assert.IsNotNull(battlefield);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsTrue(battlefield.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(battlefield.ShouldRemoveMapNow(out _));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }

  [Test]
  private void DestroyedSettlement()
  {
    using GenStepWarningDisabler gswd = new();
    using PawnAnchorer anchorer = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
        WorldObjectDefOf.DestroyedSettlement);
      DestroyedSettlement settlement = map.Parent as DestroyedSettlement;
      Assert.IsNotNull(settlement);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsTrue(settlement.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(settlement.ShouldRemoveMapNow(out _));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }

  [Test]
  private void SpaceMapParent()
  {
    using GenStepWarningDisabler gswd = new();
    using PawnAnchorer anchorer = new();

    Assert.IsFalse(manualVehicle.vehicle.Spawned);
    Assert.IsFalse(autonomousVehicle.vehicle.Spawned);

    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = null;
    try
    {
      WorldObjectDef asteroidObjectDef =
        DefDatabase<WorldObjectDef>.GetNamed("AsteroidMiningSite");
      Assert.IsNotNull(asteroidObjectDef);
      map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize, asteroidObjectDef);
      // Ensure we're testing a derivative of the type that actually implements a check for map removal
      Assert.IsTrue(map.Parent is SpaceMapParent);
      ResourceAsteroidMapParent asteroid = map.Parent as ResourceAsteroidMapParent;
      Assert.IsNotNull(asteroid);
      CameraJumper.TryJump(map.Center, map);

      // Manual vehicle with passengers
      manualVehicle.Spawn();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      manualVehicle.DisembarkAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));
      manualVehicle.BoardAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));

      // Manual vehicle no passengers
      manualVehicle.DisembarkAll();
      manualVehicle.DeSpawnPawns();
      Assert.IsTrue(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Expect.IsTrue(asteroid.ShouldRemoveMapNow(out _));

      manualVehicle.BoardAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));

      manualVehicle.DeSpawn();
      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));

      // Autonomous vehicle with passengers
      autonomousVehicle.Spawn();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      autonomousVehicle.DisembarkAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));
      autonomousVehicle.BoardAll();
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));

      // Autonomous vehicle no passengers CanMove
      autonomousVehicle.DisembarkAll();
      autonomousVehicle.DeSpawnPawns();
      Assert.IsTrue(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsTrue(autonomousVehicle.vehicle.CanMove);
      Expect.IsFalse(asteroid.ShouldRemoveMapNow(out _));
    }
    finally
    {
      manualVehicle.DeSpawn();
      autonomousVehicle.DeSpawn();
      aerialVehicle.DeSpawn();

      Assert.IsFalse(manualVehicle.vehicle.Spawned);
      Assert.IsFalse(manualVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(autonomousVehicle.vehicle.Spawned);
      Assert.IsFalse(autonomousVehicle.pawns.Any(pawn => pawn.Spawned));
      Assert.IsFalse(aerialVehicle.vehicle.Spawned);
      Assert.IsFalse(aerialVehicle.pawns.Any(pawn => pawn.Spawned));

      if (map is { Disposed: false })
      {
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
      }
      Assert.IsFalse(map is { Disposed: false });
      Assert.IsFalse(map?.Parent is { Destroyed: false });
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
    }
  }
}