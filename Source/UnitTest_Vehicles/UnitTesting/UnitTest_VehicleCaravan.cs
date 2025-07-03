using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
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
[TestDescription("VehicleCaravan mechanics on the world map.")]
internal sealed class UnitTest_VehicleCaravan
{
  private static readonly MethodInfo MergeCaravansMethod =
    AccessTools.Method(typeof(CaravanMergeUtility), "MergeCaravans");

  [Test]
  private void GetCaravan()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    Assert.AreEqual(group.pawns.Count, 1);

    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo = new(vehicleCaravan);
    Assert.AreEqual(vehicleCaravan, group.vehicle.GetVehicleCaravan());
    Assert.AreEqual(vehicleCaravan, group.pawns[0].GetVehicleCaravan());
    Expect.AreEqual(vehicleCaravan, group.vehicle.GetCaravan());
    Assert.AreEqual(vehicleCaravan, group.pawns[0].GetCaravan());
  }

  [Test]
  private void VanillaVisibility()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 5,
      properties = new VehicleProperties
      {
        visibilityWeight = 6
      }
    });
    Assert.AreEqual(group.pawns.Count, 6);

    // Base game caravans should behave as expected
    Caravan caravan = CaravanMaker.MakeCaravan(group.pawns, Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo = new(caravan);

    float visibility = CaravanVisibilityCalculator.Visibility(caravan);
    // weight = 6
    Assert.AreApproximatelyEqual(caravan.Visibility, CaravanVisibilityCalculator.NotMovingFactor);
    Assert.AreApproximatelyEqual(visibility, CaravanVisibilityCalculator.NotMovingFactor);

    // Remove group pawns first or else we'll be testing with destroyed group pawns.
    caravan.RemoveAllPawns();
    caravan.Destroy();
    Assert.IsTrue(caravan.Destroyed);
  }

  [Test]
  private void Visibility()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 5,
      properties = new VehicleProperties
      {
        visibilityWeight = 6
      }
    });
    Assert.AreEqual(group.pawns.Count, 6);

    group.BoardAll();
    VehicleCaravan vehicleCaravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo = new(vehicleCaravan);

    Assert.IsFalse(vehicleCaravan.vehiclePather.MovingNow);
    Assert.AreEqual(vehicleCaravan.pawns.Count, 1);
    Assert.AreEqual(vehicleCaravan.pawns[0], group.vehicle);
    float visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);

    // weight = 6
    Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
      1 * CaravanVisibilityCalculator.NotMovingFactor);
    Expect.AreApproximatelyEqual(visibility, 1 * CaravanVisibilityCalculator.NotMovingFactor);
    group.vehicle.DisembarkAll();
    Assert.AreEqual(vehicleCaravan.pawns.Count, 7);

    // weight = 12
    visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan);
    Expect.AreApproximatelyEqual(vehicleCaravan.Visibility,
      1.12f * CaravanVisibilityCalculator.NotMovingFactor);
    Expect.AreApproximatelyEqual(visibility, 1.12f * CaravanVisibilityCalculator.NotMovingFactor);

    // Moving
    visibility = CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    group.BoardAll();
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.pawns.InnerListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1);

    // Pawns inside vehicles (returned by getter) should not count in visibility
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1);

    // Visibility is capped at 112%
    group.vehicle.VehicleDef.properties.visibilityWeight = 999;
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
    group.DisembarkAll();
    visibility =
      CaravanVisibilityCalculator.Visibility(vehicleCaravan.PawnsListForReading, true);
    Expect.AreApproximatelyEqual(visibility, 1.12f);
  }

  [Test, Disabled] // TODO
  private void Moving()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
  }

  [Test, Disabled] // TODO
  private void MovingNow()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
  }

  [Test, Disabled] // TODO
  private void ShouldRest()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
  }

  [Test]
  [TestDescription("Appends pawns in vehicles to property for key caravan mechanics.")]
  private void PawnsListForReading()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      extraSlots = 999
    });

    group.BoardAll();

    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo = new(caravan);

    // 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
    Expect.AreEqual(caravan.pawns.Count, 1);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 2);

    Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
      Faction.OfPlayer, fixedBiologicalAge: 30));
    using ScopeEntity se = new(colonist);
    Assert.IsNotNull(colonist);
    Assert.AreEqual(colonist.Faction, Faction.OfPlayer);

    // Adding pawn to caravan recaches
    // 1 vehicle, 1 onboard, 1 dismounted (2 in caravan, 1 implicit)
    caravan.AddPawn(colonist, true);
    Expect.AreEqual(caravan.pawns.Count, 2);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

    // Removing pawn to caravan recaches as well
    // 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
    caravan.RemovePawn(colonist);
    Expect.AreEqual(caravan.pawns.Count, 1);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 2);

    // Adding to vehicle directly recaches
    // 1 vehicle, 2 onboard (1 in caravan, 2 implicit)
    Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
    Expect.AreEqual(caravan.pawns.Count, 1);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

    // Disembarking from vehicle recaches
    // 1 vehicle, 1 onboard, 1 dismounted (2 in caravan, 1 implicit)
    group.vehicle.DisembarkPawn(colonist);
    Assert.IsFalse(colonist.InVehicle());
    Assert.IsTrue(colonist.InVehicleCaravan());
    Expect.AreEqual(caravan.pawns.Count, 2);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 3);

    // Removing from vehicle recaches and does NOT add them to caravan
    // 1 vehicle, 1 onboard (1 in caravan, 1 implicit)
    Assert.IsTrue(group.vehicle.TryAddPawn(colonist));
    Assert.IsTrue(colonist.InVehicle());
    Assert.IsTrue(colonist.InVehicleCaravan());
    group.vehicle.RemovePawn(colonist);
    Expect.AreEqual(caravan.pawns.Count, 1);
    Expect.AreEqual(caravan.PawnsListForReading.Count, 2);
    Expect.IsFalse(colonist.InVehicle());
    Expect.IsFalse(colonist.InVehicleCaravan());
  }

  [Test]
  private void SplitIntoVanillaCaravans()
  {
  }

  [Test]
  private void SplitIntoVehicleCaravans()
  {
  }

  [Test]
  private void SplitIntoMixedCaravans()
  {
  }

  [Test]
  private void MergeVanillaCaravans()
  {
    const int Caravans = 3;
    const int PawnsPerCaravan = 3;

    List<Caravan> caravans = [];
    for (int i = 0; i < Caravans; i++)
    {
      List<Pawn> pawns = [];
      for (int j = 0; j < PawnsPerCaravan; j++)
      {
        Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
          Faction.OfPlayer, fixedBiologicalAge: 30));
        Assert.IsNotNull(colonist);
        Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
        pawns.Add(colonist);
      }
      caravans.Add(CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, 1, true));
    }
    MergeCaravansMethod.Invoke(null, [caravans]);

    Caravan mergedCaravan = Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOf.Caravan) as Caravan;
    Assert.IsNotNull(mergedCaravan);

    mergedCaravan.Destroy();
    Assert.IsTrue(mergedCaravan.Destroyed);
  }

  [Test]
  private void MergeVehicleCaravans()
  {
    using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      animals = 1
    });
    using VehicleGroup group3 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 3
    });

    group1.BoardAll();
    group2.BoardAll();
    group3.BoardOne();

    VehicleCaravan vehicleCaravan1 =
      CaravanHelper.MakeVehicleCaravan([group1.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo1 = new(vehicleCaravan1);
    VehicleCaravan vehicleCaravan2 =
      CaravanHelper.MakeVehicleCaravan([group2.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo2 = new(vehicleCaravan2);
    VehicleCaravan vehicleCaravan3 =
      CaravanHelper.MakeVehicleCaravan(
        group3.pawns.Where(pawn => !pawn.InVehicle()).Concat(group3.vehicle), Faction.OfPlayer, 1,
        true);
    using ScopeWorldObject swo3 = new(vehicleCaravan3);
    List<Caravan> caravanList = [vehicleCaravan1, vehicleCaravan2, vehicleCaravan3];
    MergeCaravansMethod.Invoke(null, [caravanList]);

    VehicleCaravan mergedCaravan =
      Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOfVehicles.VehicleCaravan) as VehicleCaravan;
    Assert.IsNotNull(mergedCaravan);

    mergedCaravan.RemoveAllPawns();
    Assert.IsTrue(mergedCaravan.Destroyed);
  }

  [Test]
  private void MergeMixedCaravans()
  {
    const int PawnsInVanillaCaravan = 3;

    using VehicleGroup group1 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = 1
    });
    using VehicleGroup group2 = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      passengers = PawnsInVanillaCaravan
    });

    group1.BoardAll();
    group2.BoardOne();

    List<Pawn> pawns = [];
    for (int j = 0; j < PawnsInVanillaCaravan; j++)
    {
      Pawn colonist = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
        Faction.OfPlayer, fixedBiologicalAge: 30));
      Assert.IsNotNull(colonist);
      Assert.AreEqual(colonist.Faction, Faction.OfPlayer);
      pawns.Add(colonist);
    }
    Caravan caravan = CaravanMaker.MakeCaravan(pawns, Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo = new(caravan);

    // Testing all boarded, some boarded, and vanilla
    VehicleCaravan vehicleCaravan1 =
      CaravanHelper.MakeVehicleCaravan([group1.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo1 = new(vehicleCaravan1);
    VehicleCaravan vehicleCaravan2 =
      CaravanHelper.MakeVehicleCaravan([group2.vehicle], Faction.OfPlayer, 1, true);
    using ScopeWorldObject swo2 = new(vehicleCaravan2);
    List<Caravan> caravanList = [vehicleCaravan1, vehicleCaravan2, caravan];
    MergeCaravansMethod.Invoke(null, [caravanList]);

    VehicleCaravan mergedCaravan =
      Find.WorldObjects.WorldObjectAt(1, WorldObjectDefOfVehicles.VehicleCaravan) as VehicleCaravan;
    Assert.IsNotNull(mergedCaravan);

    mergedCaravan.RemoveAllPawns();
    Assert.IsTrue(mergedCaravan.Destroyed);
  }
}