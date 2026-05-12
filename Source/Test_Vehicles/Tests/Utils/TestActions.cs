using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevTools.Testing;
using HarmonyLib;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.Compatibility;
using Vehicles.World;
using Verse;

// NOTE - RimWorld doesn't support parsing of chained namespaces,
// we have to use 1 singular namespace here.
namespace Vehicles;

[PublicAPI]
internal class TestActions
{
  private static readonly FieldInfo VehicleTrackersDict;

  static TestActions()
  {
    if (Ext_Mods.HasActiveMod(ModPackageIds.VanillaVehiclesExpanded))
    {
      Type vveType = GenTypes.GetTypeInAnyAssembly("VanillaVehiclesExpanded.Pawn_ExposeData_Patch");
      Assert.IsNotNull(vveType);
      VehicleTrackersDict = AccessTools.Field(vveType, "pawnVehicleTrackers");
    }
  }

  /// <summary>
  /// VVE is tracking defs liberally, resulting in transient defs writing to the save file. This makes loading
  /// test saves difficult as it may break other things when failing to load the mock defs during loading.
  /// </summary>
  public static void ClearVanillaVehiclesExpandedTrackerCache()
  {
    IDictionary dict = VehicleTrackersDict?.GetValue(null) as IDictionary;
    dict?.Clear();
  }

  /// <summary>
  /// Ensure a player focused map is always focused before beginning any tests to eliminate interruptions
  /// from map parent faction assumptions.
  /// </summary>
  public static void RefocusMap()
  {
    if (Current.ProgramState != ProgramState.Playing || Find.World is null)
      return;
    if (Find.CurrentMap is { IsPlayerHome: true })
      return;

    Map map = Find.AnyPlayerHomeMap;
    Assert.IsNotNull(map);
    Current.Game.CurrentMap = map;
    CameraJumper.TryHideWorld();
  }

  /// <summary>
  /// Ensure no vehicles or vehicle world objects remain after test is conducted, polluting subsequent
  /// tests and resulting in false negatives.
  /// </summary>
  public static void EmptyWorldAndMapOfVehicles()
  {
    if (Current.ProgramState != ProgramState.Playing || Find.World is null)
      return;

    VehicleWorldObjectsHolder worldObjects = Find.World.GetComponent<VehicleWorldObjectsHolder>();
    Assert.IsNotNull(worldObjects);

    Expect.IsTrue(worldObjects.AerialVehicles.Count == 0);
    for (int i = worldObjects.AerialVehicles.Count - 1; i >= 0; i--)
    {
      AerialVehicleInFlight aerialVehicle = worldObjects.AerialVehicles[i];
      aerialVehicle.Destroy();
      DestroyAndRemoveFromWorldPawns(aerialVehicle.Vehicle);
    }
    Expect.IsTrue(worldObjects.VehicleCaravans.Count == 0);
    for (int i = worldObjects.VehicleCaravans.Count - 1; i >= 0; i--)
    {
      VehicleCaravan caravan = worldObjects.VehicleCaravans[i];
      List<VehiclePawn> vehicles = [.. caravan.VehiclesListForReading];
      caravan.Destroy();
      foreach (VehiclePawn vehicle in vehicles)
        DestroyAndRemoveFromWorldPawns(vehicle);
    }
    Expect.IsTrue(worldObjects.StashedVehicles.Count == 0);
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
      if (pawn is VehiclePawn { Destroyed: false } vehicle)
      {
        Test.Fail("WorldPawn vehicle still alive.");
        DestroyAndRemoveFromWorldPawns(vehicle);
      }
    }
    foreach (Map map in Find.Maps)
    {
      foreach (Pawn pawn in map.mapPawns.AllPawns)
      {
        if (pawn is VehiclePawn { Destroyed: false } vehicle)
        {
          Test.Fail("MapPawn vehicle still alive.");
          DestroyAndRemoveFromWorldPawns(vehicle);
        }
      }
    }
    return;

    static void DestroyAndRemoveFromWorldPawns(VehiclePawn vehicle)
    {
      if (!vehicle.Destroyed)
      {
        vehicle.DestroyVehicleAndPawns();
      }
      Assert.IsTrue(vehicle.Destroyed);
      if (Find.WorldPawns.Contains(vehicle))
      {
        Find.WorldPawns.RemoveAndDiscardPawnViaGC(vehicle);
      }
      Assert.IsFalse(Find.WorldPawns.Contains(vehicle));
    }
  }
}