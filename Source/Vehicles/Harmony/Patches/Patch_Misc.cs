using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_Misc : IPatchCategory
{
  public void PatchMethods()
  {
    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(MapPawns), "PlayerEjectablePodHolder"),
      prefix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(PlayerEjectableVehicles)));

    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(Selector), "HandleMapClicks"),
      prefix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(MultiSelectFloatMenu)));
    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(MentalState_Manhunter),
        nameof(MentalState_Manhunter.ForceHostileTo), [typeof(Thing)]), prefix: null,
      postfix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(ManhunterDontAttackVehicles)));
    VehicleHarmony.Patch(
      original: AccessTools.PropertyGetter(typeof(TickManager), nameof(TickManager.Paused)),
      postfix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(PausedFromVehicles)));
    VehicleHarmony.Patch(
      original: AccessTools.PropertyGetter(typeof(TickManager), nameof(TickManager.CurTimeSpeed)),
      postfix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(ForcePauseFromVehicles)));

    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(PawnCapacitiesHandler),
        nameof(PawnCapacitiesHandler.Notify_CapacityLevelsDirty)),
      prefix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(RecheckVehicleHandlerCapacities)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill)),
      prefix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(MoveOnDeath)));
    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(PawnUtility),
        nameof(PawnUtility.ShouldSendNotificationAbout)),
      postfix: new HarmonyMethod(typeof(Patch_Misc),
        nameof(SendNotificationsVehicle)));
  }

  private static bool PlayerEjectableVehicles(Thing thing, ref IThingHolder __result)
  {
    if (thing is VehiclePawn vehicle)
    {
      __result = vehicle;
      return false;
    }
    return true;
  }

  private static bool MultiSelectFloatMenu(List<object> ___selected)
  {
    if (Event.current.type == EventType.MouseDown)
    {
      if (Event.current.button == 1 && ___selected.Count > 0)
      {
        if (___selected.Count > 1)
        {
          return !SelectionHelper.MultiSelectClicker(___selected);
        }
      }
    }
    return true;
  }

  private static void ManhunterDontAttackVehicles(Thing t, ref bool __result)
  {
    if (__result && t is VehiclePawn vehicle && !SettingsCache.TryGetValue(
      vehicle.VehicleDef, typeof(VehicleProperties),
      nameof(VehicleProperties.manhunterTargetsVehicle),
      vehicle.VehicleDef.properties.manhunterTargetsVehicle))
    {
      __result = false;
    }
  }

  private static void PausedFromVehicles(ref bool __result)
  {
    if (LandingTargeter.Instance.ForcedTargeting || StrafeTargeter.Instance.ForcedTargeting)
    {
      __result = true;
    }
  }

  private static void ForcePauseFromVehicles(ref TimeSpeed __result)
  {
    if (LandingTargeter.Instance.ForcedTargeting || StrafeTargeter.Instance.ForcedTargeting)
    {
      __result = TimeSpeed.Paused;
    }
  }

  private static void RecheckVehicleHandlerCapacities(Pawn ___pawn)
  {
    if (___pawn.GetVehicle() is { } vehicle)
    {
      //Null check for initial pawn capacities dirty caching when VehiclePawn has not yet called SpawnSetup
      vehicle.EventRegistry?[VehicleEventDefOf.PawnCapacitiesDirty].ExecuteEvents();
    }
  }

  private static void MoveOnDeath(Pawn __instance)
  {
    if (__instance.IsInVehicle())
    {
      VehiclePawn vehicle = __instance.GetVehicle();
      vehicle.AddOrTransfer(__instance);
      if (Find.World.worldPawns.Contains(__instance))
      {
        Find.WorldPawns.RemovePawn(__instance);
      }
      vehicle.EventRegistry[VehicleEventDefOf.PawnKilled].ExecuteEvents();
    }
  }

  private static void SendNotificationsVehicle(Pawn p, ref bool __result)
  {
    if (!__result && p.Faction is { IsPlayer: true } && p is
      { ParentHolder: VehicleRoleHandler or Pawn_InventoryTracker { pawn: VehiclePawn } })
    {
      __result = true;
    }
  }
}