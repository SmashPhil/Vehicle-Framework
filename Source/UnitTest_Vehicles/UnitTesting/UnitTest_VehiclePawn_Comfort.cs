using System.Linq;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
  TestCategoryNames.TickBehavior,
  TestCategoryNames.WorldObject,
  TestCategoryNames.Caravaning
)]
[TestDescription("VehicleCaravan needs mechanics on the world map.")]
internal sealed class UnitTest_VehiclePawn_Comfort
{
	private const int TickDelta = 15;

	private static readonly FastInvokeHandler TickInterval = MethodInvoker.GetHandler(AccessTools.Method(typeof(Pawn), "TickInterval"));

	private static bool InPassengerRole(Pawn pawn)
	{
		VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		return (handler.role.HandlingTypes & HandlingType.Movement) == 0;
	}

	private static bool InDriverRole(Pawn pawn)
	{
		VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		return (handler.role.HandlingTypes & HandlingType.Movement) != 0;
	}

	private static Thing AddBedrollToCaravan(VehicleCaravan caravan)
	{
		Thing bedRoll = ThingMaker.MakeThing(ThingDefOf.Bedroll, ThingDefOf.Leather_Plain);
		CaravanInventoryUtility.GiveThing(caravan, bedRoll);
		return bedRoll;
	}

	[Test]
	private void VehicleRoleComfort()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
		group.Spawn();

