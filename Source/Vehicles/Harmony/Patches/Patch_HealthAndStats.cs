using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_HealthAndStats : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehicleMoveSpeed)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(HealthUtility),
        nameof(HealthUtility.GetGeneralConditionLabel)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(ReplaceConditionLabel)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehicleShouldBeDowned)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_HealthTracker),
        nameof(Pawn_HealthTracker.AddHediff),
        parameters:
        [
          typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?),
          typeof(DamageWorker.DamageResult)
        ]),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesDontAddHediffs)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Pawn_HealthTracker), "MakeDowned"),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesCantBeDowned)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MentalStateWorker),
        nameof(MentalStateWorker.StateCanOccur)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesCantEnterMentalState)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MentalBreakWorker),
        nameof(MentalBreakWorker.BreakCanOccur)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesCantEnterMentalBreak)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealNaturally)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesDontHeal)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(HediffUtility),
        nameof(HediffUtility.CanHealFromTending)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesDontHealTended)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Verb_CastAbility),
        nameof(Verb_CastAbility.CanHitTarget)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(VehiclesImmuneToPsycast)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(StatWorker), nameof(StatWorker.IsDisabledFor)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(StatDisabledForVehicle)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(SchoolUtility), nameof(SchoolUtility.CanTeachNow)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(CantTeachVehicles)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(StunHandler), nameof(StunHandler.StunFor)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(StunVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(StaggerHandler), nameof(StaggerHandler.StaggerFor)),
      prefix: new HarmonyMethod(typeof(Patch_HealthAndStats),
        nameof(StaggerVehicle)));
  }

  /// <summary>
  /// Apply MoveSpeed upgrade stat to vehicles
  /// </summary>
  /// <param name="diagonal"></param>
  /// <param name="__instance"></param>
  /// <param name="__result"></param>
  public static bool VehicleMoveSpeed(bool diagonal, Pawn __instance, ref float __result)
  {
    if (__instance is VehiclePawn vehicle)
    {
      float speed = 1 / (vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) / 60);
      if (vehicle.Spawned && !vehicle.Map.roofGrid.Roofed(vehicle.Position))
      {
        speed /= vehicle.Map.weatherManager.CurMoveSpeedMultiplier;
      }
      if (diagonal)
      {
        speed *= Ext_Math.Sqrt2;
      }
      __result = speed.Clamp(1f, 450f);
      return false;
    }
    return true;
  }

  /// <summary>
  /// Replace vanilla labels on Boats to instead show custom ones which are modifiable in the XML defs
  /// </summary>
  /// <param name="__result"></param>
  /// <param name="pawn"></param>
  /// <param name="shortVersion"></param>
  public static bool ReplaceConditionLabel(ref string __result, Pawn pawn,
    bool shortVersion = false)
  {
    if (pawn != null)
    {
      if (pawn is VehiclePawn vehicle)
      {
        if (vehicle.movementStatus == VehicleMovementStatus.Offline && !pawn.Dead)
        {
          if (pawn.IsBoat() && vehicle.beached)
          {
            __result = "VF_healthLabel_Beached".Translate();
          }
          else
          {
            __result = "VF_healthLabel_Immobile".Translate();
          }

          return false;
        }
        if (pawn.Dead)
        {
          __result = "VF_healthLabel_Dead".Translate();
          return false;
        }
        if (vehicle.statHandler.HealthPercent < 0.95f)
        {
          __result = "VF_healthLabel_Injured".Translate();
          return false;
        }
        __result = "VF_healthLabel_Healthy".Translate();
        return false;
      }
    }
    return true;
  }

  public static bool VehiclesDontAddHediffs(Pawn ___pawn)
  {
    if (___pawn is VehiclePawn)
    {
      return false;
    }
    return true;
  }

  public static bool VehiclesCantBeDowned(Pawn ___pawn)
  {
    if (___pawn is VehiclePawn)
    {
      return false;
    }
    return true;
  }

  public static bool VehiclesCantEnterMentalState(Pawn pawn, ref bool __result)
  {
    if (pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  public static bool VehiclesCantEnterMentalBreak(Pawn pawn, ref bool __result)
  {
    if (pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  /// <summary>
  /// Only allow the Boat to be downed if specified within XML def
  /// </summary>
  /// <param name="__result"></param>
  /// <param name="___pawn"></param>
  /// <returns></returns>
  public static bool VehicleShouldBeDowned(ref bool __result, ref Pawn ___pawn)
  {
    if (___pawn != null && ___pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  /// <summary>
  /// Vehicles do not heal over time, and must be repaired instead
  /// </summary>
  /// <param name="hd"></param>
  /// <param name="__result"></param>
  /// <returns></returns>
  public static bool VehiclesDontHeal(Hediff_Injury hd, ref bool __result)
  {
    if (hd.pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  /// <summary>
  /// Boats can not be tended, and thus don't heal. They must be repaired instead
  /// </summary>
  /// <param name="hd"></param>
  /// <param name="__result"></param>
  /// <returns></returns>
  public static bool VehiclesDontHealTended(Hediff_Injury hd, ref bool __result)
  {
    if (hd.pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  /// <summary>
  /// Block vehicles from receiving psycast effects
  /// </summary>
  /// <param name="targ"></param>
  public static bool VehiclesImmuneToPsycast(LocalTargetInfo targ)
  {
    if (targ.Pawn is VehiclePawn vehicle)
    {
      Debug.Message($"Psycast blocked for {vehicle}");
      return false;
    }
    return true;
  }

  public static bool StatDisabledForVehicle(Thing thing, ref bool __result)
  {
    if (thing is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  public static bool CantTeachVehicles(Pawn teacher, ref bool __result)
  {
    if (teacher is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  public static bool StunVehicle(int ticks, Thing instigator, Thing ___parent)
  {
    if (___parent is VehiclePawn vehicle)
    {
      return vehicle.statHandler.OverrideStunPatch;
    }
    return true;
  }

  public static bool StaggerVehicle(int ticks, Thing ___parent, ref bool __result)
  {
    if (___parent is VehiclePawn vehicle)
    {
      __result = false;
      return false;
    }
    return true;
  }
}