using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Verse;

namespace Vehicles
{
  internal class Patch_MapHandling : IPatchCategory
  {
    public void PatchMethods()
    {
      // TODO 1.6 - test map generation for these 2
      VehicleHarmony.Patch(
        original: AccessTools.PropertyGetter(typeof(TileMutatorWorker_Coast), "CoastOffset"),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(CoastSizeMultiplier)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(TileMutatorWorker_River), "GetRiverWidthAt"),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(RiverNodeWidth)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(TileFinder),
          nameof(TileFinder.RandomSettlementTileFor),
          parameters:
          [typeof(PlanetLayer), typeof(Faction), typeof(bool), typeof(Predicate<PlanetTile>)]),
        transpiler: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(PushSettlementToCoastTranspiler)));
      VehicleHarmony.Patch(
        original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval))
         .GetGetMethod(),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(AnyVehicleBlockingMapRemoval)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(GasGrid), nameof(GasGrid.GasCanMoveTo)),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(GasCanMoveThroughVehicle)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate)),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(DebugUpdateVehicleRegions)));
      VehicleHarmony.Patch(
        original: AccessTools.Method(typeof(MapInterface),
          nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs)),
        postfix: new HarmonyMethod(typeof(Patch_MapHandling),
          nameof(DebugOnGUIVehicleRegions)));
    }

    /// <summary>
    /// Modify the randomized coastline width to enable coastal travel to operate more smoothly.
    /// </summary>
    private static void CoastSizeMultiplier(ref FloatRange __result)
    {
      __result *= VehicleMod.settings.main.beachMultiplier;
    }

    /// <summary>
    /// Apply ModSettings multiplier to river size to enable players to tweak the map to
    /// better suit vehicles. (eg. more water for boats or less for more land vehicle usage)
    /// </summary>
    private static void RiverNodeWidth(ref float __result)
    {
      __result *= VehicleMod.settings.main.riverMultiplier;
    }

    /// <summary>
    /// Move settlement's spawning location towards the coastline with radius r specified in the mod settings
    /// </summary>
    /// <param name="instructions"></param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> PushSettlementToCoastTranspiler(
      IEnumerable<CodeInstruction> instructions)
    {
      List<CodeInstruction> instructionList = instructions.ToList();

      for (int i = 0; i < instructionList.Count; i++)
      {
        CodeInstruction instruction = instructionList[i];

        if (instruction.opcode == OpCodes.Ldnull &&
          instructionList[i - 1].opcode == OpCodes.Ldloc_1)
        {
          //Call method, grab new location and store
          yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
          yield return new CodeInstruction(opcode: OpCodes.Call,
            operand: AccessTools.Method(typeof(WorldHelper),
              nameof(WorldHelper.PushSettlementToCoast)));
          yield return new CodeInstruction(opcode: OpCodes.Stloc_1);
          yield return new CodeInstruction(opcode: OpCodes.Ldloc_1);
        }

        yield return instruction;
      }
    }

    /// <summary>
    /// Ensure map is not removed with vehicles that contain pawns or maps currenty being targeted for landing.
    /// </summary>
    public static void AnyVehicleBlockingMapRemoval(ref bool __result, Map ___map)
    {
      if (__result is false)
      {
        if (LandingTargeter.Instance.IsTargeting && Current.Game.CurrentMap == ___map ||
          MapHelper.AnyVehicleSkyfallersBlockingMap(___map) ||
          MapHelper.AnyAerialVehiclesInRecon(___map))
        {
          __result = true;
          return;
        }

        foreach (VehiclePawn vehicle in ___map.GetDetachedMapComponent<VehiclePositionManager>()
         .AllClaimants)
        {
          if (vehicle.MovementPermissions == VehiclePermissions.Autonomous)
          {
            __result = true;
            return;
          }

          foreach (Pawn passenger in vehicle.AllPawnsAboard)
          {
            if (PawnKeepsMapOpen(passenger))
            {
              __result = true;
              return;
            }
          }
        }
      }
      return;

      static bool PawnKeepsMapOpen(Pawn pawn)
      {
        if (pawn is { Downed: false, IsColonist: true })
          return true;
        if (pawn.relations is { relativeInvolvedInRescueQuest: not null })
          return true;
        if (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer)
          return true;
        if (pawn is { CurJob.exitMapOnArrival: true })
          return true;
        return false;
      }
    }

    private static void GasCanMoveThroughVehicle(IntVec3 cell, ref bool __result, Map ___map)
    {
      if (__result)
      {
        VehiclePawn vehicle =
          ___map.GetDetachedMapComponent<VehiclePositionManager>().ClaimedBy(cell);
        __result = vehicle == null || vehicle.VehicleDef.Fillage != FillCategory.Full;
      }
    }

    public static void DebugUpdateVehicleRegions()
    {
      if (Find.CurrentMap != null && !WorldRendererUtility.WorldRendered &&
        DebugHelper.AnyDebugSettings)
      {
        DebugHelper.DebugDrawVehicleRegion(Find.CurrentMap);
      }
    }

    public static void DebugOnGUIVehicleRegions()
    {
      if (Find.CurrentMap != null && !WorldRendererUtility.WorldRendered &&
        DebugHelper.AnyDebugSettings)
      {
        DebugHelper.DebugDrawVehiclePathCostsOverlay(Find.CurrentMap);
      }
    }
  }
}