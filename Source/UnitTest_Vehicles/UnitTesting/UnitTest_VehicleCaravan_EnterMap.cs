using System;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
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
	private static readonly IntVec3 TestMapSize = new IntVec3(75, 1, 75);

	private static readonly MethodInfo ArriveAtSiteMethod = 
		AccessTools.Method(typeof(CaravanArrivalAction_VisitSite), "DoEnter");
	private static readonly MethodInfo ArriveAtEscapeShipMethod = 
		AccessTools.Method(typeof(CaravanArrivalAction_VisitEscapeShip), "DoArrivalAction");
	private static readonly MethodInfo AttackSettlementMethod = 
		AccessTools.Method(typeof(SettlementUtility), "AttackNow");

	private static PlanetTile GetValidCampTile()
	{
		foreach (SurfaceTile surfaceTile in Find.WorldGrid.Tiles)
		{
			PlanetTile tile = surfaceTile.tile;

			// Rivers are really buggy with map generation at small sizes and at edges of the world grid.
			if (tile.Tile.Mutators.Any(static tileMutatorDef => tileMutatorDef.Worker is TileMutatorWorker_River))
				continue;

			if (Find.WorldPathGrid.Passable(tile) && !Find.WorldObjects.AnyWorldObjectAt(tile) && 
				SettleInEmptyTileUtility.CanCreateMapAt(tile))
			{
				return tile;
			}
		}
		return PlanetTile.Invalid;
	}

	private static WorldObject SetUpCamp(VehicleCaravan caravan)
	{
		using GenStepWarningDisabler wd = new();
		Map map = GetOrGenerateMapUtility.GetOrGenerateMap(caravan.Tile, TestMapSize, WorldObjectDefOf.Camp);
		map.Parent.SetFaction(caravan.Faction);
		Pawn pawn = caravan.PawnsListForReading[0];
		CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Center, CaravanDropInventoryMode.DoNotDrop, false,
			cell => cell.GetRoom(map).CellCount >= 600 && !cell.GetTerrain(map).IsWater);
		CameraJumper.TryJump(pawn);
		return map.Parent;
	}

	private static WorldObject Settle(VehicleCaravan caravan)
	{
		using GenStepWarningDisabler wd = new();
		Settlement newHome = SettleUtility.AddNewHome(caravan.Tile, caravan.Faction);
		Map map = GetOrGenerateMapUtility.GetOrGenerateMap(caravan.Tile, TestMapSize, null);
		Pawn pawn = caravan.PawnsListForReading[0];
		CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Center, CaravanDropInventoryMode.DropInstantly,
			extraCellValidator: cell => cell.GetRoom(map).CellCount >= 600);
		newHome.Notify_MyMapSettled(map);
		CameraJumper.TryJump(pawn);
		return map.Parent;
	}

	private static WorldObject ArriveAtSite(VehicleCaravan caravan)
	{
		using GenStepWarningDisabler wd = new();
		Site site = SiteMaker.TryMakeSite([SitePartDefOf.PreciousLump], caravan.Tile);
		Find.WorldObjects.Add(site);
		CaravanArrivalAction_VisitSite arrivalMode = new(site);
		ArriveAtSiteMethod.Invoke(arrivalMode, [caravan, site]);
		return site;
	}

	private static WorldObject ArriveAtEscapeShip(VehicleCaravan caravan)
	{
		using GenStepWarningDisabler wd = new();
		WorldObject worldObject = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.EscapeShip);
		worldObject.Tile = caravan.Tile;
		Find.WorldObjects.Add(worldObject);
		EscapeShipComp comp = worldObject.GetComponent<EscapeShipComp>();
		Assert.IsNotNull(comp);
		CaravanArrivalAction_VisitEscapeShip arrivalMode = new(comp);
		ArriveAtEscapeShipMethod.Invoke(arrivalMode, [caravan]);
		return worldObject;
	}

	[Test]
	[TestDescription("Setting up camp correctly spawns all pawns, places the vehicle, and destroys the caravan with no vanilla caravan artifact.")]
	private void SetUpCamp()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		using ScopeWorldObject swo = new(SetUpCamp(vehicleCaravan));

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}

	[Test]
	[TestDescription("Setting up camp correctly spawns all pawns, places the vehicle, and destroys the caravan with no vanilla caravan artifact.")]
	private void CreateNewSettlement()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		using ScopeWorldObject swo = new(Settle(vehicleCaravan));

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}

	[Test]
	[TestDescription("Arriving at site correctly spawns all pawns, places the vehicle, and destroys the caravan with no vanilla caravan artifact.")]
	private void ArriveAtSite()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		using ScopeWorldObject swo = new(ArriveAtSite(vehicleCaravan));

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}

	[Test]
	private void ArriveAtEscapeShip()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		using ScopeWorldObject swo = new(ArriveAtEscapeShip(vehicleCaravan));

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}

	[Test]
	private void EnterSettlement()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		Settlement homeSettlement = Find.Maps.First(map => map.IsPlayerHome && map.Parent is Settlement).Parent as Settlement;

		CaravanArrivalAction_Enter enterSettlement = new(homeSettlement);
		enterSettlement.Arrived(vehicleCaravan);

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}

	[Test]
	private void AttackSettlement()
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
		using ScopeWorldObject sc = new(vehicleCaravan);

		group.DisembarkAll();
		// Driver
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Humanlike)));
		// Cargo Pawn
		Assert.IsTrue(group.vehicle.TryAddPawn(group.pawns.First(pawn => pawn.RaceProps.Animal)));

		Settlement enemySettlement = Find.WorldObjects.Settlements
			.First(settlement => !settlement.HasMap && settlement.Faction != Faction.OfPlayer);
		using GenStepWarningDisabler wd = new();
		AttackSettlementMethod.Invoke(null, [vehicleCaravan, enemySettlement]);

		Assert.IsTrue(vehicleCaravan.Destroyed);
		Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
		Expect.IsTrue(group.vehicle.Spawned);
		Expect.IsNull(Find.WorldObjects.PlayerControlledCaravanAt(tile));
		Expect.IsNull(Find.World.GetComponent<VehicleWorldObjectsHolder>().VehicleCaravanObject(group.vehicle));
	}
}