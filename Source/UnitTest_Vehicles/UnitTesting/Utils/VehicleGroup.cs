using System;
using System.Collections.Generic;
using System.Linq;
using DevTools;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

public class VehicleGroup : IDisposable
{
  public readonly VehiclePawn vehicle;
  public readonly List<Pawn> pawns = [];

  public VehicleGroup(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  public void Spawn()
  {
    DeSpawn();
    TestUtils.ForceSpawn(vehicle);
    BoardAll();
  }

  public void SpawnPawns()
  {
    Map map = Find.CurrentMap;
    foreach (Pawn pawn in pawns)
    {
      GenSpawn.Spawn(pawn, CellFinder.RandomSpawnCellForPawnNear(map.Center, map), map, Rot4.North);
      Assert.IsTrue(pawn.Spawned);
    }
  }

  public void DeSpawn()
  {
    if (vehicle.Spawned)
      vehicle.DeSpawn();
    Assert.IsFalse(vehicle.Spawned);

    foreach (Pawn pawn in pawns)
    {
      if (pawn.Spawned)
        pawn.DeSpawn();
      Assert.IsFalse(pawn.Spawned);
    }
  }

  public void DeSpawnPawns()
  {
    foreach (Pawn pawn in pawns)
    {
      if (pawn.Spawned)
        pawn.DeSpawn();
      Assert.IsFalse(pawn.Spawned);
    }
  }

  public void BoardOne()
  {
    Pawn pawn = pawns.First();
    Assert.IsTrue(vehicle.TryAddPawn(pawn));
    Assert.IsFalse(pawn.Spawned);
  }

  public void BoardAll()
  {
    foreach (Pawn pawn in pawns)
    {
      if (!pawn.InVehicle())
      {
        if (pawn.GetVehicleCaravan() is { } caravan)
          caravan.RemovePawn(pawn);
        Assert.IsTrue(vehicle.TryAddPawn(pawn));
      }
    }
  }

  public Pawn DisembarkOne()
  {
    Pawn pawn = pawns.First();
    vehicle.DisembarkPawn(pawn);
    if (vehicle.Spawned)
      Assert.IsTrue(pawn.Spawned);
    else if (vehicle.InVehicleCaravan())
      Assert.IsTrue(pawn.InVehicleCaravan());
    else
      throw new NotImplementedException("Unhandled disembarking situation.");
    return pawn;
  }

  public void DisembarkAll()
  {
    vehicle.DisembarkAll();
    foreach (Pawn pawn in pawns)
    {
      // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
      if (vehicle.InVehicleCaravan())
        Assert.IsTrue(pawn.InVehicleCaravan());
      else
        Assert.IsTrue(pawn.Spawned);
    }
  }

  public void Dispose()
  {
    foreach (Pawn pawn in pawns)
    {
      vehicle.RemovePawn(pawn);
      Assert.IsFalse(pawn.InVehicle());
      if (!pawn.Destroyed)
        pawn.Destroy();
      Assert.IsTrue(pawn.Destroyed);
    }
    Assert.IsTrue(vehicle.AllPawnsAboard.Count == 0);
    if (!vehicle.Destroyed)
      vehicle.Destroy();
    Assert.IsTrue(vehicle.Destroyed);
  }

  public static VehicleDef CreateVehicleDef(MockSettings settings)
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef($"VehicleDef_MOCK_{Rand.Int}", settings);

    if (!settings.statModifiers.NullOrEmpty())
    {
      vehicleDef.vehicleStats = [.. settings.statModifiers];
    }
    else
    {
      // Default values to ensure vehicle is at least moveable if required
      vehicleDef.vehicleStats =
      [
        new VehicleStatModifier
        {
          statDef = VehicleStatDefOf.MoveSpeed,
          value = !settings.permissions.HasFlag(VehiclePermissions.Mobile) ? 0 : 10
        },
        new VehicleStatModifier
        {
          statDef = VehicleStatDefOf.CargoCapacity,
          value = 1,
        }
      ];
    }

    int totalSlots = (settings.passengers + settings.animals + settings.extraSlots);
    if (totalSlots > 0)
    {
      vehicleDef.properties.roles =
      [
        new VehicleRole
        {
          key = "Passenger",
          slots = totalSlots
        }
      ];
    }

    if (!settings.permissions.HasFlag(VehiclePermissions.Autonomous))
    {
      vehicleDef.properties.roles.Add(new VehicleRole
      {
        key = "Driver",
        slots = settings.drivers,
        slotsToOperate = settings.drivers,

        handlingTypes = HandlingType.Movement
      });
    }
    return vehicleDef;
  }

  public static VehicleGroup CreateBasicVehicleGroup(MockSettings settings)
  {
    VehicleDef vehicleDef = settings.vehicleDef ?? CreateVehicleDef(settings);

    // VehicleDef needs to be complete by this point for PostGeneration events
    VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, settings.faction);
    DevLog.WriteVerbose($"Creating vehicle {vehicle}");
    VehicleGroup group = new(vehicle);
    for (int i = 0; i < settings.drivers + settings.passengers; i++)
    {
      Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
        Faction.OfPlayer, fixedBiologicalAge: 30, forceNoBackstory: true));
      Assert.IsNotNull(colonist);
      Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
      group.pawns.Add(colonist);
    }
    for (int i = 0; i < settings.animals; i++)
    {
      Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
      Assert.IsNotNull(animal);
      Assert.AreEqual(animal.Faction, Faction.OfPlayer);
      group.pawns.Add(animal);
    }
    return group;
  }

  public class MockSettings
  {
    public VehicleDef vehicleDef;
    public string debugLabel;

    // Reverse mapping permissions to def restrictions for easy configuration
    public VehicleType type = VehicleType.Land;
    public VehiclePermissions permissions = VehiclePermissions.Mobile;
    public int drivers;
    public int passengers;
    public int animals;
    public int extraSlots;

    public VehicleProperties properties;
    public VehicleDrawProperties drawProperties;
    public Faction faction = Faction.OfPlayer;

    public List<VehicleComponentProperties> components;
    public List<VehicleStatModifier> statModifiers;
    public List<CompProperties> comps;
  }
}