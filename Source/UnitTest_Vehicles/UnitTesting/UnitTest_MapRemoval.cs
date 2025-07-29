using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(
  TestCategoryNames.VehiclePermissions,
  TestCategoryNames.VehiclePawn, TestCategoryNames.WorldObject)]
internal abstract class UnitTest_MapRemoval<T> where T : MapParent
{
  private const int DefaultMapSize = 50;

  protected T mapParent;

  protected abstract WorldObjectDef WorldObjectDef { get; }

  protected virtual Faction Faction => Faction.OfPlayer;

  protected virtual void PostGenerateMap()
  {
  }

  [SetUp]
  protected void GenerateMap()
  {
    using GenStepWarningDisabler gswd = new();

    PlanetTile tile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface, Faction);
    Assert.IsTrue(tile.Valid);
    Assert.IsNull(mapParent);
    Assert.IsNotNull(WorldObjectDef);
    mapParent = (T)WorldObjectMaker.MakeWorldObject(WorldObjectDef);
    mapParent.Tile = tile;
    mapParent.SetFaction(Faction);
    Find.WorldObjects.Add(mapParent);
    Map map = MapGenerator.GenerateMap(new IntVec3(DefaultMapSize, 1, DefaultMapSize), mapParent,
      mapParent.MapGeneratorDef);
    CameraJumper.TryJump(map.Center, map);
    PostGenerateMap();
  }

  [TearDown]
  protected void RemoveMap()
  {
    if (mapParent?.Map is { Disposed: false })
    {
      Map map = mapParent.Map;
      Current.Game.DeinitAndRemoveMap(map, false);
      mapParent.Destroy();
      Assert.IsTrue(map is { Disposed: true });
      Assert.IsTrue(mapParent is { Destroyed: true });
      mapParent = null;
    }
  }

  [Test]
  protected void ManualWithPassengers()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1
    });
    group.Spawn();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
    group.DisembarkAll();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void ManualWithAnimals()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1
    });
    group.Spawn();
    Expect.IsTrue(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void ManualEmpty()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile
    });
    group.Spawn();
    Assert.IsTrue(group.pawns.NullOrEmpty());
    Expect.IsTrue(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AutonomousWithPassengers()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile | VehiclePermissions.Autonomous,
      passengers = 1
    });
    group.Spawn();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
    group.DisembarkAll();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AutonomousWithAnimals()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile | VehiclePermissions.Autonomous,
      animals = 1
    });
    group.Spawn();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AutonomousEmpty()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile | VehiclePermissions.Autonomous
    });
    group.Spawn();
    Assert.IsTrue(group.pawns.NullOrEmpty());
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AerialWithPassengers()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.Spawn();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
    group.DisembarkAll();
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AerialWithAnimals()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      animals = 1,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.Spawn();
    Expect.IsTrue(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void AerialEmpty()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.Spawn();
    Assert.IsTrue(group.pawns.NullOrEmpty());
    Expect.IsTrue(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void VehicleSkyfallerArriving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.BoardAll();
    VehicleSkyfaller_Arriving skyfaller =
      (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(
        group.vehicle.CompVehicleLauncher.Props.skyfallerIncoming, group.vehicle);
    Assert.IsNotNull(skyfaller);
    using ScopeEntity se = new(skyfaller);
    GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void VehicleSkyfallerLeaving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.BoardAll();
    VehicleSkyfaller_Leaving skyfaller =
      (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(
        group.vehicle.CompVehicleLauncher.Props.skyfallerLeaving, group.vehicle);
    Assert.IsNotNull(skyfaller);
    using ScopeEntity se = new(skyfaller);
    GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }

  [Test]
  protected void VehicleSkyfallerCrashing()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      passengers = 1,
      comps = [CompGenerator.CompPropertiesVehicleLauncher]
    });
    group.BoardAll();
    VehicleSkyfaller_Crashing skyfaller =
      (VehicleSkyfaller_Crashing)VehicleSkyfallerMaker.MakeSkyfaller(
        group.vehicle.CompVehicleLauncher.Props.skyfallerCrashing, group.vehicle);
    Assert.IsNotNull(skyfaller);
    using ScopeEntity se = new(skyfaller);
    GenSpawn.Spawn(skyfaller, Find.CurrentMap.Center, Find.CurrentMap, Rot4.North);
    Expect.IsFalse(mapParent.ShouldRemoveMapNow(out _));
  }
}