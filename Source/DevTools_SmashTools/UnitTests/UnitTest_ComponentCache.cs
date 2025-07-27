using System.Collections.Generic;
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
internal class UnitTest_ComponentCache
{
  private static readonly IntVec3 DefaultMapSize = new(50, 1, 50);

  private static PlanetTile FindValidTile(PlanetLayerDef layerDef)
  {
    PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
    return TileFinder.RandomSettlementTileFor(layer, Faction.OfPirates,
      extraValidator: ValidObjectTile);

    bool ValidObjectTile(PlanetTile tile)
    {
      return !Find.WorldObjects.AnyWorldObjectAt(tile);
    }
  }

  [Test]
  private void DetachedComponentsAdded()
  {
    // All DetachedMapComponents must be pre-cached
    Expect.IsTrue(ComponentCache.DetachedComponentCount() ==
      ComponentCache.DetachedComponentTypeCount);
  }

  [Test]
  private void Retrieval()
  {
    using GenStepWarningDisabler gswd = new();
    PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
    Assert.IsTrue(tile.Valid);

    Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
      WorldObjectDefOf.Settlement);
    Assert.IsNotNull(map);
    using ScopeWorldObject swo = new(map.Parent);
    Settlement settlement = map.Parent as Settlement;
    Assert.IsNotNull(settlement);
    Assert.IsFalse(map.Disposed);
    BreakdownManager component = map.GetComponent<BreakdownManager>();
    Assert.IsNotNull(component);
    BreakdownManager cacheComponent = map.GetCachedMapComponent<BreakdownManager>();
    Assert.IsNotNull(cacheComponent);
    Expect.ReferencesAreEqual(component, cacheComponent);
    Expect.ReferencesAreEqual(component.map, cacheComponent.map);
    Expect.AreEqual(component.map.uniqueID, cacheComponent.map.uniqueID);
  }

  [Test]
  private void MultipleMapInit()
  {
    const int MapCount = 3;
    using GenStepWarningDisabler gswd = new();

    List<Map> maps = [];
    try
    {
      for (int i = 0; i < MapCount; i++)
      {
        PlanetTile tile = FindValidTile(PlanetLayerDefOf.Surface);
        Assert.IsTrue(tile.Valid);
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, DefaultMapSize,
          WorldObjectDefOf.Settlement);
        Assert.IsNotNull(map);
        maps.Add(map);
        Settlement settlement = map.Parent as Settlement;
        Assert.IsNotNull(settlement);
        Assert.IsFalse(map.Disposed);
        BreakdownManager component = map.GetComponent<BreakdownManager>();
        Assert.IsNotNull(component);
        BreakdownManager cacheComponent = map.GetCachedMapComponent<BreakdownManager>();
        Assert.IsNotNull(cacheComponent);
        Expect.ReferencesAreEqual(component, cacheComponent);
        Expect.ReferencesAreEqual(component.map, cacheComponent.map);
        Expect.AreEqual(component.map.uniqueID, cacheComponent.map.uniqueID);
      }
      Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), 3);

      const int RemoveIndex = 1;
      Map mapToRemove = maps[RemoveIndex];
      mapToRemove.Parent.Destroy();
      Expect.IsNull(mapToRemove.GetCachedMapComponent<BreakdownManager>());
      Expect.AreEqual(MapComponentCache<BreakdownManager>.ClearAllDisposed(), 0);
      Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), 2);

      for (int i = 0; i < MapCount; i++)
      {
        BreakdownManager component = maps[i].GetComponent<BreakdownManager>();
        BreakdownManager cacheComponent = maps[i].GetCachedMapComponent<BreakdownManager>();

        // NOTE - Ludeon does not clear map component list, map will be cleaned by GC anyways, just checking here in
        // case this behavior ever changes which will affect the ComponentCache since we can then properly clean up
        // old cached references without blocking subsequent fetches from disposed maps.
        if (i == RemoveIndex)
        {
          Expect.IsNotNull(component);
          Expect.IsNull(cacheComponent);
          continue;
        }
        Assert.IsNotNull(component);
        Assert.IsNotNull(cacheComponent);
        Expect.ReferencesAreEqual(component, cacheComponent);
        Expect.ReferencesAreEqual(component.map, cacheComponent.map);
        Expect.AreEqual(component.map.uniqueID, cacheComponent.map.uniqueID);
      }
    }
    finally
    {
      foreach (Map map in maps)
      {
        if (!map.Parent.Destroyed)
          map.Parent.Destroy();
      }
    }
  }

  [Test]
  private void ClearFromDeinit()
  {
    const int MapGensForCaching = 3;

    using GenStepWarningDisabler warningDisabler = new();

    foreach (Map existingMap in Find.Maps)
      existingMap.GetCachedMapComponent<BreakdownManager>();
    Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), Find.Maps.Count);

    int countBefore = MapComponentCache<BreakdownManager>.Count();
    for (int i = 0; i < MapGensForCaching; i++)
    {
      PlanetTile tile = TestUtils.FindValidTile(PlanetLayerDefOf.Surface);
      Assert.IsTrue(tile.Valid);
      Assert.IsFalse(Find.WorldObjects.AnyWorldObjectAt(tile));

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

        int mapId = map.uniqueID;
        Expect.IsNotNull(map.GetCachedMapComponent<BreakdownManager>());
        Expect.IsNotNull(MapComponentCache<BreakdownManager>.GetComponent(mapId));
        Current.Game.DeinitAndRemoveMap(map, false);
        map.Parent.Destroy();
        Expect.IsNull(MapComponentCache<BreakdownManager>.GetComponent(mapId));
      }
      finally
      {
        if (map?.Parent is { Destroyed: false })
          map.Parent.Destroy();
      }
    }
    Expect.AreEqual(MapComponentCache<BreakdownManager>.Count(), countBefore);
  }
}