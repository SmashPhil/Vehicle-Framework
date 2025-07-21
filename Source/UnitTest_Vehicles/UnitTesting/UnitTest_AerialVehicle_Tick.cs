using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
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
[TestDescription("AerialVehicle global tick rates for all pawns in the vehicle.")]
internal sealed class UnitTest_AerialVehicle_Tick
{
  private const int StartTile = 1;
  private const int DestTile = 2;

  private static readonly MethodInfo PawnUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.UpdateRateTicks));

  private static readonly MethodInfo VehicleUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.UpdateRateTicks));

  private static readonly MethodInfo AerialVehicleUpdateRateTicks =
    AccessTools.PropertyGetter(typeof(WorldObject), "UpdateRateTicks");

  private static readonly MethodInfo OverrideMethod =
    AccessTools.Method(typeof(UnitTest_VehicleCaravan_Tick), nameof(OverrideUpdateRateTicks));

  private VehicleGroup.MockSettings mockSettings;

  private static void OverrideUpdateRateTicks(out int __result)
  {
    __result = 1;
  }

  [SetUp]
  private void CreateAerialVehicleSettings()
  {
    RimWorld.Planet.World world = Find.World;
    Assert.IsNotNull(world);
    Map map = Find.CurrentMap;
    Assert.IsNotNull(map);
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
  private void Vehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    Assert.IsTrue(Find.WorldObjects.Contains(aerialVehicle));
    // Pass vehicle and passengers to world
    using ScopedMethodHook smhAerialVehicle =
      new(AerialVehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using ScopedMethodHook smhVehicle = new(VehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
    using TickObserver<VehiclePawn> to = new(group.vehicle);
    Assert.AreEqual(group.vehicle.UpdateRateTicks, 1);
    Assert.IsTrue(group.vehicle.IsWorldPawn());
    Find.TickManager.DoSingleTick();
    Expect.AreEqual(to.TickCount, 1);
  }

  [Test]
  private void BoardedPawns()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);
    using ScopedMethodHook smhAerialVehicle =
      new(AerialVehicleUpdateRateTicks, postfix: new HarmonyMethod(OverrideMethod));
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

  [Test] // TODO
  [TestDescription("Aerial vehicle launch from and to caravans ticks at the correct rate for the inner vehicle.")]
  private void CaravanSkippingVehicle()
  {
  }

  [Test] // TODO
  [TestDescription("Aerial vehicle launch from and to caravans ticks at the correct rate for all pawns onboard.")]
  private void CaravanSkippingBoardedPawns()
  {
  }

  [Test] // TODO
  [TestDescription("Skyfaller leaving launch ticks at the correct rate for the inner vehicle.")]
  private void SkyfallerExitVehicle()
  {
  }

  [Test] // TODO
  [TestDescription("Skyfaller leaving launch ticks at the correct rate for all pawns onboard.")]
  private void SkyfallerExitBoardedPawns()
  {
  }
}