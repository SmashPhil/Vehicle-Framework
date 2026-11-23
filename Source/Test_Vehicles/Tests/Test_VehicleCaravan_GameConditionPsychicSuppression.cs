using System.Linq;
using DevTools.Testing;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[LoadIfRoyaltyActive]
[TestCategory(
  TestCategoryNames.TickBehavior,
  TestCategoryNames.WorldObject,
  TestCategoryNames.Caravaning
)]
[TestDescription("Psychic suppression affecting pawns in VehicleCaravans.")]
internal sealed class Test_VehicleCaravan_GameConditionPsychicSuppression
{
  private VehicleGroup group;
  private VehicleCaravan caravan;
  private CompCauseGameCondition_PsychicSuppression suppressor;

  [SetUp]
  private void CreateTestCaravanAndSuppressor()
  {
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1
    });
    group.BoardAll();
    caravan = CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
    Assert.IsNotNull(caravan);

    Pawn pawn = group.pawns[0];
    Assert.IsTrue(pawn.gender is Gender.Male or Gender.Female);
    suppressor = new CompCauseGameCondition_PsychicSuppression
    {
      parent = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("PsychicSuppressor"))
    };
    suppressor.Initialize(new CompProperties_CausesGameCondition
    {
      conditionDef = GameConditionDefOf.PsychicSuppression,
      worldRange = 10
    });
    suppressor.gender = pawn.gender;
    suppressor.LinkWithSite(new Site { Tile = caravan.Tile });
    Assert.IsTrue(suppressor.Active);
  }

  [TearDown]
  private void DisposeTestCaravanAndSuppressor()
  {
    if (suppressor != null)
    {
      foreach (GameCondition condition in suppressor.CausedConditions.ToList())
      {
        condition.suppressEndMessage = true;
        condition.End();
      }
    }
    if (caravan is { Destroyed: false })
    {
      caravan.RemoveAllPawns();
      caravan.Destroy();
    }
    group?.Dispose();
  }

  [Test]
  private void PsychicSuppressionBoardedPawn()
  {
    Pawn pawn = group.pawns[0];
    Assert.IsTrue(pawn.InVehicle());
    Assert.IsTrue(caravan.ContainsPawn(pawn));
    Assert.IsFalse(pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicSuppression));

    suppressor.CompTick();

    Expect.IsTrue(pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicSuppression));
  }

  [Test]
  private void PsychicSuppressionDismountedPawn()
  {
    group.DisembarkAll();
    Pawn pawn = group.pawns[0];
    Assert.IsTrue(pawn.InVehicleCaravan());
    Assert.IsFalse(pawn.InVehicle());
    Assert.IsFalse(pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicSuppression));

    suppressor.CompTick();

    Expect.IsTrue(pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicSuppression));
  }
}
