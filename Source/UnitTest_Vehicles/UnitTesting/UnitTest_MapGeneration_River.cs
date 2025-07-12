using System;
using System.Linq;
using System.Reflection;
using DevTools.UnitTesting;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Noise;

namespace Vehicles.UnitTesting;

[UnitTest(TestType.Playing)]
[TestCategory(TestCategoryNames.MapGeneration)]
[TestDescription("Maps generate with the ModSettings river multiplier taken into account.")]
internal sealed class UnitTest_MapGeneration_River
{
  private static readonly IntVec3 DefaultMapSize = new(50, 1, 50);

  private static readonly MethodInfo GetRiverWidthAt =
    AccessTools.Method(typeof(TileMutatorWorker_River), "GetRiverWidthAt");

  // Execute first in case any other mods being tested for compatibility suddenly patch into this
  private static readonly HarmonyMethod GetRiverWidthPostfixMethod =
    new(AccessTools.Method(typeof(UnitTest_MapGeneration_River), nameof(GetRiverWidthPostfix)),
      HarmonyLib.Priority.First);

  private static readonly HarmonyMethod GetRiverWidthFinalizerMethod =
    new(AccessTools.Method(typeof(UnitTest_MapGeneration_River), nameof(GetRiverWidthFinalizer)));

  // Static so patch methods can access for validation
  private static RiverDef riverDef;
  private static float expectedWidth;

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
    expectedWidth = -1;
    riverDef = RiverDefOf.River;
  }

  [TearDown]
  private void ClearFields()
  {
    expectedWidth = -1;
    riverDef = null;
  }

  [Test]
  private void RiverMultiplier()
  {
    TileMutatorDef testDef = TileMutatorDefOf.River;
    Assert.IsNotNull(testDef);
    PlanetTile tile = RandomTile(tile => tile is { Rivers.Count: > 0 }, testDef);
    Assert.IsTrue(tile.Valid);
    RiverMock riverMock = new(tile, testDef);
    using ScopedMethodHook smh = new(GetRiverWidthAt, postfix: GetRiverWidthPostfixMethod,
      finalizer: GetRiverWidthFinalizerMethod);
    using ScopedValueRollback<float> setting = new(ref VehicleMod.settings.main.riverMultiplier);
    using ScopedValueRollback<float> svr = new(ref expectedWidth);

    // 0%
    VehicleMod.settings.main.riverMultiplier = SectionMain.RiverMultMin;
    Assert.AreApproximatelyEqual(ModSettingsHelper.RiverMultiplier, 1 + VehicleMod.settings.main.riverMultiplier);
    expectedWidth = riverDef.widthOnMap;
    Expect.AreApproximatelyEqual(ModSettingsHelper.RiverSizeWithMultiplier(riverDef), expectedWidth);
    GetRiverWidthAt.Invoke(riverMock.Worker, [riverMock.node, Vector2.zero]);

    // 100%
    VehicleMod.settings.main.riverMultiplier = 1;
    Assert.AreApproximatelyEqual(ModSettingsHelper.RiverMultiplier, 1 + VehicleMod.settings.main.riverMultiplier);
    expectedWidth = riverDef.widthOnMap * 2;
    Expect.AreApproximatelyEqual(ModSettingsHelper.RiverSizeWithMultiplier(riverDef), expectedWidth);
    GetRiverWidthAt.Invoke(riverMock.Worker, [riverMock.node, Vector2.zero]);

    // 200%
    VehicleMod.settings.main.riverMultiplier = SectionMain.RiverMultMax;
    Assert.AreApproximatelyEqual(ModSettingsHelper.RiverMultiplier, 1 + VehicleMod.settings.main.riverMultiplier);
    expectedWidth = riverDef.widthOnMap * 3;
    Expect.AreApproximatelyEqual(ModSettingsHelper.RiverSizeWithMultiplier(riverDef), expectedWidth);
    GetRiverWidthAt.Invoke(riverMock.Worker, [riverMock.node, Vector2.zero]);
  }

  private static void GetRiverWidthPostfix(ref readonly float __result)
  {
    expectedWidth = __result * ModSettingsHelper.RiverMultiplier;
  }

  private static void GetRiverWidthFinalizer(ref readonly float __result)
  {
    Expect.AreApproximatelyEqual(__result, expectedWidth);
  }

  [UsedImplicitly(ImplicitUseTargetFlags.Members)]
  private readonly struct RiverMock : IDisposable
  {
    public readonly TileMutatorDef def;
    public readonly RiverNode node;

    private readonly ScopedReferenceRollback<TileMutatorWorker_River, ModuleBase> perlinRollback;

    public RiverMock(PlanetTile tile, TileMutatorDef def)
    {
      const string RiverWidthPerlinNoiseName = "riverWidthNoise";
      const int PerlinSeed = 1234;
      const double PerlinFreq = 1.0;
      const double PerlinLacunarity = 1.0;
      const double PerlinPersistence = 1.0;
      const int PerlinOctaves = 1;

      Tile = tile;
      Worker = def.Worker as TileMutatorWorker_River;
      Perlin perlin = new(PerlinFreq, PerlinLacunarity, PerlinPersistence, PerlinOctaves, PerlinSeed, QualityMode.Low);
      perlinRollback =
        new ScopedReferenceRollback<TileMutatorWorker_River, ModuleBase>(Worker, RiverWidthPerlinNoiseName, perlin);
      this.def = def;
      TileMutatorDef existingDef = tile.Tile.Mutators.FirstOrDefault(mutDef => mutDef == def);
      Assert.IsNotNull(existingDef);
      Assert.IsTrue(ReferenceEquals(existingDef, this.def));

      node = new RiverNode
      {
        width = riverDef.widthOnMap,
        start = Vector3.zero,
        end = new Vector3(DefaultMapSize.x, 0, DefaultMapSize.z)
      };
    }

    public PlanetTile Tile { get; }

    public TileMutatorWorker_River Worker { get; }

    void IDisposable.Dispose()
    {
      perlinRollback.Dispose();
    }
  }
}