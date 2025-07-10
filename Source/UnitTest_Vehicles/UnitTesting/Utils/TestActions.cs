using System.Collections.Generic;
using System.Linq;
using DevTools.UnitTesting;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

// NOTE - RimWorld doesn't support parsing of chained namespaces,
// we have to use 1 singular namespace here.
namespace Vehicles;

internal class TestActions
{
  /// <summary>
  /// Ensure no vehicles or vehicle world objects remain after test is conducted, polluting subsequent
  /// tests and resulting in false negatives.
  /// </summary>
  [UsedImplicitly] // Post-test action
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
        Expect.IsTrue(false, "WorldPawn vehicle still alive.");
        DestroyAndRemoveFromWorldPawns(vehicle);
      }
    }
    foreach (Map map in Find.Maps)
    {
      foreach (Pawn pawn in map.mapPawns.AllPawns)
      {
        if (pawn is VehiclePawn { Destroyed: false } vehicle)
        {
          Expect.IsTrue(false, "MapPawn vehicle still alive.");
          DestroyAndRemoveFromWorldPawns(vehicle);
        }
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