using DevTools.Testing;
using RimWorld;
using UnityEngine.Assertions;
using Vehicles.World;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.Caravaning,
  TestCategoryNames.WorldObject,
  TestCategoryNames.VehiclePawn
)]
[Disabled]
[TestDescription("WorldTargeter for AerialVehicle, VehicleCaravan, and CompVehicleLauncher.")]
internal sealed class Test_WorldTargeter
{
  [Test]
  private void LaunchFromCaravan()
  {
    const int StartTile = 1;
    const int DestTile = 2;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(GetLauncherSettings());
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

  private static VehicleGroup.MockSettings GetLauncherSettings()
  {
    var settings = new VehicleGroup.MockSettings
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
    settings.vehicleDef = VehicleGroup.CreateVehicleDef(settings);
    return settings;
  }
}