using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CoreLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;

namespace Vehicles.Testing;

internal static class VehicleBlockingMapRemoval
{
  [StartupAction(Category = "World", Name = "Map Removal", GameState = GameState.Playing)]
  private static void SetUpCampRepeated()
  {
    Map map = Find.CurrentMap ?? Find.Maps.FirstOrDefault();
    if (map == null)
    {
      Log.Error("No map to test on.");
      return;
    }
    VehicleDef vehicleDef = DefDatabase<VehicleDef>.AllDefsListForReading.FirstOrDefault();
    if (vehicleDef == null)
    {
      Log.Error("No vehicle to test with.");
      return;
    }
    CoroutineManager.Instance.StartCoroutine(SettleTillFailureRoutine(vehicleDef, map));
  }

  private static IEnumerator SettleTillFailureRoutine(VehicleDef vehicleDef, Map map)
  {
    using VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      vehicleDef = vehicleDef,
      drivers = 1,
      permissions = VehiclePermissions.Mobile
    });
    group.BoardAll();
    VehicleCaravan caravan =
      CaravanHelper.MakeVehicleCaravan([group.vehicle], Faction.OfPlayer, map.Tile, addToWorldPawnsIfNotAlready: true);
    
    HashSet<PlanetTile> processedTiles = [];
    foreach (SurfaceTile surfaceTile in Find.WorldGrid.Tiles)
    {
      PlanetTile tile = surfaceTile.tile;
      if (!tile.Valid || !Find.WorldPathGrid.Passable(tile))
        continue;
      if (!processedTiles.Add(tile) || !SettleInEmptyTileUtility.CanCreateMapAt(tile))
        continue;

      caravan.Tile = surfaceTile.tile;
      CameraJumper.TryJump(caravan, CameraJumper.MovementMode.Cut);
      Command_Action command = (Command_Action)SettleInEmptyTileUtility.SetupCamp(caravan);
      Assert.AreEqual(expected: group.pawns.Count + 1, caravan.PawnsListForReading.Count);
      command.action();
      yield return new WaitForSeconds(1);
      while (Current.ProgramState == ProgramState.MapInitializing || LongEventHandler.AnyEventNowOrWaiting)
      {
        yield return null;
      }
      yield return new WaitForSeconds(1);

      if (group.vehicle.Destroyed)
      {
        Log.Error("Vehicle destroyed!");
        break;
      }
      caravan = CaravanHelper.ExitMapAndCreateVehicleCaravan([group.vehicle], Faction.OfPlayer, tile, tile, tile, sendMessage: false);
    }
  }
}
