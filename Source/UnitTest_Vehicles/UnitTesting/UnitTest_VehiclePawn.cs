using DevTools.UnitTesting;
using RimWorld;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn)]
internal sealed class UnitTest_VehiclePawn
{
  [Test]
  private void Destroy()
  {
    VehicleDef vehicleDef =
      TestDefGenerator.CreateTransientVehicleDef("VehicleDef_ForDestruction");
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
    Assert.IsTrue(colonist.IsInVehicle());
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