using System;
using System.Linq;
using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.WorldObject)]
[TestDescription("VehicleCaravans generate a camp, spawn all pawns, and correctly transition from world to local map.")]
internal sealed class UnitTest_VehicleCaravan_EnterMap
{
	private static PlanetTile GetValidCampTile()
	{
		foreach (SurfaceTile surfaceTile in Find.WorldGrid.Tiles)
		{
			PlanetTile tile = surfaceTile.tile;
			if (SettleInEmptyTileUtility.CanCreateMapAt(tile))
				return tile;
		}
		return PlanetTile.Invalid;
	}

	private static void SettleInTile(VehicleCaravan caravan)
	{
		Map map = GetOrGenerateMapUtility.GetOrGenerateMap(caravan.Tile,
			WorldObjectDefOf.Camp.overrideMapSize ?? Find.World.info.initialMapSize, WorldObjectDefOf.Camp);
		map.Parent.SetFaction(caravan.Faction);
		Thing thing = caravan.PawnsListForReading[0];
		CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Center, CaravanDropInventoryMode.DoNotDrop, false,
			cell => cell.GetRoom(map).CellCount >= 600 && !cell.GetTerrain(map).IsWater);
		CameraJumper.TryJump(thing);
	}

	[Test]
	[TestDescription("Setting up camp correctly spawns all pawns, places the vehicle, and destroys the caravan with no vanilla caravan artifact.")]
	private void Camp()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1,
			mechanoids = 1
		});

		PlanetTile tile = GetValidCampTile();
		group.BoardAll();
		VehicleCaravan vehicleCaravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, tile, true);
		Assert.IsNotNull(vehicleCaravan);
		using ScopeWorldObject swo = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		SettleInTile(vehicleCaravan);

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}
}