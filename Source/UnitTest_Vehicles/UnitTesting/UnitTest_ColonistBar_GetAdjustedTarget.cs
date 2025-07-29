using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.UnitTesting;

// Validation of vehicle functionality needs to occur before
[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Caravaning, TestCategoryNames.WorldObject)]
[TestDescription("ColonistBar behavior and all logic surrounding icon recaching and target jumping.")]
internal sealed class UnitTest_ColonistBar_GetAdjustedTarget
{
  [Test]
  private void Vehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    GlobalTargetInfo target = CameraJumper.GetAdjustedTarget(group.pawns[0]);
    Expect.AreEqual(target, group.vehicle);

    Find.Selector.ClearSelection();
    CameraJumper.TrySelect(group.pawns[0]);
    Expect.ReferencesAreEqual(Find.Selector.SingleSelectedThing, group.vehicle);
    Find.Selector.ClearSelection();
  }

  [Test]
  private void VehicleCaravan()
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
    using ScopeWorldObject swo = new(caravan);

    GlobalTargetInfo target = CameraJumper.GetAdjustedTarget(group.pawns[0]);
    Expect.AreEqual(target, caravan);

    Find.WorldSelector.ClearSelection();
    CameraJumper.TrySelect(group.pawns[0]);
    Assert.IsTrue(caravan.SelectableNow);
    Expect.ReferencesAreEqual(Find.WorldSelector.SingleSelectedObject, caravan);
    Find.WorldSelector.ClearSelection();
  }

  [Test]
  private void AerialVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.BoardAll();
    AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(group.vehicle, 0);
    Assert.IsNotNull(aerialVehicle);
    using ScopeWorldObject swo = new(aerialVehicle);

    GlobalTargetInfo target = CameraJumper.GetAdjustedTarget(group.pawns[0]);
    Expect.AreEqual(target, aerialVehicle);

    Find.WorldSelector.ClearSelection();
    CameraJumper.TrySelect(group.pawns[0]);
    Assert.IsTrue(aerialVehicle.SelectableNow);
    Expect.ReferencesAreEqual(Find.WorldSelector.SingleSelectedObject, aerialVehicle);
    Find.WorldSelector.ClearSelection();
  }

  [Test]
  private void VehicleSkyfaller_Arriving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    GlobalTargetInfo target = CameraJumper.GetAdjustedTarget(group.pawns[0]);
    Expect.AreEqual(target, group.vehicle);
  }

  [Test]
  private void VehicleSkyfaller_Leaving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.Spawn();

    GlobalTargetInfo target = CameraJumper.GetAdjustedTarget(group.pawns[0]);
    Expect.AreEqual(target, group.vehicle);
  }
}