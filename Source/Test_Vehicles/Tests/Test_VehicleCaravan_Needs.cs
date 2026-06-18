using System;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Vehicles.Compatibility;
using Vehicles.World;
using Verse;

namespace Vehicles.Testing;

[TestFixture(TestType.Playing)]
[TestCategory(
  TestCategoryNames.TickBehavior,
  TestCategoryNames.WorldObject,
  TestCategoryNames.WorldPawnGC,
  TestCategoryNames.Caravaning
)]
[TestDescription("VehicleCaravan needs mechanics on the world map.")]
internal sealed class Test_VehicleCaravan_Needs
{
  private const int WorldUpdateTicksForPawn = 15;
  private static readonly Action<Pawn, ChemicalDef> ApplyAddiction;
  private static readonly AccessTools.FieldRef<Need_Rest, int> LastRestTickFieldRef;

  private VehicleGroup group;
  private VehicleCaravan caravan;

  static Test_VehicleCaravan_Needs()
  {
    MethodInfo method = AccessTools.Method(typeof(PawnAddictionHediffsGenerator), "ApplyAddiction");
    Assert.IsNotNull(method);
    ApplyAddiction = AccessTools.MethodDelegate<Action<Pawn, ChemicalDef>>(method);
    LastRestTickFieldRef = AccessTools.FieldRefAccess<int>(typeof(Need_Rest), "lastRestTick");
    Assert.IsNotNull(LastRestTickFieldRef);
  }

