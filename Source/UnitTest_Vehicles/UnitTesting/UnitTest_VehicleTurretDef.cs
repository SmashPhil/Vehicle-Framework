using System;
using DevTools.UnitTesting;
using UnityEngine;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.MainMenu)]
internal sealed class UnitTest_VehicleTurretDef : UnitTest_VehicleDefTest
{
  protected override bool ShouldTest(VehicleDef vehicleDef)
  {
    return vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>() is { } comp &&
      !comp.turrets.NullOrEmpty();
  }

  [Test]
  private void HitChanceAccuracy()
  {
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      using Test.Group group = new(vehicleDef.defName);

      CompProperties_VehicleTurrets comp =
        vehicleDef.GetCompProperties<CompProperties_VehicleTurrets>();

      foreach (VehicleTurret turret in comp.turrets)
      {
        using Test.Group turretGroup = new(turret.def.defName);
        if (turret.def.fireModes.NullOrEmpty())
        {
          Test.Skip("No turret fireModes.");
          continue;
        }

        foreach (FireMode fireMode in turret.def.fireModes)
        {
          // Clamped to lower bound
          Expect.Throws<ArgumentOutOfRangeException>(
            delegate { _ = fireMode.GetHitChanceFactor(-1); }, "Distance < 0");
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(0), fireMode.accuracyTouch,
            "Accuracy 0");

          // Touch
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(FireMode.DistanceTouch),
            fireMode.accuracyTouch, "Accuracy Touch");
          ExpectedAccuracy(fireMode, FireMode.DistanceTouch, FireMode.DistanceShort,
            fireMode.accuracyTouch, fireMode.accuracyShort, 0.25f, "Accuracy Touch > Short 25%");
          ExpectedAccuracy(fireMode, FireMode.DistanceTouch, FireMode.DistanceShort,
            fireMode.accuracyTouch, fireMode.accuracyShort, 0.5f, "Accuracy Touch > Short 50%");
          ExpectedAccuracy(fireMode, FireMode.DistanceTouch, FireMode.DistanceShort,
            fireMode.accuracyTouch, fireMode.accuracyShort, 0.75f, "Accuracy Touch > Short 75%");
          ExpectedAccuracy(fireMode, FireMode.DistanceTouch, FireMode.DistanceShort,
            fireMode.accuracyTouch, fireMode.accuracyShort, 1, "Accuracy Touch > Short 100%");

          // Short
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(FireMode.DistanceShort),
            fireMode.accuracyShort, "Accuracy Short");
          ExpectedAccuracy(fireMode, FireMode.DistanceShort, FireMode.DistanceMedium,
            fireMode.accuracyShort, fireMode.accuracyMedium, 0.25f, "Accuracy Short > Medium 25%");
          ExpectedAccuracy(fireMode, FireMode.DistanceShort, FireMode.DistanceMedium,
            fireMode.accuracyShort, fireMode.accuracyMedium, 0.5f, "Accuracy Short > Medium 50%");
          ExpectedAccuracy(fireMode, FireMode.DistanceShort, FireMode.DistanceMedium,
            fireMode.accuracyShort, fireMode.accuracyMedium, 0.75f, "Accuracy Short > Medium 75%");
          ExpectedAccuracy(fireMode, FireMode.DistanceShort, FireMode.DistanceMedium,
            fireMode.accuracyShort, fireMode.accuracyMedium, 1, "Accuracy Short > Medium 100%");

          // Medium
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(FireMode.DistanceMedium),
            fireMode.accuracyMedium, "Accuracy Medium");
          ExpectedAccuracy(fireMode, FireMode.DistanceMedium, FireMode.DistanceLong,
            fireMode.accuracyMedium, fireMode.accuracyLong, 0.25f, "Accuracy Medium > Long 25%");
          ExpectedAccuracy(fireMode, FireMode.DistanceMedium, FireMode.DistanceLong,
            fireMode.accuracyMedium, fireMode.accuracyLong, 0.5f, "Accuracy Medium > Long 50%");
          ExpectedAccuracy(fireMode, FireMode.DistanceMedium, FireMode.DistanceLong,
            fireMode.accuracyMedium, fireMode.accuracyLong, 0.75f, "Accuracy Medium > Long 75%");
          ExpectedAccuracy(fireMode, FireMode.DistanceMedium, FireMode.DistanceLong,
            fireMode.accuracyMedium, fireMode.accuracyLong, 1, "Accuracy Medium > Long 100%");

          // Long
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(FireMode.DistanceLong),
            fireMode.accuracyLong, "Accuracy Long");
          // Clamped to upper bound
          Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(FireMode.DistanceLong + 1),
            fireMode.accuracyLong, "Accuracy Long +1");
        }
      }
    }
    return;

    static void ExpectedAccuracy(FireMode fireMode, float min, float max, float minAccuracy,
      float maxAccuracy, float t, string message = null)
    {
      float distance = Mathf.Lerp(min, max, t);
      float expected = Mathf.Lerp(minAccuracy, maxAccuracy, t);
      Expect.AreApproximatelyEqual(fireMode.GetHitChanceFactor(distance), expected,
        message: message);
    }
  }
}