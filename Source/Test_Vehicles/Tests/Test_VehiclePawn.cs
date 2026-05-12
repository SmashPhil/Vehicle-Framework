using System.Linq;
using DevTools.Testing;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn)]
internal sealed class Test_VehiclePawn
{
	[Test]
	private void VehicleRoleParent()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			drivers = 1
		});
		group.Spawn();

		Pawn pawn = group.pawns.FirstOrDefault();
		Assert.IsTrue(pawn.InVehicle());
		Assert.IsTrue(pawn.ParentHolder is VehicleRoleHandler);
		Thing firstParentThing = ThingOwnerUtility.GetFirstParentThing(pawn);
		Expect.ReferencesAreEqual(group.vehicle, firstParentThing);
	}

	[Test]
	private void VehicleInventoryParent()
	{
		using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
		{
			permissions = VehiclePermissions.Mobile,
			animals = 1
		});
		group.Spawn();

		Pawn pawn = group.pawns.FirstOrDefault();
		Assert.IsTrue(pawn.InVehicle());
		Assert.IsTrue(pawn.ParentHolder is Pawn_InventoryTracker { pawn: VehiclePawn });
		Thing firstParentThing = ThingOwnerUtility.GetFirstParentThing(pawn);
		Expect.ReferencesAreEqual(group.vehicle, firstParentThing);
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