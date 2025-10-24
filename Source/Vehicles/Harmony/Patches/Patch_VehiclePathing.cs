using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_VehiclePathing : IPatchCategory
{
  private static readonly List<VehiclePawn> MultiSelectGotoList = [];

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(FloatMenuOptionProvider_DraftedMove), "PawnCanGoto"),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MultiselectVehicleGotoBlocked)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(FloatMenuOptionProvider),
        nameof(FloatMenuOptionProvider.Applies)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(DontVanillaDraftMoveVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Selector), "HandleMultiselectGoto"),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MultiselectGotoDraggingBlocked)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_JobTracker),
        nameof(Pawn_JobTracker.IsCurrentJobPlayerInterruptible)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(JobInterruptibleForVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_PathFollower),
        nameof(Pawn_PathFollower.StartPath)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(StartVehiclePath)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GenAdj), nameof(GenAdj.AdjacentTo8WayOrInside),
        parameters: [typeof(IntVec3), typeof(Thing)]),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(AdjacentTo8WayOrInsideVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GenAdj), nameof(GenAdj.OccupiedRect),
        parameters: [typeof(Thing)]),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(OccupiedRectVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pathing),
        nameof(Pathing.RecalculatePerceivedPathCostAt)),
      postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(RecalculatePerceivedPathCostForVehicle)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(TerrainGrid), "DoTerrainChangedEffects"),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(SetTerrainAndUpdateVehiclePathCosts)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.DeSpawn)),
      transpiler: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(DeSpawnAndUpdateVehicleRegionsTranspiler)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.SpawnSetup)),
      transpiler: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(SpawnAndUpdateVehicleRegionsTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position)),
      postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(SetPositionAndUpdateVehicleRegions)));
    HarmonyPatcher.Patch(
      original: AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Rotation)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(SetRotationAndUpdateVehicleRegionsClipping)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.Register)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MonitorThingGridRegisterStart)),
      finalizer: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MonitorThingGridRegisterEnd)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ThingGrid), nameof(ThingGrid.Deregister)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MonitorThingGridDeregisterStart)),
      finalizer: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(MonitorThingGridDeregisterEnd)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GenStep_RocksFromGrid),
        nameof(GenStep_RocksFromGrid.Generate)),
      prefix: new HarmonyMethod(typeof(Patch_VehiclePathing),
        nameof(DisableRegionUpdatingRockGen)));
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.DisableIncrementalDirtying)),
			postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
			nameof(BeginPathGridCapture)));
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(PathGrid), nameof(PathGrid.ReEnableIncrementalDirtying)),
			postfix: new HarmonyMethod(typeof(Patch_VehiclePathing),
			nameof(EndPathGridCapture)));
	}

  private static bool MultiselectVehicleGotoBlocked(Pawn pawn, ref AcceptanceReport __result)
  {
    if (pawn is VehiclePawn)
    {
      __result = false;
      return false;
    }
    return true;
  }

  private static bool DontVanillaDraftMoveVehicles(ref bool __result,
    FloatMenuOptionProvider __instance, FloatMenuContext context)
  {
    if (__instance is not FloatMenuOptionProvider_Vehicle && !context.IsMultiselect &&
      context.FirstSelectedPawn is VehiclePawn)
    {
      // DraftedMove is handled by FloatMenuOptionProvider_OrderVehicle for vehicles
      __result = false;
      return false;
    }
    return true;
  }

  private static bool MultiselectGotoDraggingBlocked(FloatMenuContext context)
  {
    if (context.IsMultiselect)
    {
      if (context.allSelectedPawns.All(pawn => pawn is VehiclePawn))
      {
        Assert.AreEqual(MultiSelectGotoList.Count, 0);
        MultiSelectGotoList.AddRange(context.allSelectedPawns.Cast<VehiclePawn>());
        if (!PathingHelper.TryFindNearestStandableCell(MultiSelectGotoList.FirstOrDefault(),
          context.ClickedCell, out IntVec3 result))
        {
          return false;
        }
        VehicleOrientationController.StartOrienting(MultiSelectGotoList, result,
          context.ClickedCell);
        MultiSelectGotoList.Clear();
        return false;
      }
      // Remove any vehicles if not all are vehicles, preventing vanilla assigned position goto's
      for (int i = context.allSelectedPawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = context.allSelectedPawns[i];
        if (pawn is VehiclePawn)
          context.allSelectedPawns.RemoveAt(i);
      }
    }
    return true;
  }

  /// <summary>
  /// Bypass vanilla check for now, since it forces on-fire pawns to not be able to interrupt jobs which obviously shouldn't apply to vehicles.
  /// </summary>
  private static bool JobInterruptibleForVehicle(Pawn_JobTracker __instance, Pawn ___pawn,
    ref bool __result)
  {
    if (___pawn is VehiclePawn)
    {
      if (__instance.curJob == null || __instance.curDriver == null)
      {
        __result = true;
        return false;
      }
      __result = __instance is
        { curJob.def.playerInterruptible: true } or
        { curDriver.PlayerInterruptable: true };
      return false;
    }
    return true;
  }

  /// <summary>
  /// StartPath hook to divert to vehicle related pather
  /// </summary>
  /// <param name="dest"></param>
  /// <param name="peMode"></param>
  /// <param name="___pawn"></param>
  private static bool StartVehiclePath(LocalTargetInfo dest, PathEndMode peMode, Pawn ___pawn)
  {
    if (___pawn is VehiclePawn vehicle)
    {
      vehicle.vehiclePather.StartPath(dest, peMode);
      return false;
    }
    return true;
  }

  private static bool AdjacentTo8WayOrInsideVehicle(IntVec3 root, Thing t, ref bool __result)
  {
    if (t is VehiclePawn vehicle)
    {
      IntVec2 size = vehicle.def.size;
      Rot4 rot = vehicle.Rotation;
      Ext_Vehicles.AdjustForVehicleOccupiedRect(ref size, ref rot);
      __result = root.AdjacentTo8WayOrInside(vehicle.Position, rot, size);
      return false;
    }

    return true;
  }

  private static bool OccupiedRectVehicles(Thing t, ref CellRect __result)
  {
    if (t is VehiclePawn vehicle)
    {
      __result = vehicle.VehicleRect();
      return false;
    }

    return true;
  }

  private static void RecalculatePerceivedPathCostForVehicle(IntVec3 c, PathingContext ___normal)
  {
    PathingHelper.RecalculatePerceivedPathCostAt(c, ___normal.map);
  }

  /// <summary>
  /// Pass <paramref name="c"/> by reference to allow Harmony to skip prefix method when MapPreview skips it during preview generation
  /// </summary>
  /// <param name="c"></param>
  /// <param name="___map"></param>
  private static void SetTerrainAndUpdateVehiclePathCosts(ref IntVec3 c, Map ___map)
  {
    if (Current.ProgramState == ProgramState.Playing)
    {
      PathingHelper.RecalculatePerceivedPathCostAt(c, ___map);
    }
  }

  private static IEnumerable<CodeInstruction> DeSpawnAndUpdateVehicleRegionsTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    MethodInfo coverGridDeregisterMethod = AccessTools.Method(typeof(TickManager),
      nameof(TickManager.DeRegisterAllTickabilityFor));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(coverGridDeregisterMethod))
      {
        yield return instruction;
        instruction = instructionList[++i];

        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_VehiclePathing),
            nameof(Patch_VehiclePathing.DeSpawnAndNotifyVehicleRegions)));
      }

      yield return instruction;
    }
  }

  private static IEnumerable<CodeInstruction> SpawnAndUpdateVehicleRegionsTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    MethodInfo coverGridDeregisterMethod =
      AccessTools.Method(typeof(CoverGrid), nameof(CoverGrid.Register));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(coverGridDeregisterMethod))
      {
        yield return instruction;
        instruction = instructionList[++i];

        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_VehiclePathing),
            nameof(Patch_VehiclePathing.SpawnAndNotifyVehicleRegions)));
      }

      yield return instruction;
    }
  }

  private static void SetPositionAndUpdateVehicleRegions(Thing __instance)
  {
    if (__instance.Spawned)
    {
      if (__instance is VehiclePawn vehicle)
        vehicle.ReclaimPosition();
      PathingHelper.ThingAffectingRegionsOrientationChanged(__instance, __instance.Map);
    }
  }

  private static bool SetRotationAndUpdateVehicleRegionsClipping(Thing __instance, Rot4 value,
    ref Rot4 ___rotationInt)
  {
    if (__instance is VehiclePawn vehicle)
    {
      vehicle.SetRotationInt(value, ref ___rotationInt);
      return false;
    }

    return true;
  }

  private static void SetRotationAndUpdateVehicleRegions(Thing __instance)
  {
    if (__instance.Spawned && (__instance.def.size.x != 1 || __instance.def.size.z != 1))
    {
      PathingHelper.ThingAffectingRegionsOrientationChanged(__instance, __instance.Map);
    }
  }

  private static void MonitorThingGridRegisterStart(ThingGrid __instance)
  {
    Monitor.Enter(__instance);
  }

  private static void MonitorThingGridRegisterEnd(ThingGrid __instance)
  {
    Monitor.Exit(__instance);
  }

  private static void MonitorThingGridDeregisterStart(ThingGrid __instance)
  {
    Monitor.Enter(__instance);
  }

  private static void MonitorThingGridDeregisterEnd(ThingGrid __instance)
  {
    Monitor.Exit(__instance);
  }

  private static void DisableRegionUpdatingRockGen(Map map)
  {
    if (!map.TileInfo.WaterCovered)
    {
      map.GetCachedMapComponent<VehiclePathingSystem>().DisableAllRegionUpdaters();
    }
  }

	private static void BeginPathGridCapture(Map ___map)
	{
		___map.GetCachedMapComponent<VehiclePathingSystem>().BeginCapturingPathGridDirtying();
	}

	private static void EndPathGridCapture(Map ___map)
	{
		___map.GetCachedMapComponent<VehiclePathingSystem>().EndCapturingPathGridDirtying();
	}

	/* ---- Helper Methods related to patches ---- */

	private static void SpawnAndNotifyVehicleRegions(Thing thing, Map map)
  {
    PathingHelper.ThingAffectingRegionsStateChange(thing, map, true);
  }

  private static void DeSpawnAndNotifyVehicleRegions(Thing thing, Map map)
  {
    PathingHelper.ThingAffectingRegionsStateChange(thing, map, false);
  }
}