		Pawn pawn = group.pawns[0];
		VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		Assert.IsNotNull(pawn.needs.comfort);
		// Comfort is set while inside vehicle
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, handler.role.Comfort);
	}

	[Test]
	private void VehicleInventoryComfort()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
		group.Spawn();
		group.DisembarkAll();
		Pawn pawn = group.pawns[0];
		group.vehicle.AddOrTransfer(pawn);
		Assert.IsTrue(pawn.ParentHolder is Pawn_InventoryTracker { pawn: VehiclePawn });
		Assert.IsNotNull(pawn.needs.comfort);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		// Comfort is set while inside vehicle cargo
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, VehicleRoleHandler.ComfortInsideCargo);
	}

	[Test]
	private void CaravanMountedNoBedroll()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.pawns[0];
		VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		Assert.IsNotNull(pawn.needs.comfort);
		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		// inside vehicle
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, handler.role.Comfort);
	}

	[Test]
	private void CaravanMountedBedroll()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.pawns[0];
		VehicleRoleHandler handler = pawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		Assert.IsNotNull(pawn.needs.comfort);
		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		Thing bedroll = AddBedrollToCaravan(caravan);
		float comfort = bedroll.GetStatValue(StatDefOf.Comfort, true, 100);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, comfort);
		// Vehicle role doesn't override bedroll comfort if lower
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, comfort);
	}

	[Test]
	private void CaravanMountedBedrollFallback()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		// First pawn in vehicle takes bedroll
		Pawn pawnWithBedroll = group.vehicle.AllPawnsAboard[0];
		Pawn pawnNoBedroll = group.vehicle.AllPawnsAboard[1];
		Assert.IsNotNull(pawnWithBedroll.needs.comfort);
		Assert.IsNotNull(pawnNoBedroll.needs.comfort);
		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		Thing bedroll = AddBedrollToCaravan(caravan);
		float comfort = bedroll.GetStatValue(StatDefOf.Comfort, true, 100);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawnWithBedroll.needs.comfort.CurInstantLevel, comfort);

		VehicleRoleHandler handler = pawnNoBedroll.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		PawnUtility.GainComfortFromThingIfPossible(pawnNoBedroll, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawnNoBedroll.needs.comfort.CurInstantLevel, handler.role.Comfort);
	}

	[Test]
	private void CaravanDismountedBedroll()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		group.DisembarkAll();
		Pawn pawn = group.pawns[0];
		Assert.IsNotNull(pawn.needs.comfort);
		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		Thing bedroll = AddBedrollToCaravan(caravan);
		float comfort = bedroll.GetStatValue(StatDefOf.Comfort, true, 100);

		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, comfort);
	}

	[Test]
	private void CaravanDismountedNoBedroll()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		group.DisembarkAll();
		Pawn pawn = group.pawns[0];
		Assert.IsNotNull(pawn.needs.comfort);
		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, 0);
	}

	[Test]
	private void CaravanDismountedPriorityBedroll()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1,
			passengers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.DisembarkOne();
		Pawn mountedPawn = group.pawns.First(Ext_Vehicles.InVehicle);
		Assert.IsNotNull(pawn.needs.comfort);
		Assert.IsNotNull(mountedPawn.needs.comfort);

		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		Thing bedroll = AddBedrollToCaravan(caravan);
		float comfort = bedroll.GetStatValue(StatDefOf.Comfort);

		caravan.beds.BedsTrackerTickInterval(TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, comfort);

		VehicleRoleHandler handler = mountedPawn.ParentHolder as VehicleRoleHandler;
		Assert.IsNotNull(handler);
		PawnUtility.GainComfortFromThingIfPossible(mountedPawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(mountedPawn.needs.comfort.CurInstantLevel, handler.role.Comfort);
	}

	[Test]
	private void CaravanMountedDismounted()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.pawns[0];
		Assert.IsNotNull(pawn.needs.comfort);

		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, CurrentHandler(pawn).role.Comfort);

		group.DisembarkAll();
		// Set use tick to expire vehicle comfort bonus
		pawn.needs.comfort.lastComfortUseTick = Find.TickManager.TicksGame - (TickDelta + 1);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, 0);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, 0);

		group.BoardAll();
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, CurrentHandler(pawn).role.Comfort);
		return;

		static VehicleRoleHandler CurrentHandler(Pawn pawn)
		{
			return pawn.ParentHolder as VehicleRoleHandler;
		}
	}

	[Test]
	private void CaravanMountedDismountedInventory()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.pawns[0];
		Assert.IsNotNull(pawn.needs.comfort);

		Assert.IsFalse(caravan.pather.MovingNow);
		Assert.IsFalse(caravan.vehiclePather.MovingNow);

		group.DisembarkAll();
		group.vehicle.AddOrTransfer(pawn);
		Assert.IsTrue(pawn.ParentHolder is Pawn_InventoryTracker { pawn: VehiclePawn });

		ResetComfortCacheTick();

		// inventory -> role
		group.DisembarkAll();
		group.BoardAll();
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, CurrentHandler(pawn).role.Comfort);
		group.vehicle.AddOrTransfer(pawn);
		ResetComfortCacheTick();
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, VehicleRoleHandler.ComfortInsideCargo);

		// dismounted -> inventory
		group.DisembarkAll();
		ResetComfortCacheTick();
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, 0);
		group.vehicle.AddOrTransfer(pawn);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, VehicleRoleHandler.ComfortInsideCargo);

		// inventory -> dismounted
		ResetComfortCacheTick();
		group.vehicle.AddOrTransfer(pawn);
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, VehicleRoleHandler.ComfortInsideCargo);
		group.DisembarkAll();
		ResetComfortCacheTick();
		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, 0);
		return;

		void ResetComfortCacheTick()
		{
			pawn.needs.comfort.lastComfortUseTick = Find.TickManager.TicksGame - (TickDelta + 1);
			caravan.beds.BedsTrackerTickInterval(TickDelta);
			PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		}

		static VehicleRoleHandler CurrentHandler(Pawn pawn)
		{
			return pawn.ParentHolder as VehicleRoleHandler;
		}
	}

	[Test]
	private void CaravanInventory()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});

		group.BoardAll();
		VehicleCaravan caravan =
			CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
		Assert.IsNotNull(caravan);
		using ScopeWorldObject sc = new(caravan);

		Pawn pawn = group.pawns[0];
		group.DisembarkAll();
		group.vehicle.AddOrTransfer(pawn);
		Assert.IsTrue(pawn.ParentHolder is Pawn_InventoryTracker { pawn: VehiclePawn });

		caravan.beds.BedsTrackerTickInterval(TickDelta);
		PawnUtility.GainComfortFromThingIfPossible(pawn, group.vehicle, TickDelta);
		Expect.AreApproximatelyEqual(pawn.needs.comfort.CurInstantLevel, VehicleRoleHandler.ComfortInsideCargo);
	}

	[Test, Disabled]
	private void CaravanMovingMountedSick()
	{
		// TODO VF-305
	}

	[Test, Disabled]
	private void CaravanMovingDismountedSick()
	{
		// TODO VF-305
	}

	[Test, Disabled]
	private void CaravanMovingInventorySick()
	{
		// TODO VF-305
	}
}