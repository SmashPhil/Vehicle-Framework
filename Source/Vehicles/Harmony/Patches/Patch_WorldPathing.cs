using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Patching;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles;

internal class Patch_WorldPathing : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"),
      prefix: new HarmonyMethod(typeof(Patch_WorldPathing),
        nameof(AutoOrderVehicleCaravanPathing)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Caravan_PathFollower), "StartPath"),
      prefix: new HarmonyMethod(typeof(Patch_WorldPathing),
        nameof(StartVehicleCaravanPath)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WorldRoutePlanner),
        nameof(WorldRoutePlanner.DoRoutePlannerButton)),
      postfix: new HarmonyMethod(typeof(Patch_WorldPathing),
        nameof(VehicleRoutePlannerButton)));
  }

  /// <summary>
  /// Intercept AutoOrderToTileNow method to StartPath on VehicleCaravan_PathFollower
  /// Necessary due to CaravanUtility.BestGotoDestNear returning incorrect positions based on custom tile values for vehicles
  /// </summary>
  /// <param name="c"></param>
  /// <param name="tile"></param>
  private static bool AutoOrderVehicleCaravanPathing(Caravan c, PlanetTile tile)
  {
    if (c is VehicleCaravan vehicleCaravan)
    {
      if (tile < 0 || (tile == vehicleCaravan.Tile && !vehicleCaravan.vehiclePather.Moving))
      {
        return false;
      }
      if (vehicleCaravan.VehiclesListForReading.NullOrEmpty())
      {
        return false;
      }
      foreach (VehiclePawn vehicle in vehicleCaravan.VehiclesListForReading)
      {
        if (!WorldVehiclePathGrid.Instance.Passable(tile, vehicle.VehicleDef) ||
          vehicle.VehicleDef.type == VehicleType.Air)
        {
          return false;
        }
      }
      int bestTile = WorldHelper.BestGotoDestForVehicle(vehicleCaravan, tile);
      if (bestTile >= 0)
      {
        vehicleCaravan.vehiclePather.StartPath(bestTile, null, true);
        vehicleCaravan.gotoMote.OrderedToTile(bestTile);
        SoundDefOf.ColonistOrdered.PlayOneShotOnCamera();
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Catch-All for Caravan_PathFollower.StartPath, redirect to VehicleCaravan_PathFollower
  /// </summary>
  /// <param name="destTile"></param>
  /// <param name="arrivalAction"></param>
  /// <param name="___caravan"></param>
  /// <param name="repathImmediately"></param>
  /// <param name="resetPauseStatus"></param>
  private static bool StartVehicleCaravanPath(PlanetTile destTile,
    CaravanArrivalAction arrivalAction,
    Caravan ___caravan, bool repathImmediately = false, bool resetPauseStatus = true)
  {
    if (___caravan is VehicleCaravan vehicleCaravan)
    {
      vehicleCaravan.vehiclePather.StartPath(destTile, arrivalAction, repathImmediately,
        resetPauseStatus);
      return false;
    }
    return true;
  }

  private static void VehicleRoutePlannerButton(ref float curBaseY)
  {
    Find.World.GetComponent<VehicleRoutePlanner>()?.DoRoutePlannerButton(ref curBaseY);
  }
}