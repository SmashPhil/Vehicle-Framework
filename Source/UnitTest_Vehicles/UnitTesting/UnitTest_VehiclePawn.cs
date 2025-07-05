using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn)]
internal sealed class UnitTest_VehiclePawn
{
  [Test]
  private void OrderGoto()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    group.Spawn();
    group.vehicle.ignition.Drafted = true;
    Assert.IsTrue(group.vehicle.Drafted);
    Assert.IsTrue(group.vehicle.CanMoveFinal);
    IntVec3 gotoLoc = group.vehicle.Position + new IntVec3(0, 0, 1);
    Assert.IsTrue(gotoLoc.Walkable(group.vehicle.VehicleDef, group.vehicle.Map));
    FloatMenuOptionProvider_OrderVehicle.PawnGotoAction(gotoLoc, group.vehicle, gotoLoc,
      Rot8.North);
    Assert.IsTrue(group.vehicle.jobs.curJob.def == JobDefOf.Goto);
  }

  [Test]
  private void SpawnDestroy()
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef("VehicleDef_ForDestruction", null);
    Assert.IsNotNull(vehicleDef);

    vehicleDef.properties.roles =
    [
      new VehicleRole
      {
        key = "Driver",
        slots = 1,
        slotsToOperate = 1,

        handlingTypes = HandlingType.Movement
      }
    ];
    VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(vehicleDef, Faction.OfPlayer);
    Assert.IsNotNull(vehicle);
    Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    Assert.IsNotNull(colonist);
    Assert.IsTrue(vehicle.TryAddPawn(colonist));
    Assert.IsTrue(colonist.InVehicle());
    Assert.IsTrue(vehicle.AllPawnsAboard.Contains(colonist));

    TestUtils.ForceSpawn(vehicle);
    Assert.IsTrue(vehicle.Spawned);

    vehicle.Destroy();
    Assert.IsTrue(vehicle.Destroyed);
    Expect.IsTrue(vehicle.Discarded);
    // Colonist is ejected out of the vehicle
    Expect.IsFalse(colonist.Destroyed);
    Expect.IsFalse(colonist.Discarded);
    Expect.IsTrue(colonist.Spawned);
    Expect.IsFalse(Find.WorldPawns.Contains(vehicle));

    colonist.Destroy();
    Assert.IsTrue(colonist.Destroyed);
  }
}