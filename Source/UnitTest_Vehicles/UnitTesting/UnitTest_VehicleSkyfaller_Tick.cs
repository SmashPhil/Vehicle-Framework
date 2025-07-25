using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using RimWorld.Planet;
using SmashTools.Targeting;
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
[TestDescription("VehicleSkyfaller global tick rates for all pawns in the vehicle.")]
internal sealed class UnitTest_VehicleSkyfaller_Tick
{
  private const int StartTile = 1;
  private const int DestTile = 2;

  private static readonly MethodInfo PawnUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.UpdateRateTicks));

  private static readonly MethodInfo VehicleUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.UpdateRateTicks));

  private static readonly MethodInfo VehicleSkyfallerUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(Thing), "UpdateRateTicks");

  private static readonly MethodInfo OverrideMethod =
    AccessTools.Method(typeof(UnitTest_VehicleSkyfaller_Tick), nameof(OverrideUpdateRateTicks));

  private VehicleGroup.MockSettings mockSettings;

  private static void OverrideUpdateRateTicks(out int __result)
  {
    __result = 1;
  }

  [SetUp]
  private void CreateAerialVehicleSettings()
  {
    // We can't initialize this on launch, it requires the faction manager.
    mockSettings = new VehicleGroup.MockSettings
    {
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
    };
    mockSettings.vehicleDef = VehicleGroup.CreateVehicleDef(mockSettings);
  }

  [Test]
  [TestDescription("Skyfaller leaving ticks at the correct rate for the inner vehicle.")]
  private void SkyfallerLeaving_Vehicle()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.Spawn();

    TargetData<GlobalTargetInfo> targetData = new();
    targetData.targets.AddRange([new GlobalTargetInfo(1), new GlobalTargetInfo(2)]);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    compLauncher.Launch(targetData, new ArrivalAction_LandToCaravan(group.vehicle));

    // Pass vehicle and passengers to world
    using ScopedMethodHook smhSkyfaller =
      new(VehicleSkyfallerUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using ScopedMethodHook smhVehicle = new(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using TickObserver<VehiclePawn> to = new(group.vehicle);
    Assert.AreEqual(group.vehicle.UpdateRateTicks, 1);
    Assert.IsFalse(group.vehicle.IsWorldPawn());
    Find.TickManager.DoSingleTick();
    Expect.AreEqual(to.TickCount, 1);
  }

  [Test]
  [TestDescription("Skyfaller leaving ticks at the correct rate for all pawns onboard.")]
  private void SkyfallerLeaving_BoardedPawns()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.Spawn();

    TargetData<GlobalTargetInfo> targetData = new();
    targetData.targets.AddRange([new GlobalTargetInfo(1), new GlobalTargetInfo(2)]);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    compLauncher.Launch(targetData, new ArrivalAction_LandToCaravan(group.vehicle));

    using ScopedMethodHook smhSkyfaller =
      new(VehicleSkyfallerUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
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
  [TestDescription("Skyfaller arriving ticks at the correct rate for the inner vehicle.")]
  private void SkyfallerArriving_Vehicle()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();
    IntVec3 cell = map.Center;
    TestUtils.PrepareArea(map, cell, group.vehicle.VehicleDef);

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, map.Tile);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    Assert.IsTrue(Find.WorldObjects.Contains(aerialVehicle));
    aerialVehicle.OrderFlyToTiles([new FlightNode(map.Tile)],
      new ArrivalAction_LandToCell(group.vehicle, map.Parent, cell, Rot4.North));
    aerialVehicle.ArriveAtTile(map.Tile);
    aerialVehicle.flightPath.ConsumeNode();
    Assert.IsTrue(aerialVehicle.Destroyed);

    VehicleSkyfaller_Arriving skyfaller = map.thingGrid.ThingAt<VehicleSkyfaller_Arriving>(cell);
    Assert.IsNotNull(skyfaller);

    // Pass vehicle and passengers to world
    using ScopedMethodHook smhSkyfaller =
      new(VehicleSkyfallerUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using ScopedMethodHook smhVehicle = new(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using TickObserver<VehiclePawn> to = new(group.vehicle);
    Assert.AreEqual(group.vehicle.UpdateRateTicks, 1);
    Assert.IsFalse(group.vehicle.IsWorldPawn());
    Find.TickManager.DoSingleTick();
    Expect.AreEqual(to.TickCount, 1);
  }

  [Test]
  [TestDescription("Skyfaller arriving ticks at the correct rate for all pawns onboard.")]
  private void SkyfallerArriving_BoardedPawns()
  {
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.Spawn();

    TargetData<GlobalTargetInfo> targetData = new();
    targetData.targets.AddRange([new GlobalTargetInfo(1), new GlobalTargetInfo(2)]);
    CompVehicleLauncher compLauncher = group.vehicle.CompVehicleLauncher;
    compLauncher.Launch(targetData, new ArrivalAction_LandToCaravan(group.vehicle));

    using ScopedMethodHook smhSkyfaller =
      new(VehicleSkyfallerUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
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
}