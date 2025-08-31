using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
  TestCategoryNames.Caravaning,
  TestCategoryNames.WorldObject,
  TestCategoryNames.VehiclePawn
)]
[Disabled]
[TestDescription("WorldTargeter for AerialVehicle, VehicleCaravan, and CompVehicleLauncher.")]
internal sealed class UnitTest_WorldTargeter
{
  private VehicleGroup.MockSettings mockSettings;

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

  [TearDown]
  private void RemoveSettings()
  {
    mockSettings = null;
  }

  [Test]
  private void LaunchFromCaravan()
  {
    const int StartTile = 1;
    const int DestTile = 2;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, StartTile, true);
    using ScopeWorldObject swo = new(caravan);
    group.vehicle.CompVehicleLauncher.inFlight = true;
    AerialVehicleInFlight aerialVehicle = group.vehicle.GetOrMakeAerialVehicle();
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject sav = new(aerialVehicle);
    aerialVehicle.recon = false;
    aerialVehicle.OrderFlyToTiles([new FlightNode(DestTile)], new ArrivalAction_LandToCaravan(group.vehicle));
    Assert.IsTrue(group.vehicle.CompVehicleLauncher.inFlight);
    Assert.IsTrue(caravan.Destroyed);
    Assert.IsFalse(group.vehicle.Destroyed);
    Assert.IsTrue(ReferenceEquals(aerialVehicle, group.vehicle.GetAerialVehicle()));
    using ScopeWorldObject swoAerial = new(aerialVehicle);
    Assert.IsNotNull(aerialVehicle);
    aerialVehicle.ArriveAtTile(DestTile);
    Assert.IsTrue(aerialVehicle.Destroyed);
    Assert.IsFalse(group.vehicle.Destroyed);
    Assert.IsFalse(group.vehicle.CompVehicleLauncher.inFlight);
    VehicleCaravan newCaravan = group.vehicle.GetVehicleCaravan();
    Assert.IsNotNull(newCaravan);
    using ScopeWorldObject swoNew = new(newCaravan);
  }
}