using System.Collections.Generic;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
	TestCategoryNames.WorldObject,
	TestCategoryNames.Caravaning
)]
[TestDescription("Dialog_Trade initialization with vehicles.")]
internal sealed class UnitTest_Trade_Settlement
{
	private static readonly MethodInfo InSellablePosition =
		AccessTools.Method(typeof(TradeDeal), "InSellablePosition");

	// Adds things to caravan and returns inventory list for verifying every item made it into the trade list
	private static List<Thing> FillInventory(VehicleGroup group, ITrader trader)
	{
		List<Thing> tradeables = [];
		Thing beer = CreateAndAdd(ThingDefOf.Beer);
		Thing mealPack = CreateAndAdd(ThingDefOf.MealSurvivalPack);
		Thing yayo = CreateAndAdd(ThingDefOf.Yayo);

		if (group.pawns.Count > 0)
		{
			Pawn dismounted = group.DisembarkOne();
			Pawn mounted = group.pawns.FirstOrDefault(pawn => pawn.IsColonist && pawn.InVehicle());
			Assert.AreNotEqual(dismounted, mounted);
			mounted.inventory.TryAddAndUnforbid(beer);
			dismounted.inventory.TryAddAndUnforbid(mealPack);
		}
		group.vehicle.inventory.TryAddAndUnforbid(yayo);
		group.BoardAll();
		return tradeables;

		Thing CreateAndAdd(ThingDef thingDef)
		{
			Assert.IsTrue(trader.TraderKind.WillTrade(thingDef));
			ThingWithComps thing = (ThingWithComps)ThingMaker.MakeThing(thingDef);
			thing.stackCount = thing.def.stackLimit;
			Assert.IsTrue(TradeUtility.PlayerSellableNow(thing, trader));
			tradeables.Add(thing);
			return thing;
		}
	}

	private static bool ContainsTradeable(Thing thing)
	{
		foreach (Tradeable tradeable in TradeSession.deal.AllTradeables)
		{
			if (tradeable.AnyThing == thing)
				return true;
		}
		return false;
	}

