using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using Vehicles.World;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

internal class Patch_CaravanFormation : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanFormingUtility),
        nameof(CaravanFormingUtility.IsFormingCaravan)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(IsFormingCaravanVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(TransferableUtility),
        nameof(TransferableUtility.CanStack)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(CanStackVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GiveToPackAnimalUtility),
        nameof(GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(UsableVehicleWithMostFreeSpace)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanExitMapUtility),
        nameof(CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow)),
      postfix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(CanVehicleExitMapAndJoinOrCreateCaravanNow)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CaravanExitMapUtility),
        nameof(CaravanExitMapUtility.ExitMapAndJoinOrCreateCaravan)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(ExitMapAndJoinOrCreateVehicleCaravan)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertySetter(typeof(Pawn_InventoryTracker),
        nameof(Pawn_InventoryTracker.UnloadEverything)),
      prefix: new HarmonyMethod(typeof(Patch_CaravanFormation),
        nameof(VehiclesShouldntUnloadEverything)));
  }

  /// <summary>
  /// Forming Caravan extension method based on Vehicle LordJob
  /// </summary>
  /// <param name="p"></param>
  /// <param name="__result"></param>
  private static bool IsFormingCaravanVehicle(Pawn p, ref bool __result)
  {
    Lord lord = p.GetLord();
    if (lord is { LordJob: LordJob_FormAndSendVehicles })
    {
      __result = true;
      return false;
    }
    return true;
  }

  /// <summary>
  /// Prevent stacking of Vehicles in the Dialog window of forming VehicleCaravan
  /// </summary>
  private static void CanStackVehicle(Thing thing, ref bool __result)
  {
    if (thing is VehiclePawn)
      __result = false;
  }

  /// <summary>
  /// Find Vehicle (Not pack animal) with usable free space for caravan packing
  /// </summary>
  /// <param name="pawn"></param>
  /// <param name="__result"></param>
  private static bool UsableVehicleWithMostFreeSpace(Pawn pawn, ref Pawn __result)
  {
    if (CaravanHelper.IsFormingCaravanShipHelper(pawn) ||
      pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).HasVehicle())
    {
      __result = CaravanHelper.UsableVehicleWithTheMostFreeSpace(pawn);
      return false;
    }
    return true;
  }

  private static void CanVehicleExitMapAndJoinOrCreateCaravanNow(Pawn pawn, ref bool __result)
  {
    if (pawn is VehiclePawn vehicle)
    {
      __result = vehicle.Spawned && vehicle.Map.exitMapGrid.MapUsesExitGrid &&
        (vehicle.AllPawnsAboard.NotNullAndAny(p => p.IsColonist) ||
          CaravanHelper.FindCaravanToJoinForAllowingVehicles(vehicle) != null);
    }
  }

  private static bool ExitMapAndJoinOrCreateVehicleCaravan(Pawn pawn, Rot4 exitDir)
  {
    VehiclePawn vehicle = pawn as VehiclePawn;
    if (vehicle != null &&
      CaravanHelper.OpportunistcallyCreatedAerialVehicle(vehicle, pawn.Map.Tile))
    {
      return false;
    }
    Caravan caravan = CaravanHelper.FindCaravanToJoinForAllowingVehicles(pawn);
    if (caravan == null &&
      CaravanHelper.FindAerialVehicleToJoinForAllowingVehicles(pawn) is { } aerialVehicle)
    {
      VehicleRoleHandler handler =
        aerialVehicle.vehicle.handlers.FirstOrDefault(handler => handler.AreSlotsAvailable);
      if (handler != null)
      {
        aerialVehicle.vehicle.TryAddPawn(pawn, handler);
        return false;
      }
    }
    if (caravan is VehicleCaravan vehicleCaravan &&
      (vehicle is null || vehicle.IsBoat() == vehicleCaravan.LeadVehicle.IsBoat()))
    {
      CaravanHelper.AddVehicleCaravanExitTaleIfShould(pawn);
      vehicleCaravan.AddPawn(pawn, true);
      pawn.ExitMap(false, exitDir);
      return false;
    }
    else if (vehicle != null)
    {
      Map map = pawn.Map;
      int directionTile =
        CaravanHelper.FindRandomStartingTileBasedOnExitDir(vehicle, map.Tile, exitDir);
      VehicleCaravan newCaravan =
        CaravanHelper.ExitMapAndCreateVehicleCaravan(Gen.YieldSingle(pawn), pawn.Faction,
          map.Tile, directionTile, -1);
      newCaravan.autoJoinable = true;

      if (caravan != null)
      {
        caravan.pawns.TryTransferAllToContainer(newCaravan.pawns);
        caravan.Destroy();
        newCaravan.Notify_Merged([caravan]);
      }
      bool animalWantsToJoin = false;
      foreach (Pawn mapPawn in map.mapPawns.AllPawnsSpawned)
      {
        if (CaravanHelper.FindCaravanToJoinForAllowingVehicles(mapPawn) != null &&
          !mapPawn.Downed && !mapPawn.Drafted)
        {
          if (mapPawn.RaceProps.Animal)
          {
            animalWantsToJoin = true;
          }
          RestUtility.WakeUp(mapPawn);
          mapPawn.jobs.CheckForJobOverride();
        }
      }

      TaggedString taggedString = "MessagePawnLeftMapAndCreatedCaravan"
       .Translate(pawn.LabelShort, pawn).CapitalizeFirst();
      if (animalWantsToJoin)
      {
        taggedString += " " + "MessagePawnLeftMapAndCreatedCaravan_AnimalsWantToJoin".Translate();
      }
      Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion);
      return false;
    }
    return true;
  }

  private static void VehiclesShouldntUnloadEverything(ref bool value, Pawn ___pawn)
  {
    if (___pawn is VehiclePawn)
    {
      value = false;
    }
  }
}