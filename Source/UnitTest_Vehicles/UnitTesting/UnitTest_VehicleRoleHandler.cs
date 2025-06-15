using System.Linq;
using DevTools.UnitTesting;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using Verse;
using DescriptionAttribute = DevTools.UnitTesting.TestDescriptionAttribute;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.TickBehavior)]
[Description("VehicleRoleHandler behavior and all logic surrounding board and disembark.")]
internal sealed class UnitTest_VehicleRoleHandler : UnitTest_MapTest
{
  protected override bool ShouldTest(VehicleDef vehicleDef)
  {
    return vehicleDef.properties.roles.NotNullAndAny(role => role.Slots > 0);
  }

  [Test]
  private void BoardingUnboarding()
  {
    foreach (VehiclePawn vehicle in vehicles)
    {
      using VehicleTestCase vtc = new(vehicle, this);

      GenSpawn.Spawn(vehicle, root, map);
      Assert.IsTrue(vehicle.Spawned);

      // Colonists can board
      int total = vehicle.SeatsAvailable;
      for (int i = 0; i < total; i++)
      {
        Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        Assert.IsNotNull(colonist);
        Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
        Expect.IsTrue(vehicle.TryAddPawn(colonist), $"Boarded {i + 1}/{total}");
      }

      // Colonist cannot board full vehicle
      Pawn failColonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
      Assert.IsNotNull(failColonist);
      Assert.AreEqual(failColonist.Faction, Faction.OfPlayer);
      Expect.IsFalse(vehicle.TryAddPawn(failColonist), "Reject boarding (Full Capacity)");

      failColonist.Destroy();
      vehicle.DestroyPawns();

      Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
      Assert.IsNotNull(animal);
      Assert.AreEqual(animal.Faction, Faction.OfPlayer);
      Expect.IsTrue(vehicle.TryAddPawn(animal), "Boarded animal");

      vehicle.DestroyPawns();

      if (ModsConfig.BiotechActive)
      {
        Pawn mechanoid =
          PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, Faction.OfPlayer);
        Assert.IsNotNull(mechanoid);
        Assert.AreEqual(mechanoid.Faction, Faction.OfPlayer);
        Expect.IsTrue(vehicle.TryAddPawn(mechanoid), "Boarded mech");
      }
    }
  }

  [Test]
  private void RoleTicking()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });

    group.Spawn();

    // Vehicle parent
    {
      using TickObserver<VehiclePawn> observer = new(group.vehicle);
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }

    // Internal roles
    {
      Pawn pawn = group.pawns.First();
      Assert.IsFalse(pawn.Spawned);
      Assert.IsTrue(pawn.IsInVehicle());
      using TickObserver<Pawn> observer = new(pawn);
      Find.TickManager.DoSingleTick();
      Expect.AreEqual(observer.TickCount, 1);
    }
  }
}