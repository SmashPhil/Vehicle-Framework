using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.TickBehavior,
  TestCategoryNames.WorldObject,
  TestCategoryNames.Caravaning
)]
[TestDescription("VehicleCaravan global tick rates for all pawns in the caravan.")]
internal sealed class Test_VehicleCaravan_Tick
{
  private static readonly MethodInfo PawnUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.UpdateRateTicks));

  private static readonly MethodInfo VehicleUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.UpdateRateTicks));

  private static readonly MethodInfo VehicleCaravanUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(WorldObject), "UpdateRateTicks");

  private static readonly MethodInfo OverrideMethod =
    AccessTools.Method(typeof(Test_VehicleCaravan_Tick), nameof(OverrideUpdateRateTicks));

  private static void OverrideUpdateRateTicks(out int __result)
  {
    __result = 1;
  }

  [Test]
  private void Vehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.ExitMapAndCreateVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, 2, 3, sendMessage: false);
    Assert.IsNotNull(vehicleCaravan);
    using ScopeWorldObject swo = new(vehicleCaravan);
    using ScopedMethodHook smh = new(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using TickObserver<VehiclePawn> to = new(group.vehicle);
    Assert.AreEqual(group.vehicle.UpdateRateTicks, 1);
    Assert.IsTrue(group.vehicle.IsWorldPawn());
    Find.TickManager.DoSingleTick();
    Expect.AreEqual(to.TickCount, 1);
  }

  [Test]
  private void BoardedPawns()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1,
      animals = 1,
    });
    group.Spawn();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.ExitMapAndCreateVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, 2, 3, sendMessage: false);
    Assert.IsNotNull(vehicleCaravan);
    using ScopeWorldObject swo = new(vehicleCaravan);
    using ScopedMethodHook smhCaravan = new(VehicleCaravanUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using ScopedMethodHook smhVehicle = new(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using ScopedMethodHook smh = new(PawnUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    foreach (Pawn pawn in group.vehicle.AllPawnsAboard)
    {
      Assert.AreEqual(pawn.UpdateRateTicks, 1);
      Assert.IsFalse(pawn.IsWorldPawn());
      using TickObserver<Pawn> to = new(pawn);
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(to.TickCount, 1);
    }
  }

  [Test]
  private void DismountedPawns()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1,
      animals = 1,
    });
    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.ExitMapAndCreateVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, 2, 3, sendMessage: false);
    Assert.IsNotNull(vehicleCaravan);
    using ScopeWorldObject swo = new(vehicleCaravan);
    using ScopedMethodHook smh = new(PawnUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));

    group.DisembarkAll();
    foreach (Pawn pawn in group.pawns)
    {
      Assert.AreEqual(pawn.UpdateRateTicks, 1);
      Assert.IsTrue(pawn.IsWorldPawn());
      using TickObserver<Pawn> to = new(pawn);
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(to.TickCount, 1);
    }
  }
}