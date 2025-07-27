using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[PublicAPI]
public static class TestUtils
{
  public static PlanetTile FindValidTile(PlanetLayerDef layerDef, Faction faction)
  {
    PlanetLayer layer = Find.WorldGrid.FirstLayerOfDef(layerDef);
    return TileFinder.RandomSettlementTileFor(layer, faction,
      extraValidator: ValidObjectTile);

    bool ValidObjectTile(PlanetTile tile)
    {
      return !Find.WorldObjects.AnyWorldObjectAt(tile);
    }
  }

  public static void PrepareArea(Map map, IntVec3 center, VehicleDef vehicleDef)
  {
    int maxSize = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z);
    CellRect testArea = CellRect.CenteredOn(center, maxSize).ExpandedBy(5);
    PrepareArea(map, testArea, vehicleDef);
  }

  public static void PrepareArea(Map map, CellRect areaRect, VehicleDef vehicleDef)
  {
    TerrainDef terrainDef = DefDatabase<TerrainDef>.AllDefsListForReading
     .FirstOrDefault(def => VehiclePathGrid.PassableTerrainCost(vehicleDef, def, out _) &&
        def.affordances.Contains(vehicleDef.buildDef.terrainAffordanceNeeded));
    DebugHelper.DestroyArea(areaRect, map, terrainDef);
  }

  public static void ForceSpawn(VehiclePawn vehicle)
  {
    Assert.IsFalse(vehicle.Spawned);
    VehicleDef vehicleDef = vehicle.VehicleDef;
    Map map = Find.CurrentMap;
    IntVec3 spawnCell = map.Center;
    PrepareArea(map, spawnCell, vehicleDef);
    GenSpawn.Spawn(vehicle, spawnCell, map, vehicleDef.defaultPlacingRot);
  }
}