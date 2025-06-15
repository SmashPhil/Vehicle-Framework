using System;
using DevTools.UnitTesting;
using RimWorld;
using RimWorld.Planet;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.Playing), ExecutionPriority(Priority.First)]
[TestCategory(TestCategoryNames.ComponentCache)]
[TestDescription(
  "Map components are initialized during map generation and cleared when map is unloaded.")]
internal class UnitTest_ComponentCache_Init
{
  [Test]
  private void DetachedComponentsAdded()
  {
    // All DetachedMapComponents must be pre-cached
    Expect.IsTrue(ComponentCache.DetachedComponentCount() ==
      ComponentCache.DetachedComponentTypeCount);
  }

  [Test]
  private void ClearFromDeinit()
  {
    const int MapGensForCaching = 3;

    using GenStepWarningDisabler warningDisabler = new();

    foreach (Map existingMap in Find.Maps)
      existingMap.GetCachedMapComponent<BreakdownManager>();
    Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), Find.Maps.Count);

    for (int i = 0; i < MapGensForCaching; i++)
    {
      PlanetTile tile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface);
      Assert.IsTrue(tile.Valid);

      Map map = null;
      try
      {
        Settlement settlement =
          (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        settlement.Tile = tile;
        settlement.SetFaction(Faction.OfPlayer);
        Find.WorldObjects.Add(settlement);
        map = MapGenerator.GenerateMap(new IntVec3(50, 1, 50), settlement,
          settlement.MapGeneratorDef);
        CameraJumper.TryJump(map.Center, map);

        int index = map.Index;
        Expect.IsNotNull(map.GetCachedMapComponent<BreakdownManager>());
        Expect.IsNotNull(MapComponentCache<BreakdownManager>.GetComponent(index));
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
        Expect.Throws<IndexOutOfRangeException>(() =>
          map.GetCachedMapComponent<BreakdownManager>());
        Expect.IsNull(MapComponentCache<BreakdownManager>.GetComponent(index));
      }
      finally
      {
        if (map is { Disposed: false })
        {
          Current.Game.DeinitAndRemoveMap(map, false);
          map.Parent.Destroy();
        }
        Assert.IsFalse(map is { Disposed: false });
        Assert.IsFalse(map?.Parent is { Destroyed: false });
        Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));
      }
    }
    Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), Find.Maps.Count);
  }
}