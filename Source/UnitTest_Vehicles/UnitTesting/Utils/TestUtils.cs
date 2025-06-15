using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.UnitTesting;

[PublicAPI]
public static class TestUtils
{
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

  /// <summary>
  /// Ensure no vehicles or vehicle world objects remain after test is conducted, polluting subsequent
  /// tests and resulting in false negatives.
  /// </summary>
  public static void EmptyWorldAndMapOfVehicles()
  {
    VehicleWorldObjectsHolder worldObjects = Find.World.GetComponent<VehicleWorldObjectsHolder>();
    Assert.IsNotNull(worldObjects);
    for (int i = worldObjects.AerialVehicles.Count - 1; i >= 0; i--)
    {
      AerialVehicleInFlight aerialVehicle = worldObjects.AerialVehicles[i];
      aerialVehicle.Destroy();
      DestroyAndRemoveFromWorldPawns(aerialVehicle.vehicle);
    }
    for (int i = worldObjects.VehicleCaravans.Count - 1; i >= 0; i--)
    {
      VehicleCaravan caravan = worldObjects.VehicleCaravans[i];
      List<VehiclePawn> vehicles = [.. caravan.VehiclesListForReading];
      caravan.Destroy();
      foreach (VehiclePawn vehicle in vehicles)
        DestroyAndRemoveFromWorldPawns(vehicle);
    }
    for (int i = worldObjects.StashedVehicles.Count - 1; i >= 0; i--)
    {
      StashedVehicle stashedVehicle = worldObjects.StashedVehicles[i];
      List<VehiclePawn> vehicles = stashedVehicle.Vehicles.ToList();
      stashedVehicle.Destroy();
      foreach (VehiclePawn vehicle in vehicles)
        DestroyAndRemoveFromWorldPawns(vehicle);
    }
    foreach (Pawn pawn in Find.World.worldPawns.AllPawnsAliveOrDead)
    {
      if (pawn is VehiclePawn vehicle)
        DestroyAndRemoveFromWorldPawns(vehicle);
    }
    foreach (Map map in Find.Maps)
    {
      foreach (Pawn pawn in map.mapPawns.AllPawns)
      {
        if (pawn is VehiclePawn { Destroyed: false } vehicle)
          DestroyAndRemoveFromWorldPawns(vehicle);
      }
    }
    return;

    static void DestroyAndRemoveFromWorldPawns(VehiclePawn vehicle)
    {
      if (!vehicle.Destroyed)
        vehicle.DestroyVehicleAndPawns();
      Assert.IsTrue(vehicle.Destroyed);
      if (Find.WorldPawns.Contains(vehicle))
        Find.WorldPawns.RemoveAndDiscardPawnViaGC(vehicle);
      Assert.IsFalse(Find.WorldPawns.Contains(vehicle));
    }
  }
}