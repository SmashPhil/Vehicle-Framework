using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using UnityEngine;
using Vehicles.Rendering;
using Vehicles.World;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace Vehicles;

internal class Patch_Rendering : IPatchCategory
{
  /// <summary>
  /// Const values from <see cref="CellInspectorDrawer"/>
  /// </summary>
  public const float DistFromMouse = 26f;

  public const float LabelColumnWidth = 130f;
  public const float InfoColumnWidth = 170f;
  public const float WindowPadding = 12f;
  public const float ColumnPadding = 12f;
  public const float LineHeight = 24f;
  public const float ThingIconSize = 22f;
  public const float WindowWidth = 336f;

  private static readonly List<AerialVehicleInFlight> tmpAerialVehicles = [];

  private static readonly List<Pawn> tmpPawns = [];

  public static MethodInfo TrueCenter_Thing { get; private set; }

  public static MethodInfo TrueCenter_Baseline { get; private set; }

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

  void IPatchCategory.PatchMethods()
  {
    TrueCenter_Thing = AccessTools.Method(typeof(GenThing), nameof(GenThing.TrueCenter),
      parameters: [typeof(Thing)]);
    TrueCenter_Baseline = AccessTools.Method(typeof(GenThing), nameof(GenThing.TrueCenter),
      parameters: [typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(float)]);

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_RotationTracker),
        nameof(Pawn_RotationTracker.UpdateRotation)),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(UpdateVehicleRotation)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawIconsVehicles)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(ColonistBar), "CheckRecacheEntries"),
      transpiler: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(CheckRecacheAerialVehicleEntriesTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawSelectionBracketsVehicles)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(CellInspectorDrawer), "DrawThingRow"),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(CellInspectorDrawVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn), nameof(Pawn.ProcessPostTickVisuals)),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(ProcessVehiclePostTickVisuals)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GhostDrawer), nameof(GhostDrawer.DrawGhostThing)),
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawGhostVehicle)));
    HarmonyPatcher.Patch(original: TrueCenter_Thing,
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(TrueCenterVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(PawnRenderer), "ParallelGetPreRenderResults"),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DisableCachingPawnOverlays)));

    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterOnGUI)),
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawTargeters)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)),
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(ProcessTargeterInputEvents)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)),
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(TargeterUpdate)));
  }

  /// <summary>
  /// Use own Vehicle rotation to disallow moving rotation for various tasks such as Drafted
  /// </summary>
  public static bool UpdateVehicleRotation(Pawn ___pawn)
  {
    if (___pawn is VehiclePawn vehicle)
    {
      if (vehicle.Destroyed || vehicle.jobs.HandlingFacing)
      {
        return false;
      }
      if (vehicle.vehiclePather.Moving)
      {
        if (vehicle.vehiclePather.curPath == null || vehicle.vehiclePather.curPath.NodesLeft < 1)
        {
          return false;
        }
        vehicle.UpdateRotationAndAngle();
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Render small vehicle icon on colonist bar picture rect if they are currently onboard a vehicle
  /// </summary>
  /// <param name="rect"></param>
  /// <param name="colonist"></param>
  public static void DrawIconsVehicles(Rect rect, Pawn colonist)
  {
    if (colonist.Dead || colonist.ParentHolder is not VehicleRoleHandler handler)
      return;

    // Transient vehicles won't have icons cached
    if (!VehicleTex.CachedTextureIcons.TryGetValue(handler.vehicle.VehicleDef,
        out Texture2D icon) || !icon)
      return;

    float num = 20f * Find.ColonistBar.Scale;
    Vector2 vector = new(rect.xMax - num - 1f, rect.yMax - num - 1f);

    Rect rect2 = new(vector.x, vector.y, num, num);
    GUI.DrawTexture(rect2, icon);
    TooltipHandler.TipRegion(rect2,
      "VF_ActivityIconOnBoardShip".Translate(handler.vehicle.Label));
    vector.x += num;
  }

  public static IEnumerable<CodeInstruction> CheckRecacheAerialVehicleEntriesTranspiler(
    IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    MethodInfo clearCachedEntriesMethod =
      AccessTools.Method(typeof(List<int>), nameof(List<int>.Clear));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(clearCachedEntriesMethod))
      {
        yield return instruction; //CALLVIRT : List<int32>.Clear
        instruction = instructionList[++i];

        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0);
        yield return new CodeInstruction(opcode: OpCodes.Ldfld,
          operand: AccessTools.Field(typeof(ColonistBar), "cachedEntries"));
        yield return new CodeInstruction(opcode: OpCodes.Ldloca, operand: 0);
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(Patch_Rendering),
            nameof(RecacheAerialVehicleEntries)));
      }

      yield return instruction;
    }
  }

  private static void RecacheAerialVehicleEntries(List<ColonistBar.Entry> cachedEntries,
    ref int group)
  {
    tmpAerialVehicles.Clear();
    tmpAerialVehicles.AddRange(VehicleWorldObjectsHolder.Instance.AerialVehicles);
    tmpAerialVehicles.SortBy(aerialVehicle => aerialVehicle.ID);
    foreach (AerialVehicleInFlight aerialVehicle in tmpAerialVehicles)
    {
      if (aerialVehicle.IsPlayerControlled)
      {
        tmpPawns.Clear();
        tmpPawns.AddRange(aerialVehicle.vehicle.AllPawnsAboard);
        PlayerPawnsDisplayOrderUtility.Sort(tmpPawns);
        foreach (Pawn pawn in tmpPawns)
        {
          if (pawn.IsColonist)
          {
            cachedEntries.Add(new ColonistBar.Entry(pawn, null, group));
          }
        }
        group++;
      }
    }
  }

  /// <summary>
  /// Draw diagonal and shifted brackets for Boats
  /// </summary>
  /// <param name="obj"></param>
  public static bool DrawSelectionBracketsVehicles(object obj, Material overrideMat)
  {
    var vehicle = obj as VehiclePawn;
    var building = obj as VehicleBuilding;
    if (vehicle != null || building?.vehicle != null)
    {
      if (vehicle is null)
      {
        vehicle = building.vehicle;
      }
      Vector3[] brackets = new Vector3[4];
      float angle = vehicle.Angle;

      Ext_Pawn.CalculateSelectionBracketPositionsWorldForMultiCellPawns(brackets, vehicle,
        vehicle.DrawPos, vehicle.RotatedSize.ToVector2(), SelectionDrawer.SelectTimes,
        Vector2.one, angle, 1f);

      int num = Mathf.CeilToInt(angle);
      for (int i = 0; i < 4; i++)
      {
        Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
        Graphics.DrawMesh(MeshPool.plane10, brackets[i], rotation,
          overrideMat ?? MaterialPresets.SelectionBracketMat, 0);
        num -= 90;
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Divert render call to instead render full vehicle in UI
  /// </summary>
  /// <param name="thing"></param>
  public static bool CellInspectorDrawVehicle(Thing thing, ref int ___numLines)
  {
    if (thing is VehiclePawn vehicle)
    {
      float num = ___numLines * LineHeight;
      List<object> selectedObjects = Find.Selector.SelectedObjects;
      Rect rect = new Rect(LineHeight / 2, num + LineHeight / 2, WindowWidth - LineHeight,
        LineHeight);
      if (selectedObjects.Contains(thing))
      {
        Widgets.DrawHighlight(rect);
      }
      else if (___numLines % 2 == 1)
      {
        Widgets.DrawLightHighlight(rect);
      }
      rect = new Rect(LineHeight, num + LineHeight / 2 + 1f, ThingIconSize, ThingIconSize);
      VehicleGraphics.DrawVehicle(rect, vehicle);
      rect = new Rect(58f, num + LineHeight / 2, 370f, LineHeight);
      Widgets.Label(rect, thing.LabelMouseover);
      ___numLines++;
      return false;
    }
    return true;
  }

  public static bool ProcessVehiclePostTickVisuals(Pawn __instance, int ticksPassed,
    CellRect viewRect)
  {
    if (__instance is VehiclePawn vehicle)
    {
      vehicle.ProcessPostTickVisuals(ticksPassed, viewRect);
      return false;
    }
    return true;
  }

  private static void DrawGhostVehicle(IntVec3 center, Rot8 rot, ThingDef thingDef,
    Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
  {
    if (thingDef is VehicleBuildDef def)
    {
      VehicleDef vehicleDef = def.thingToSpawn;
      VehicleGhostUtility.DrawGhostOverlays(center, rot, vehicleDef, baseGraphic, ghostCol,
        drawAltitude, thing: thing);
    }
  }

  private static bool TrueCenterVehicle(Thing t, ref Vector3 __result)
  {
    if (t is VehiclePawn vehicle)
    {
      __result = vehicle.TrueCenter();
      return false;
    }
    return true;
  }

  private static void DisableCachingPawnOverlays(Pawn ___pawn, ref bool disableCache)
  {
    if (___pawn.InVehicle())
    {
      disableCache = true;
    }
  }

  /* ---------------- Hooks onto Targeter calls ---------------- */
  private static void DrawTargeters()
  {
    Targeters.OnGUITargeter();
  }

  private static void ProcessTargeterInputEvents()
  {
    Targeters.ProcessTargeterInputEvent();
  }

  private static void TargeterUpdate()
  {
    Targeters.UpdateTargeter();
  }
  /* ----------------------------------------------------------- */
}