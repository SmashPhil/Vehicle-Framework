using DevTools.UnitTesting;
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
  TestCategoryNames.WorldPawnGC,
  TestCategoryNames.Caravaning
)]
internal sealed class UnitTest_AerialVehicle
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

  [Test, ExecutionPriority(Priority.First)]
  private void Init()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();
    VehicleWorldObjectsHolder holder = Find.World.GetComponent<VehicleWorldObjectsHolder>();
    Assert.IsNotNull(holder);
    Assert.IsTrue(holder.AerialVehicles.Count == 0);

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    using ScopeWorldObject swo = new(aerialVehicle);
    Expect.IsFalse(group.vehicle.Destroyed, "Vehicle destroyed.");
    Expect.IsFalse(group.vehicle.Discarded, "Vehicle discarded.");
  }

  [Test]
  private void CaravanConversion()
  {
    const int StartTile = 1;
    const int DestTile = 2;

    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, StartTile, true);
    using ScopeWorldObject swo = new(caravan);
    group.vehicle.CompVehicleLauncher.inFlight = true;
    AerialVehicleInFlight aerialVehicle =
      AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(group.vehicle);
    aerialVehicle.recon = false;
    aerialVehicle.OrderFlyToTiles([new FlightNode(DestTile)],
      Find.WorldGrid.GetTileCenter(DestTile),
      arrivalAction: new AerialVehicleArrivalAction_FormVehicleCaravan(group.vehicle));
    Assert.IsTrue(group.vehicle.CompVehicleLauncher.inFlight);
    Assert.IsTrue(caravan.Destroyed);
    Assert.IsFalse(group.vehicle.Destroyed);
    Assert.IsTrue(ReferenceEquals(aerialVehicle, group.vehicle.GetAerialVehicle()));
    using ScopeWorldObject swoAerial = new(aerialVehicle);
    Assert.IsNotNull(aerialVehicle);
    aerialVehicle.LandAtTile(DestTile);
    Assert.IsTrue(aerialVehicle.Destroyed);
    Assert.IsFalse(group.vehicle.Destroyed);
    Assert.IsFalse(group.vehicle.CompVehicleLauncher.inFlight);
    VehicleCaravan newCaravan = group.vehicle.GetVehicleCaravan();
    Assert.IsNotNull(newCaravan);
    using ScopeWorldObject swoNew = new(newCaravan);
  }

  [Test]
  private void WorldPawnGC()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();

    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 1);
    VehiclePawn vehicle = aerialVehicle.vehicle;
    // Pass vehicle and passengers to world
    Find.WorldPawns.PassToWorld(vehicle);
    foreach (Pawn pawn in vehicle.AllPawnsAboard)
    {
      Expect.IsFalse(pawn.Destroyed, "Passenger destroyed.");
      Expect.IsFalse(pawn.Discarded, "Passenger discarded.");
      if (!pawn.IsWorldPawn())
      {
        Find.WorldPawns.PassToWorld(pawn);
      }
    }
    // Pass inventory pawns to world
    foreach (Thing thing in vehicle.inventory.innerContainer)
    {
      if (thing is Pawn pawn && !pawn.IsWorldPawn())
      {
        Expect.IsFalse(pawn.Destroyed, "Inventory pawn destroyed.");
        Expect.IsFalse(pawn.Discarded, "Inventory pawn discarded.");
        Find.WorldPawns.PassToWorld(pawn);
      }
    }
    Expect.ReferencesAreEqual(vehicle.ParentHolder, aerialVehicle, "Vehicle ParentHolder");
    Expect.All(vehicle.AllPawnsAboard,
      pawn => pawn.ParentHolder is VehicleRoleHandler handler && handler.vehicle == vehicle,
      "Passenger ParentHolder");
    Expect.All(vehicle.inventory.innerContainer, pawn => ThingInVehicle(vehicle, pawn),
      "Inventory pawn ParentHolder");

    Find.WorldPawns.gc.CancelGCPass();
    _ = Find.WorldPawns.gc.PawnGCPass();

    Find.WorldPawns.gc.PawnGCDebugResults();
    Expect.IsFalse(vehicle.Destroyed, "Vehicle GC destroyed.");
    Expect.IsFalse(vehicle.Discarded, "Vehicle GC discarded.");
    Expect.None(vehicle.AllPawnsAboard, pawn => pawn.Destroyed, "Passenger GC destroyed.");
    Expect.None(vehicle.AllPawnsAboard, pawn => pawn.Discarded, "Passenger GC discarded.");
    Expect.None(vehicle.inventory.innerContainer, thing => thing.Destroyed,
      "Inventory GC destroyed.");
    Expect.None(vehicle.inventory.innerContainer, thing => thing.Discarded,
      "Inventory GC discarded.");

    aerialVehicle.Destroy();
    Expect.IsFalse(Find.WorldPawns.Contains(aerialVehicle.vehicle));
    return;

    static bool ThingInVehicle(VehiclePawn vehicle, Thing thing)
    {
      if (thing is Pawn pawn)
      {
        return pawn.ParentHolder is Pawn_InventoryTracker inventoryTracker &&
          inventoryTracker.pawn == vehicle;
      }
      return thing.ParentHolder == vehicle.inventory;
    }
  }

  [Test]
  private void CrashEvent()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(mockSettings);
    group.BoardAll();

    PlanetTile tile =
      Find.WorldGrid.Tiles.RandomElementByWeight(TileWeight)?.tile ?? PlanetTile.Invalid;
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, tile);
    aerialVehicle.InitiateCrashEvent();
    Assert.IsTrue(aerialVehicle.Destroyed);
    CrashSite site = Find.WorldObjects.WorldObjectAt<CrashSite>(tile);
    Assert.IsNotNull(site);
    return;

    float TileWeight(SurfaceTile surfaceTile)
    {
      return Find.WorldPathGrid.PassableFast(surfaceTile.tile) ? 1 : 0;
    }
  }
}