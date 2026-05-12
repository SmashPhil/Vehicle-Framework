using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

public class VehicleGroup : IDisposable
{
	public readonly VehiclePawn vehicle;
	public readonly List<Pawn> pawns = [];

	public VehicleGroup(VehiclePawn vehicle)
	{
		this.vehicle = vehicle;
	}

	// NOTE - many tests rely on the behavior that spawning the vehicle group will immediately board all pawns.
	// This should not be changed without first checking every location a vehicle group is spawned.
	public void Spawn()
	{
		DeSpawn();
		// Boarding must happen BEFORE the vehicle spawns so that any events in SpawnSetup will have the pawn list
		// primed and ready for reading, otherwise that list will be stale.
		BoardAll();
		TestUtils.ForceSpawn(vehicle);
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
		vehicle.DisembarkAllFromInventory();
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
					value = (settings.permissions & VehiclePermissions.Mobile) != 0 ? 10 : 0
				},
				new VehicleStatModifier
				{
					statDef = VehicleStatDefOf.CargoCapacity,
					value = 1,
				}
			];
		}
		TestDefGenerator.ClearStatWorkerCaches(vehicleDef);
		vehicleDef.RecacheMovementPermissions();

		int totalSlots = settings.passengers + settings.prisoners + settings.extraSlots;
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

		if ((settings.permissions & VehiclePermissions.Autonomous) == 0)
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
		VehicleGroup group = new(vehicle);
		for (int i = 0; i < settings.drivers + settings.passengers; i++)
		{
			PawnGenerationRequest request = new(PawnKindDefOf.Colonist,
				settings.faction, fixedBiologicalAge: 30, forceNoBackstory: true)
			{
				ForceNoIdeo = settings.forceNoIdeology
			};
			Pawn colonist = PawnGenerator.GeneratePawn(request);
			Assert.IsNotNull(colonist);
			Assert.AreEqual(colonist.Faction, settings.faction);
			if (settings.destroyInventory)
			{
				// Colonists can spawn with drugs in their inventory, generating a thing with no ownership
				// in caravans which will log an error for transferable operations.
				colonist.inventory.DestroyAll();
			}
			group.pawns.Add(colonist);
		}
		for (int i = 0; i < settings.prisoners; i++)
		{
			Faction faction = Find.World.factionManager.RandomEnemyFaction();
			PawnGenerationRequest request = new(PawnKindDefOf.Colonist,
				faction, fixedBiologicalAge: 30, forceNoBackstory: true)
			{
				ForceNoIdeo = settings.forceNoIdeology
			};
			Pawn prisoner = PawnGenerator.GeneratePawn(request);
			prisoner.guest?.CapturedBy(Faction.OfPlayer);
			group.pawns.Add(prisoner);
		}
		for (int i = 0; i < settings.animals; i++)
		{
			Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, settings.faction);
			Assert.IsNotNull(animal);
			Assert.AreEqual(animal.Faction, settings.faction);
			group.pawns.Add(animal);
		}
		Assert.IsTrue(settings.mechanoids == 0 || ModsConfig.BiotechActive);
		for (int i = 0; i < settings.mechanoids; i++)
		{
			Pawn mech = PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, settings.faction);
			Assert.IsNotNull(mech);
			Assert.AreEqual(mech.Faction, settings.faction);
			group.pawns.Add(mech);
		}
		return group;
	}

	public class MockSettings
	{
		public VehicleDef vehicleDef;
		public string debugLabel;
		public IntVec2 size = IntVec2.One;
		public VehicleProperties properties;
		public VehicleDrawProperties drawProperties;

		public List<VehicleComponentProperties> components;
		public List<VehicleStatModifier> statModifiers;
		public List<CompProperties> comps;

		// Precepts can add a lot of unknown mechanics that make tests unpredictable, disable by default.
		[MayRequireIdeology]
		public bool forceNoIdeology = true;

		// Reverse mapping permissions to def restrictions for easy configuration
		public VehicleType type = VehicleType.Land;
		public VehiclePermissions permissions = VehiclePermissions.Mobile;
		public int drivers;
		public int passengers;
		public int prisoners;
		public int animals;
		public int mechanoids;
		public int extraSlots;

		public bool destroyInventory;

		public Faction faction = Faction.OfPlayer;
	}
}