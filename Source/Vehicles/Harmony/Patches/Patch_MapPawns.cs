using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_MapPawns : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(PawnsFinder),
      nameof(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead)),
      postfix: new HarmonyMethod(typeof(Patch_MapPawns),
      nameof(AllAerialVehicles_AliveOrDead)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertyGetter(typeof(PawnsFinder),
      nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)),
      postfix: new HarmonyMethod(typeof(Patch_MapPawns),
      nameof(AllMapsVehiclePassengers_Alive)));

    HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(MapPawns),
      nameof(MapPawns.AllPawnsUnspawned)),
      postfix: new HarmonyMethod(typeof(Patch_MapPawns),
        nameof(AllPawnsInVehiclesUnspawned)));
  }

  private static void AllAerialVehicles_AliveOrDead(ref List<Pawn> __result)
  {
    var worldObjHolder = Find.World?.GetComponent<VehicleWorldObjectsHolder>();

    // NOTE: PawnsFinder checks for null World and being in MainMenu scene, presumably because the music manager
    // reaches into PawnsFinder. This causes issues with mods doing something similar, silently skipping over
    // vanilla and throwing a null reference exception in this patch.
    if (worldObjHolder == null)
      return;

    foreach (AerialVehicleInFlight aerialVehicle in worldObjHolder.AerialVehicles)
    {
      __result.AddRange(aerialVehicle.Vehicle.AllPawnsAboard);
    }
  }

  private static void AllMapsVehiclePassengers_Alive(List<Pawn> __result)
  {
    // NOTE: PawnsFinder checks for null World and being in MainMenu scene, presumably because the music manager
    // reaches into PawnsFinder. This causes issues with mods doing something similar, silently skipping over
    // vanilla and throwing a null reference exception in this patch.
    if (Current.ProgramState == ProgramState.Entry)
      return;

    foreach (Map map in Find.Maps)
    {
      AddAllFromMap(map, __result);
    }
    return;

    static void AddAllFromMap(Map map, List<Pawn> result)
    {
      VehiclePositionManager positionMgr = map.GetDetachedMapComponent<VehiclePositionManager>();
      foreach (VehiclePawn vehicle in positionMgr.AllClaimants)
      {
        if (vehicle.AllPawnsAboard.Count == 0)
          continue;

        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
          result.Add(pawn);
        }
      }
    }
  }

  private static void AllPawnsInVehiclesUnspawned(Map ___map, List<Pawn> ___allPawnsUnspawnedResult)
  {
    VehiclePositionManager positionMgr = ___map.GetDetachedMapComponent<VehiclePositionManager>();
    foreach (VehiclePawn vehicle in positionMgr.AllClaimants)
    {
      if (vehicle.AllPawnsAboard.Count == 0)
        continue;

      foreach (Pawn pawn in vehicle.AllPawnsAboard)
      {
        ___allPawnsUnspawnedResult.Add(pawn);
      }
    }
  }
}