  [SetUp]
  private void CreateTestGroup()
  {
    Assert.IsTrue(CameraJumper.TryShowWorld());
    Assert.IsNull(group);
    group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile,
      drivers = 1,
      // Give plenty of room for test cases without generating additional colonists
      extraSlots = 999
    });

    group.BoardAll();
    caravan = CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, 1, true);
  }

  [TearDown]
  private void DisposeTestGroup()
  {
    caravan.RemoveAllPawns();
    group?.Dispose();
    group = null;
  }

  [Test]
  private void RestNeeds()
  {
    const float FullSleepHours = 10.5f;
    const float FullSleepTicks = GenDate.TicksPerHour * FullSleepHours;
    // Rest is incremented/decremented once every 150 ticks with no apparent constant
    const float RatePerTick = 150 / FullSleepTicks;

    Pawn restPawn = group.pawns.FirstOrDefault();
    Assert.IsNotNull(restPawn);
    Assert.AreEqual(restPawn.UpdateRateTicks, WorldUpdateTicksForPawn);
    Assert.IsFalse(caravan.pather.MovingNow);
    Assert.IsFalse(restPawn.InCaravanBed());
    Assert.IsFalse(restPawn.CarriedByCaravan());
    Assert.IsTrue(restPawn.needs.TryGetNeed(out Need_Rest need));
    need.CurLevel = 0;
    Assert.AreApproximatelyEqual(need.CurLevel, 0);

    float restEffectiveness = StatDefOf.BedRestEffectiveness.valueIfMissing;
    float restMultiplier = restPawn.GetStatValue(StatDefOf.RestRateMultiplier);
    float expected = restEffectiveness * restMultiplier * RatePerTick;
    // Update lastRestEffectiveness
    need.TickResting(restEffectiveness);
    // Force enable Resting flag by setting lastRestTick to max tick delta.
    LastRestTickFieldRef.Invoke(need) = WorldUpdateTicksForPawn;
    Assert.IsTrue(need.Resting);
    need.NeedInterval();
    Assert.AreApproximatelyEqual(need.CurLevel, expected);

    // TODO - resting disabled while moving
  }

  [Test]
  private void FoodNeeds()
  {
    Pawn pawn = group.pawns.FirstOrDefault();
    Assert.IsNotNull(pawn);
    Assert.AreEqual(pawn.UpdateRateTicks, WorldUpdateTicksForPawn);
    Assert.IsTrue(pawn.needs.TryGetNeed(out Need_Food need));

    ThingWithComps food = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
    food.stackCount = 1;
    Assert.IsNotNull(food.def.ingestible);
    group.vehicle.inventory.TryAddAndUnforbid(food);
    Assert.AreEqual(food.ParentHolder, group.vehicle.inventory);

    need.CurLevel = 0;
    Assert.AreApproximatelyEqual(need.CurLevel, 0);
    Assert.AreApproximatelyEqual(need.NutritionWanted, 1);
    float ingested = FoodUtility.NutritionForEater(pawn, food);
    caravan.needs.NeedsTrackerTickInterval(1);
    Assert.AreApproximatelyEqual(need.CurLevel, ingested);
  }

  [Test]
  private void ChemicalNeeds()
  {
    // Disable drug desire as it can set a negative degree (Teetotaler) and disable drug addictions
    Pawn addict = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist,
      Faction.OfPlayer, prohibitedTraits: [TraitDefOf.DrugDesire], fixedBiologicalAge: 30));
    Assert.IsNotNull(addict);
    Assert.AreEqual(addict.Faction, Faction.OfPlayer);
    ApplyAddiction(addict, ChemicalDefOf.Alcohol);
    Assert.IsTrue(
      addict.health.hediffSet.TryGetHediff(ChemicalDefOf.Alcohol.addictionHediff, out _));
    group.pawns.Add(addict);
    group.BoardAll();

    Assert.IsNotNull(addict);
    Assert.AreEqual(addict.UpdateRateTicks, WorldUpdateTicksForPawn);
    Assert.IsTrue(addict.needs.TryGetNeed(out Need_Chemical need));
    Assert.IsNotNull(need);
    Assert.IsNotNull(need.AddictionHediff);
    if (addict.drugs.CurrentPolicy != null)
    {
      addict.drugs.CurrentPolicy[ThingDefOf.Beer].allowScheduled = true;
      addict.drugs.CurrentPolicy[ThingDefOf.Beer].allowedForAddiction = true;
      addict.drugs.CurrentPolicy[ThingDefOf.Beer].allowedForJoy = true;
    }

    ThingWithComps beer = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.Beer);
    beer.stackCount = 1;
    Assert.IsNotNull(beer.def.ingestible);
    CompDrug compDrug = beer.TryGetComp<CompDrug>();
    Assert.IsNotNull(compDrug);
    Assert.IsNotNull(compDrug.Props);
    Assert.IsTrue(group.vehicle.inventory.innerContainer.TryAdd(beer));
    Assert.AreEqual(beer.ParentHolder, group.vehicle.inventory);

    need.CurLevel = 0;
    Assert.AreApproximatelyEqual(need.CurLevel, 0);
    Assert.IsTrue(need.CurCategory < DrugDesireCategory.Satisfied);
    Assert.IsTrue(CaravanInventoryUtility.TryGetDrugToSatisfyChemicalNeed(caravan, addict,
      need.AddictionHediff, out Thing drug, out _));
    Expect.ReferencesAreEqual(beer, drug);
    caravan.needs.NeedsTrackerTickInterval(1);
    Expect.IsTrue(beer.Destroyed); // Beer was consumed
    Expect.IsTrue(addict.health.hediffSet.TryGetHediff(HediffDefOf.AlcoholHigh, out _));

    // 'Consume' pawn so we don't reuse them
    group.vehicle.DisembarkPawn(addict);
    caravan.RemovePawn(addict);
    group.pawns.Remove(addict);
    addict.Destroy();
  }

  [Test, LoadIfBiotechActive]
  private void ChemicalDependency()
  {
    Pawn addict = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
      PawnKindDefOf.Colonist,
      Faction.OfPlayer, prohibitedTraits: [TraitDefOf.DrugDesire], fixedBiologicalAge: 30));
    Assert.IsNotNull(addict);
    Assert.AreEqual(addict.Faction, Faction.OfPlayer);
    Hediff_ChemicalDependency addictionHediff =
      (Hediff_ChemicalDependency)addict.health.AddHediff(HediffDefOf.GeneticDrugNeed);
    Assert.IsNotNull(addictionHediff);
    addictionHediff.chemical = ChemicalDefOf.Alcohol;
    group.pawns.Add(addict);
    group.BoardAll();

    ThingWithComps beer = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.Beer);
    beer.stackCount = 1;
    Assert.IsNotNull(beer.def.ingestible);
    CompDrug compDrug = beer.TryGetComp<CompDrug>();
    Assert.IsNotNull(compDrug);
    Assert.IsNotNull(compDrug.Props);
    Assert.IsTrue(group.vehicle.inventory.innerContainer.TryAdd(beer));
    Assert.AreEqual(beer.ParentHolder, group.vehicle.inventory);

    GeneDef alcoholDependency = DefDatabase<GeneDef>.GetNamed("ChemicalDependency_Alcohol");
    Assert.IsNotNull(alcoholDependency);
    addict.genes.AddGene(alcoholDependency, false);
    Hediff_ChemicalDependency chemicalDependency =
      addict.health.hediffSet.GetFirstHediff<Hediff_ChemicalDependency>();
    Assert.IsNotNull(chemicalDependency);
    Assert.AreEqual(addict.UpdateRateTicks, WorldUpdateTicksForPawn);

    // ShouldSatify = minSeverity - 0.1, set slightly higher so it's guaranteed to be true
    float minSeverity = chemicalDependency.def.stages[2].minSeverity;
    chemicalDependency.Severity = minSeverity;
    caravan.needs.NeedsTrackerTickInterval(1);
    Expect.IsTrue(addict.health.hediffSet.TryGetHediff(HediffDefOf.AlcoholHigh, out _));

    // 'Consume' pawn so we don't reuse them
    group.vehicle.DisembarkPawn(addict);
    caravan.RemovePawn(addict);
    group.pawns.Remove(addict);
    addict.Destroy();
  }

	[Test]
  private void JoyNeeds()
  {
    const int JoyTickInterval = 1250;
    const float JoyGain = 4E-05f * JoyTickInterval;

    Pawn pawn = group.pawns.FirstOrDefault();
    Assert.IsNotNull(pawn);
    Assert.AreEqual(pawn.UpdateRateTicks, WorldUpdateTicksForPawn);
    Assert.IsTrue(pawn.needs.TryGetNeed(out Need_Joy need));
    Assert.IsFalse(caravan.pather.MovingNow);
    Assert.IsFalse(caravan.vehiclePather.MovingNow);

    need.CurLevel = 0;
    Assert.AreApproximatelyEqual(need.CurLevel, 0);
    Assert.IsFalse(pawn.needs.joy.tolerances.BoredOf(JoyKindDefOf.Meditative));
    caravan.needs.NeedsTrackerTickInterval(JoyTickInterval);
    Expect.AreApproximatelyEqual(need.CurLevel, JoyGain);
  }

  [Test, LoadIfRoyaltyActive]
  private void PsyfocusNeeds()
  {
  }

  [Test, LoadIfBiotechActive]
  private void HemogenNeeds()
  {
  }

  [Test, LoadIfModsActive(ModPackageIds.DubsBadHygiene)]
  private void WaterNeeds()
  {
  }

  [Test, LoadIfModsActive(ModPackageIds.DubsBadHygiene)]
  private void HygieneNeeds()
  {
  }

  [Test, LoadIfModsActive(ModPackageIds.DubsBadHygiene)]
  private void BladderNeeds()
  {
  }
}