using DevTools.Testing;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("VehiclePawn is blocked from ever triggering mental states or breaks.")]
internal sealed class Test_VehiclePawn_Mental
{
  [Test]
  private void MentalStateWorkersRejectVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    foreach (MentalStateDef stateDef in DefDatabase<MentalStateDef>.AllDefsListForReading)
    {
      Expect.IsFalse(stateDef.Worker.StateCanOccur(group.vehicle), $"{stateDef} was applied to vehicle.");
    }
    AssertNoMentalState(group.vehicle);
  }

  [Test]
  private void MentalBreakWorkersRejectVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    foreach (MentalBreakDef breakDef in DefDatabase<MentalBreakDef>.AllDefsListForReading)
    {
      Expect.IsFalse(breakDef.Worker.BreakCanOccur(group.vehicle), breakDef.defName);
    }
    AssertNoMentalState(group.vehicle);
  }

  [Test]
  private void MentalWorkersStillAllowOrdinaryPawn()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      drivers = 1
    });
    group.Spawn();
    Pawn pawn = group.DisembarkOne();

    Expect.IsTrue(MentalStateDefOf.Berserk.Worker.StateCanOccur(pawn));
    Expect.IsTrue(MentalBreakDefOf.Berserk.Worker.BreakCanOccur(pawn));
  }

  [TestCase(false, false)]
  [TestCase(false, true)]
  [TestCase(true, false)]
  [TestCase(true, true)]
  private void TryStartMentalState(bool spawned, bool forced)
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    if (spawned)
    {
      group.Spawn();
    }
    MentalStateHandler handler = group.vehicle.mindState.mentalStateHandler;
    Assert.IsNotNull(handler);

    Expect.IsFalse(handler.TryStartMentalState(MentalStateDefOf.Berserk, forced: forced, forceWake: true));
    AssertNoMentalState(group.vehicle);
  }

  [TestCase(false, false, false, false)]
  [TestCase(true, false, false, false)]
  [TestCase(false, true, false, false)]
  [TestCase(false, false, true, false)]
  [TestCase(false, false, false, true)]
  private void ForcedMentalStateCauses(bool causedByMood, bool causedByDamage,
    bool causedByPsycast, bool transitionSilently)
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    Expect.IsFalse(group.vehicle.mindState.mentalStateHandler.TryStartMentalState(
      MentalStateDefOf.Berserk, forced: true, forceWake: true,
      causedByMood: causedByMood, transitionSilently: transitionSilently,
      causedByDamage: causedByDamage, causedByPsycast: causedByPsycast));
    AssertNoMentalState(group.vehicle);
  }

  [Test]
  private void TryStartMentalStateWithOverriddenWorker()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    Expect.IsFalse(group.vehicle.mindState.mentalStateHandler.TryStartMentalState(
      MentalStateDefOf.Wander_OwnRoom, forced: true, forceWake: true));
    AssertNoMentalState(group.vehicle);
  }

  [TestCase(false)]
  [TestCase(true)]
  private void MentalBreakWorkerCannotStartVehicle(bool causedByMood)
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    Expect.IsFalse(MentalBreakDefOf.Berserk.Worker.TryStart(group.vehicle, reason: "testing",
      causedByMood));
    AssertNoMentalState(group.vehicle);
  }

  [Test]
  private void CatatonicBreakWorkerCannotStartVehicle()
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();

    // Catatonic applies a hediff directly instead of calling TryStartMentalState.
    Expect.IsFalse(MentalBreakDefOf.Catatonic.Worker.TryStart(group.vehicle, reason: "testing", true));
    Expect.IsFalse(group.vehicle.health.hediffSet.HasHediff(HediffDefOf.CatatonicBreakdown));
    AssertNoMentalState(group.vehicle);
  }

  [Test]
  private void MentalBreakerCannotStartVehicle([DefsSource] MentalBreakDef mentalBreakDef)
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings());
    group.Spawn();
    MentalBreaker breaker = group.vehicle.mindState.mentalBreaker;
    Assert.IsNotNull(breaker);

    Expect.IsFalse(breaker.TryDoMentalBreak(reason: "testing", mentalBreakDef));
    Expect.IsFalse(breaker.TryDoRandomMoodCausedMentalBreak());
    foreach (MentalBreakIntensity intensity in new[]
             { MentalBreakIntensity.Minor, MentalBreakIntensity.Major, MentalBreakIntensity.Extreme })
    {
      Expect.IsFalse(breaker.TryGetRandomMentalBreak(intensity, out MentalBreakDef breakDef));
      Expect.IsNull(breakDef);
    }
    AssertNoMentalState(group.vehicle);
  }

  private static void AssertNoMentalState(VehiclePawn vehicle)
  {
    Expect.IsFalse(vehicle.InMentalState);
    Expect.IsNull(vehicle.mindState.mentalStateHandler.CurState);
    Expect.IsNull(vehicle.mindState.mentalStateHandler.CurStateDef);
  }
}
