using System;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.MapGeneration)]
[TestDescription("Maps generate with the ModSettings beach multiplier taken into account.")]
internal sealed class UnitTest_MapGeneration_Coast
{
  private static readonly IntVec3 DefaultMapSize = new(50, 1, 50);

  private static readonly MethodInfo CoastOffset =
    AccessTools.PropertyGetter(typeof(TileMutatorWorker_Coast), "CoastOffset");

  // Execute first in case any other mods being tested for compatibility suddenly patch into this
  private static readonly HarmonyMethod CoastOffsetPostfixMethod =
    new(AccessTools.Method(typeof(UnitTest_MapGeneration_Coast), nameof(CoastOffsetAtPostfix)),
      HarmonyLib.Priority.First);

  private static readonly HarmonyMethod CoastOffsetFinalizerMethod =
    new(AccessTools.Method(typeof(UnitTest_MapGeneration_Coast), nameof(CoastOffsetAtFinalizer)));

  // Static so patch methods can access for validation
  private static float expectedOffsetMin;
  private static float expectedOffsetMax;
  private static Map map;

  private static PlanetTile RandomTile(Func<SurfaceTile, bool> validator, TileMutatorDef fallbackMutator)
  {
    foreach (SurfaceTile tile in Find.WorldGrid.Tiles)
    {
      if (validator(tile))
        return tile.tile;
    }
    foreach (SurfaceTile tile in Find.WorldGrid.Tiles)
    {
      if (!Find.WorldObjects.AnyWorldObjectAt(tile.tile))
      {
        tile.AddMutator(fallbackMutator);
        return tile.tile;
      }
    }
    return PlanetTile.Invalid;
  }

  [SetUp]
  private void ResetExpectedWidth()
  {
    expectedOffsetMin = -1;
    expectedOffsetMax = -1;
    map = Find.CurrentMap;
    Assert.IsNotNull(map);
  }

  private void UnsetMap()
  {
    expectedOffsetMin = -1;
    expectedOffsetMax = -1;
    map = null;
  }

  [Test]
  private void CoastMultiplier()
  {
    TileMutatorDef testDef = TileMutatorDefOf.Coast;
    Assert.IsNotNull(testDef);
    PlanetTile tile = RandomTile(tile => tile is { IsCoastal: true }, testDef);
    Assert.IsTrue(tile.Valid);
    using ScopedMethodHook smh = new(CoastOffset, postfix: CoastOffsetPostfixMethod,
      finalizer: CoastOffsetFinalizerMethod);
    using ScopedValueRollback<float> setting = new(ref VehicleMod.settings.main.beachMultiplier);
    using ScopedValueRollback<float> svrMin = new(ref expectedOffsetMin);
    using ScopedValueRollback<float> svrMax = new(ref expectedOffsetMax);

    // TileMutatorWorker_Coast::CoastOffset
    const float CoastOffsetMin = 0.1f;
    const float CoastOffsetMax = 0.2f;

    // 0%
    VehicleMod.settings.main.beachMultiplier = SectionMain.BeachMultMin;
    expectedOffsetMin = CoastOffsetMin;
    expectedOffsetMax = CoastOffsetMax;
    // Verify ModSettingsHelper applies multiplier correctly
    FloatRange offsetRange = new(expectedOffsetMin, expectedOffsetMax);
    FloatRange settingsRange = ModSettingsHelper.BeachMultiplier(new FloatRange(CoastOffsetMin, CoastOffsetMax));
    Expect.AreApproximatelyEqual(settingsRange.min, offsetRange.min);
    Expect.AreApproximatelyEqual(settingsRange.max, offsetRange.max);
    // Verify patch applies multiplier correctly
    CoastOffset.Invoke(testDef.Worker, []);

    // 100%
    VehicleMod.settings.main.beachMultiplier = 1;
    expectedOffsetMin = CoastOffsetMin * 2;
    expectedOffsetMax = CoastOffsetMax * 2;
    // Verify ModSettingsHelper applies multiplier correctly
    offsetRange = new FloatRange(expectedOffsetMin, expectedOffsetMax);
    settingsRange = ModSettingsHelper.BeachMultiplier(new FloatRange(CoastOffsetMin, CoastOffsetMax));
    Expect.AreApproximatelyEqual(settingsRange.min, offsetRange.min);
    Expect.AreApproximatelyEqual(settingsRange.max, offsetRange.max);
    // Verify patch applies multiplier correctly
    CoastOffset.Invoke(testDef.Worker, []);

    // 200%
    VehicleMod.settings.main.beachMultiplier = SectionMain.BeachMultMax;
    expectedOffsetMin = CoastOffsetMin * 3;
    // Verify ModSettingsHelper applies multiplier correctly
    expectedOffsetMax = CoastOffsetMax * 3;
    offsetRange = new FloatRange(expectedOffsetMin, expectedOffsetMax);
    settingsRange = ModSettingsHelper.BeachMultiplier(new FloatRange(CoastOffsetMin, CoastOffsetMax));
    Expect.AreApproximatelyEqual(settingsRange.min, offsetRange.min);
    Expect.AreApproximatelyEqual(settingsRange.max, offsetRange.max);
    // Verify patch applies multiplier correctly
    CoastOffset.Invoke(testDef.Worker, []);
  }

  private static void CoastOffsetAtPostfix(ref readonly FloatRange __result)
  {
    expectedOffsetMin = __result.min * (1 + VehicleMod.settings.main.beachMultiplier);
    expectedOffsetMax = __result.max * (1 + VehicleMod.settings.main.beachMultiplier);
  }

  private static void CoastOffsetAtFinalizer(ref readonly FloatRange __result)
  {
    Expect.AreApproximatelyEqual(__result.min, expectedOffsetMin);
    Expect.AreApproximatelyEqual(__result.max, expectedOffsetMax);
  }
}