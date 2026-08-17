using System.Collections.Generic;
using System.Linq;
using DevTools;
using DevTools.Testing;
using LudeonTK;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn)]
[TestDescription("MapPawns properly fetches pawns in vehicles.")]
internal sealed class Test_MapPawns
{
  private Map map;

  [OneTimeSetUp, ExecutionPriority(Priority.First)]
  private void KillEverything()
  {
    map = Find.CurrentMap;
    Assert.IsTrue(Find.Maps.Count == 1);
    Assert.IsTrue(map.IsPlayerHome, "Unable to kill everything on non-player settlement, map will be removed.");
    MapUtils.KillEverything(map);
  }

  [SetUp]
  private void VerifyNoPawnsOnMap()
  {
    Assert.IsFalse(map.spawnedThings.Any(thing => thing is Pawn));
  }

  // RimWorld uses game init data for initial game states, but after Game.InitNewGame it resets it to null.
  // We have to reinit this property for transient faction generation.
  [SetUp]
  private void CreateGameInitData()
  {
    Current.Game.InitData = new GameInitData();
  }

  [TearDown]
  private void ClearGameInitData()
  {
    Current.Game.InitData = null;
  }

  [Test]
  [TestDescription("Verify Humanlike pawns list correctly from MapPawns.get_AllHumanlike when inside a vehicle on the map.")]
  private void AllHumanlike()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.AllHumanlike.Count);
    group.Spawn();
    Expect.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 1, map.mapPawns.AllHumanlike.Count);
  }

  [Test]
  private void AllHumanlikeSpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.AllHumanlikeSpawned.Count);
    group.Spawn();
    Expect.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 0, map.mapPawns.AllHumanlikeSpawned.Count);
  }

  [Test]
  private void AllPawns()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.AllPawns.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 2, map.mapPawns.AllPawns.Count);
    Expect.IsTrue(map.mapPawns.AllPawns.Contains(group.pawns[0]));
    Expect.IsTrue(map.mapPawns.AllPawns.Contains(group.vehicle));
  }

  [Test]
  private void AllPawnsSpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.AllPawnsSpawned.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 1, map.mapPawns.AllPawnsSpawned.Count);
    Expect.IsFalse(map.mapPawns.AllPawnsSpawned.Contains(group.pawns[0]));
    Expect.IsTrue(map.mapPawns.AllPawnsSpawned.Contains(group.vehicle));
  }

  [Test]
  private void AllPawnsUnspawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.AllPawnsUnspawned.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 1, map.mapPawns.AllPawnsUnspawned.Count);
    Expect.IsTrue(map.mapPawns.AllPawnsUnspawned.Contains(group.pawns[0]));
    Expect.IsFalse(map.mapPawns.AllPawnsUnspawned.Contains(group.vehicle));
  }

  [Test]
  private void SpawnedPawnsInFaction()
  {
    Faction faction = Faction.OfPlayer;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      faction = faction
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.SpawnedPawnsInFaction(faction).Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    List<Pawn> spawnedPawnsInFaction = map.mapPawns.SpawnedPawnsInFaction(faction);
    Expect.AreEqual(expected: 1, spawnedPawnsInFaction.Count);
    Expect.IsFalse(spawnedPawnsInFaction.Contains(group.pawns[0]));
    Expect.IsTrue(spawnedPawnsInFaction.Contains(group.vehicle));
  }

  [Test]
  private void AnyPawnBlockingMapRemoval()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.IsFalse(map.mapPawns.AnyPawnBlockingMapRemoval);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.IsTrue(map.mapPawns.AnyPawnBlockingMapRemoval);
  }

  [Test]
  private void AnyVehicleBlockingMapRemoval()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      // Empty vehicle should not block map removal
      drivers = 0
    });
    group.BoardAll();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(map.mapPawns.AnyPawnBlockingMapRemoval);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Expect.IsFalse(map.mapPawns.AnyPawnBlockingMapRemoval);
  }

  [Test]
  private void AnyAutonomousVehicleBlockingMapRemoval()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Autonomous
    });
    group.BoardAll();
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(map.mapPawns.AnyPawnBlockingMapRemoval);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Expect.IsTrue(map.mapPawns.AnyPawnBlockingMapRemoval);
  }

  [Test]
  private void ColonistCount()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.ColonistCount);
    Assert.IsFalse(map.mapPawns.AnyPawnBlockingMapRemoval);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 1, map.mapPawns.ColonistCount);
  }

  [Test]
  private void ColonistSpawnedCount()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.ColonistCount);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 0, map.mapPawns.ColonistsSpawnedCount);
  }

  [Test]
  private void ColonyAnimals()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      animals = 1
    });
    Pawn animal = group.pawns.First(pawn => pawn.IsAnimal);
    group.BoardAll();
    Assert.IsTrue(group.pawns.All(Ext_Vehicles.InVehicle));
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Assert.AreEqual(expected: 0, map.mapPawns.ColonyAnimals.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Expect.AreEqual(expected: 1, map.mapPawns.ColonyAnimals.Count);
    Expect.IsTrue(map.mapPawns.ColonyAnimals.Contains(animal));
  }

  [Test]
  private void ColonyAnimalsSpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      animals = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns.All(Ext_Vehicles.InVehicle));
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Assert.AreEqual(expected: 0, map.mapPawns.SpawnedColonyAnimals.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Expect.AreEqual(expected: 0, map.mapPawns.SpawnedColonyAnimals.Count);
  }

  [Test]
  private void ColonySubhumansControllable()
  {
  }

  [Test]
  private void ColonySubhumansControllableSpawned()
  {
  }

  [Test]
  private void PrisonersOfColony()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      prisoners = 1
    });
    Pawn animal = group.pawns.First(pawn => pawn.IsPrisonerOfColony);
    group.BoardAll();
    Assert.IsTrue(group.pawns.All(Ext_Vehicles.InVehicle));
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Assert.AreEqual(expected: 0, map.mapPawns.PrisonersOfColony.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Expect.AreEqual(expected: 1, map.mapPawns.PrisonersOfColony.Count);
    Expect.IsTrue(map.mapPawns.PrisonersOfColony.Contains(animal));
  }

  [Test]
  private void PrisonersOfColonySpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      animals = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns.All(Ext_Vehicles.InVehicle));
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Assert.AreEqual(expected: 0, map.mapPawns.PrisonersOfColonySpawned.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Expect.AreEqual(expected: 0, map.mapPawns.PrisonersOfColonySpawned.Count);
  }

  [Test]
  private void SlavesOfColonySpawned()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      slaves = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns.All(Ext_Vehicles.InVehicle));
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Assert.AreEqual(expected: 0, map.mapPawns.SlavesOfColonySpawned.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns.Any(pawn => pawn.Spawned));
    Expect.AreEqual(expected: 0, map.mapPawns.SlavesOfColonySpawned.Count);
  }

  [Test]
  private void FreeColonistsSpawnedOrInPlayerEjectablePodsCount()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.BoardAll();
    Assert.IsTrue(group.pawns[0].InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(group.pawns[0].Spawned);
    Expect.AreEqual(expected: 0, map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount);
  }

  [Test]
  private void FreeHumanlikesOfFaction()
  {
    Faction faction = Faction.OfPlayer;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      faction = faction
    });
    group.BoardAll();
    Pawn pawn = group.pawns[0];
    Assert.IsTrue(pawn.InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(pawn.Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.FreeHumanlikesOfFaction(faction).Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(pawn.Spawned);
    List<Pawn> freeHumanlikes = map.mapPawns.FreeHumanlikesOfFaction(faction);
    Expect.AreEqual(expected: 1, freeHumanlikes.Count);
    Expect.IsFalse(freeHumanlikes.Contains(group.vehicle));
    Expect.IsTrue(freeHumanlikes.Contains(pawn));
  }

  [Test]
  private void FreeHumanlikesSpawnedOfFaction()
  {
    Faction faction = Faction.OfPlayer;
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      faction = faction
    });
    group.BoardAll();
    Pawn pawn = group.pawns[0];
    Assert.IsTrue(pawn.InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(pawn.Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.FreeHumanlikesSpawnedOfFaction(faction).Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(pawn.Spawned);
    List<Pawn> freeHumanlikes = map.mapPawns.FreeHumanlikesSpawnedOfFaction(faction);
    Expect.AreEqual(expected: 0, freeHumanlikes.Count);
    Expect.IsFalse(freeHumanlikes.Contains(group.vehicle));
    Expect.IsFalse(freeHumanlikes.Contains(pawn));
    group.DisembarkAll();
    freeHumanlikes = map.mapPawns.FreeHumanlikesSpawnedOfFaction(faction);
    Expect.AreEqual(expected: 1, freeHumanlikes.Count);
    Expect.IsFalse(freeHumanlikes.Contains(group.vehicle));
    Expect.IsTrue(freeHumanlikes.Contains(pawn));
  }

  [Test]
  [LoadIfAnomalyActive]
  private void SpawnedShamblers()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1,
      extraSlots = 1
    });
    // Shambler should be tossed into cargo, so no extra slot is needed.
    Pawn shambler = PawnGenerator.GeneratePawn(PawnKindDefOf.ShamblerSwarmer);
    group.pawns.Add(shambler);
    group.BoardAll();
    Assert.IsTrue(shambler.InVehicle());
    Assert.IsFalse(group.vehicle.Spawned);
    Assert.IsFalse(shambler.Spawned);
    Assert.AreEqual(expected: 0, map.mapPawns.SpawnedShamblers.Count);
    group.Spawn();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsTrue(shambler.InVehicle());
    Assert.IsFalse(shambler.Spawned);
    Expect.AreEqual(expected: 0, map.mapPawns.SpawnedShamblers.Count);
    group.DisembarkAll();
    Assert.IsTrue(group.vehicle.Spawned);
    Assert.IsFalse(shambler.InVehicle());
    Assert.IsTrue(shambler.Spawned);
    Expect.AreEqual(expected: 1, map.mapPawns.SpawnedShamblers.Count);
  }
}