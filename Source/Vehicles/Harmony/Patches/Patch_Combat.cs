using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

internal class Patch_Combat : IPatchCategory
{
  public void PatchMethods()
  {
    VehicleHarmony.Patch(
      original: AccessTools.PropertyGetter(typeof(Projectile), "StartingTicksToImpact"),
      postfix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(StartingTicksFromTurret)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "CanHit"),
      prefix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(TurretHitFlags)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile_Explosive), "Impact"),
      prefix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(ImpactExplosiveProjectiles)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "ImpactSomething"),
      transpiler: new HarmonyMethod(typeof(Patch_Combat),
        nameof(VehicleProjectileChanceToHit)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.Destroy)),
      prefix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(ProjectileMapToWorld)));
    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(Projectile), "CheckForFreeIntercept"),
      transpiler: new HarmonyMethod(typeof(Patch_Combat),
        nameof(VehicleProjectileInterceptor)));
    VehicleHarmony.Patch(original: AccessTools.Method(typeof(Explosion), "AffectCell"),
      prefix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(AffectVehicleInCell)));
    VehicleHarmony.Patch(
      original: AccessTools.Method(typeof(DamageWorker), "ExplosionDamageThing"),
      postfix: new HarmonyMethod(typeof(Patch_Combat),
        nameof(VehicleMultipleExplosionInstances)),
      transpiler: new HarmonyMethod(typeof(Patch_Combat),
        nameof(VehicleExplosionDamageTranspiler)));
  }

  /// <summary>
  /// If projectile has <see cref="CompTurretProjectileProperties"/> override total ticks to
  /// impact for speed readjustment
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="__result"></param>
  /// <param name="___origin"></param>
  /// <param name="___destination"></param>
  private static void StartingTicksFromTurret(Projectile __instance, ref float __result,
    Vector3 ___origin, Vector3 ___destination)
  {
    if (__instance.TryGetComp<CompTurretProjectileProperties>() is { } comp)
    {
      float num = (___origin - ___destination).magnitude / (comp.speed / 100);
      if (num <= 0f)
      {
        num = 0.001f;
      }
      __result = num;
    }
  }

  /// <summary>
  /// Enforces behavior from <see cref="CompTurretProjectileProperties"/> where overridden hit
  /// flags should determine valid Things for interception
  /// </summary>
  /// <param name="thing"></param>
  /// <param name="__instance"></param>
  /// <param name="___launcher"></param>
  /// <param name="__result"></param>
  private static bool TurretHitFlags(Thing thing, Projectile __instance, Thing ___launcher,
    ref bool __result)
  {
    if (__instance.TryGetComp<CompTurretProjectileProperties>() is { } comp)
    {
      if (!thing.Spawned)
      {
        __result = false;
        return false;
      }
      if (thing == ___launcher)
      {
        __result = false;
        return false;
      }

      bool flag = false;
      foreach (IntVec3 c in thing.OccupiedRect())
      {
        bool flag2 = false;
        foreach (Thing thingFromList in c.GetThingList(__instance.Map))
        {
          if (thingFromList != thing && ((comp.hitflags != null &&
                thingFromList.def.fillPercent >= comp.hitflags.minFillPercent) ||
              (comp.hitflags is null && thingFromList.def.Fillage == FillCategory.Full)) &&
            thingFromList.def.Altitude >= thing.def.Altitude)
          {
            flag2 = true;
            break;
          }
        }
        if (!flag2)
        {
          flag = true;
          break;
        }
      }
      if (!flag)
      {
        __result = false;
        return false;
      }

      ProjectileHitFlags hitFlags = __instance.HitFlags;
      if (thing == __instance.intendedTarget && hitFlags.HasFlag(ProjectileHitFlags.IntendedTarget))
      {
        __result = true;
        return false;
      }
      if (thing != __instance.intendedTarget)
      {
        if (thing is Pawn pawn)
        {
          if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
          {
            __result = true;
            return false;
          }
          if (comp.hitflags is { hitThroughPawns: true } && !pawn.Dead && !pawn.Downed)
          {
            thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, comp.speed * 2, 0, -1, __instance));
          }
        }
        else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
        {
          __result = true;
          return false;
        }
      }
      if (comp.hitflags is { minFillPercent: > 0 })
      {
        __result = thing.def.fillPercent >= comp.hitflags?.minFillPercent;
        return false;
      }
      __result = thing == __instance.intendedTarget &&
        thing.def.fillPercent >= comp.hitflags?.minFillPercent;
      return false;
    }
    return true;
  }

  private static bool ImpactExplosiveProjectiles(Thing hitThing, Projectile __instance,
    Thing ___launcher)
  {
    if (hitThing is VehiclePawn vehicle)
    {
      /*IntVec3 cell = */
      vehicle.statHandler.RegisterImpacter(___launcher, __instance.Position);
      //ProjectileHelper.DeflectProjectile(__instance, vehicle);
      //return false;
    }
    if (VehicleMod.settings.main.reduceExplosionsOnWater &&
      __instance.def.GetModExtension<ReduceExplosionOnWater>() != null)
    {
      Map map = __instance.Map;
      TerrainDef terrainImpact = map.terrainGrid.TerrainAt(__instance.Position);
      if (__instance.def.projectile.explosionDelay == 0 && terrainImpact.IsWater && !__instance
       .Position.GetThingList(__instance.Map).NotNullAndAny(x => x is VehiclePawn))
      {
        DamageHelper.Explode(__instance);
        return false;
      }
    }
    return true;
  }

  private static IEnumerable<CodeInstruction> VehicleProjectileChanceToHit(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.opcode == OpCodes.Stloc_S &&
        instruction.operand is LocalBuilder { LocalIndex: 8 })
      {
        yield return instruction; //Stloc_S : 8
        instruction = instructionList[++i];
        yield return instruction; //Ldloc_S : 8
        instruction = instructionList[++i];
        yield return new CodeInstruction(opcode: OpCodes.Call,
          AccessTools.Method(typeof(Patch_Combat), nameof(VehiclePawnFillageInterceptReroute)));
      }

      yield return instruction;
    }
  }

  private static void ProjectileMapToWorld(Thing __instance)
  {
    if (__instance is Projectile projectile &&
      projectile.GetComp<CompProjectileExitMap>() is { } comp)
    {
      comp.LeaveMap();
    }
  }

  private static IEnumerable<CodeInstruction> VehicleProjectileInterceptor(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      // if (t is Pawn and not VehiclePawn)
      if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder
        {
          LocalIndex: 10
        })
      {
        // Stloc_S : 10
        yield return instruction;
        instruction = instructionList[++i];
        // Ldloc_S : 10
        yield return instruction;
        instruction = instructionList[++i];

        yield return new CodeInstruction(opcode: OpCodes.Call,
          AccessTools.Method(typeof(Patch_Combat), nameof(VehiclePawnFillageInterceptReroute)));
      }

      yield return instruction;
    }
  }

  private static bool AffectVehicleInCell(Explosion __instance, IntVec3 c)
  {
    if (__instance.Map.GetDetachedMapComponent<VehiclePositionManager>().ClaimedBy(c) is
      { } vehicle)
    {
      // If cell is not on edge of vehicle, block explosion
      return vehicle.OccupiedRect().EdgeCells.Contains(c);
    }
    return true;
  }

  private static void VehicleMultipleExplosionInstances(Thing t, ref List<Thing> damagedThings,
    List<Thing> ignoredThings)
  {
    if (t is VehiclePawn vehicle)
    {
      if (ignoredThings != null && ignoredThings.Contains(t))
      {
        return;
      }
      damagedThings.Remove(vehicle);
    }
  }

  private static IEnumerable<CodeInstruction> VehicleExplosionDamageTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    MethodInfo takeDamageMethod = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(takeDamageMethod))
      {
        //Clear stack for rerouted call
        yield return new CodeInstruction(opcode: OpCodes.Pop); //ldarg.2 : thing
        yield return new CodeInstruction(opcode: OpCodes.Pop); //ldloc.1 : dinfo

        yield return new CodeInstruction(opcode: OpCodes.Ldloc_1); //dinfo
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_2); //thing
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_S, 5); //cell
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_Combat),
            nameof(TakeDamageReroute)));

        instruction = instructionList[++i];
      }
      yield return instruction;
    }
  }

  private static DamageWorker.DamageResult TakeDamageReroute(DamageInfo dinfo, Thing thing,
    IntVec3 cell)
  {
    if (thing is VehiclePawn vehicle &&
      vehicle.TryTakeDamage(dinfo, cell, out DamageWorker.DamageResult result))
    {
      return result;
    }
    return thing.TakeDamage(dinfo);
  }

  private static Pawn VehiclePawnFillageInterceptReroute(Pawn pawn)
  {
    // If pawn is vehicle, assign back to null to avoid "stance" based interception chance
    // and fall through to fillage.
    return pawn is VehiclePawn ? null : pawn;
  }
}