	[Test]
	private void VehicleCaravanInventory()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1
		});
		group.BoardAll();
		Assert.IsFalse(group.pawns.Any(WorldPawnsUtility.IsWorldPawn));
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		// Verify all tradeables get pulled from caravan
		ITrader trader = new MockTrader();
		List<Thing> tradeables = FillInventory(group, trader);
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		Assert.IsFalse(negotiator.IsWorldPawn());
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);

		foreach (Thing thing in tradeables)
		{
			Expect.IsTrue((bool)InSellablePosition.Invoke(TradeSession.deal, [thing, null]));
			Expect.IsTrue(ContainsTradeable(thing));
		}
	}

	[Test]
	private void AerialVehicleInventory()
	{
		const int DestTile = 1;

		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1,
			comps =
			[
				new CompProperties_VehicleLauncher
				{
					compClass = typeof(CompVehicleLauncher),
					launchProtocol = new DefaultTakeoff
					{
						launchProperties = new LaunchProtocolProperties(),
						landingProperties = new LaunchProtocolProperties()
					}
				}
			]
		});
		group.BoardAll();
		Assert.IsFalse(group.pawns.Any(WorldPawnsUtility.IsWorldPawn));
		AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
		Assert.IsNotNull(aerialVehicle);
		using ScopeWorldObject sav = new(aerialVehicle);
		aerialVehicle.recon = false;
		aerialVehicle.OrderFlyToTiles([new FlightNode(DestTile)], new ArrivalAction_LandToCaravan(group.vehicle));
		aerialVehicle.ArriveAtTile(DestTile);
		aerialVehicle.flightPath.ConsumeNode();
		Assert.IsTrue(aerialVehicle.Destroyed);
		VehicleCaravan caravan = group.vehicle.GetVehicleCaravan();
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);
		Assert.IsFalse(group.pawns.Any(WorldPawnsUtility.IsWorldPawn));

		// Verify all tradeables get pulled from caravan
		ITrader trader = new MockTrader();
		List<Thing> tradeables = FillInventory(group, trader);
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);
		foreach (Thing thing in tradeables)
		{
			Expect.IsTrue((bool)InSellablePosition.Invoke(TradeSession.deal, [thing, null]));
			Expect.IsTrue(ContainsTradeable(thing));
		}
	}

	[Test]
	private void TradeAnimalsInVehicle()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1
		});
		group.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		ITrader trader = new MockTrader();
		Pawn tradeable = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
		Assert.IsNotNull(tradeable);
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		Assert.IsFalse(negotiator.IsWorldPawn());
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);

		Expect.IsTrue((bool)InSellablePosition.Invoke(TradeSession.deal, [tradeable, null]));
		Expect.IsTrue(ContainsTradeable(tradeable));
	}

	[Test]
	private void TradeAnimalsDismounted()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1
		});
		group.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		group.vehicle.DisembarkAllFromInventory();

		ITrader trader = new MockTrader();
		Pawn tradeable = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
		Assert.IsNotNull(tradeable);
		Assert.IsTrue(tradeable.InVehicleCaravan());
		Assert.IsFalse(tradeable.InVehicle());
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		Assert.IsFalse(negotiator.IsWorldPawn());
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);

		Expect.IsTrue((bool)InSellablePosition.Invoke(TradeSession.deal, [tradeable, null]));
		Expect.IsTrue(ContainsTradeable(tradeable));
	}

	[Test]
	private void SellAnimalFromVehicle()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1
		});
		group.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		ITrader trader = new MockTrader();
		Pawn tradeable = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
		Assert.IsNotNull(tradeable);
		Assert.IsTrue(tradeable.InVehicle());
		Assert.IsTrue(tradeable.InVehicleCaravan());
		Assert.IsTrue(trader.TraderKind.WillTrade(tradeable.def));
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		Assert.IsFalse(negotiator.IsWorldPawn());
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);

		trader.GiveSoldThingToTrader(tradeable, 1, negotiator);
		Expect.IsFalse(tradeable.InVehicle());
		Expect.IsFalse(tradeable.InVehicleCaravan());
		Expect.IsFalse(caravan.AllPawnsAndVehiclePassengers.Contains(tradeable),
			"VehicleCaravan was not recached after trading");
	}

	[Test]
	private void SellAnimalFromDismounted()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1,
			animals = 1
		});
		group.BoardAll();

		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		group.vehicle.DisembarkAllFromInventory();

		ITrader trader = new MockTrader();
		Pawn tradeable = group.pawns.FirstOrDefault(pawn => pawn.IsAnimal);
		Assert.IsNotNull(tradeable);
		Assert.IsFalse(tradeable.InVehicle());
		Assert.IsTrue(tradeable.InVehicleCaravan());
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNotNull(negotiator);
		Assert.IsFalse(negotiator.IsWorldPawn());
		TradeSession.SetupWith(trader, negotiator, giftMode: false);
		Assert.IsTrue(TradeSession.Active);
		Assert.IsNotNull(TradeSession.deal);

		trader.GiveSoldThingToTrader(tradeable, 1, negotiator);
		Expect.IsFalse(tradeable.InVehicle());
		Expect.IsFalse(tradeable.InVehicleCaravan());
		Expect.IsFalse(caravan.AllPawnsAndVehiclePassengers.Contains(tradeable),
			"VehicleCaravan was not recached after trading");
	}

	[Test]
	private void VehicleCaravanAutonomous()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Autonomous,
		});
		Assert.IsTrue(group.vehicle.AllPawnsAboard.Count == 0);
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, startingTile: 1,
				addToWorldPawnsIfNotAlready: true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject swo = new(caravan);

		// Verify all tradeables get pulled from caravan
		ITrader trader = new MockTrader();
		_ = FillInventory(group, trader);
		Expect.IsFalse(caravan.CanTradeNow);
		Pawn negotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction.OfTradersGuild, trader.TraderKind);
		Assert.IsNull(negotiator);
	}

	private class MockTrader : ITrader
	{
		private const int PriceFactorSeed = 574019283;

		private readonly List<Thing> goods = [];

		public TraderKindDef TraderKind { get; } =
			DefDatabase<TraderKindDef>.GetNamed("Caravan_Outlander_BulkGoods");

		IEnumerable<Thing> ITrader.Goods => goods;

		int ITrader.RandomPriceFactorSeed => PriceFactorSeed;

		string ITrader.TraderName => "MockTrader";

		bool ITrader.CanTradeNow => true;

		float ITrader.TradePriceImprovementOffsetForPlayer => 0;

		Faction ITrader.Faction => Faction.OfTradersGuild;

		TradeCurrency ITrader.TradeCurrency => TradeCurrency.Silver;

		IEnumerable<Thing> ITrader.ColonyThingsWillingToBuy(Pawn playerNegotiator)
		{
			return playerNegotiator.GetVehicleCaravan().ColonyThingsWillingToBuy(playerNegotiator);
		}

		void ITrader.GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			playerNegotiator.GetVehicleCaravan().GiveSoldThingToPlayer(toGive, countToGive, playerNegotiator);
		}

		void ITrader.GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			Thing thing = toGive.SplitOff(countToGive);
			thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, this);
			thing.Destroy();
		}
	}
}