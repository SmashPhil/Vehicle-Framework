using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools.Patching;

namespace SmashTools;

public static class CameraController
{
  private static MethodInfo getSunShadowsViewRect_MethodInfo;
  private static int lastViewRectGetFrame = -1;

  private static Camera camera;

  private static bool Patched { get; set; }

  public static bool InUse { get; private set; }

  private static CellRect CurrentViewRect
  {
    get
    {
      if (Time.frameCount != lastViewRectGetFrame)
      {
        field = default;
        float num = UI.screenWidth / (float)UI.screenHeight;
        Vector3 currentRealPosition = Find.Camera.transform.position;
        field.minX = Mathf.FloorToInt(currentRealPosition.x * num - 1f);
        field.maxX = Mathf.CeilToInt(currentRealPosition.x * num);
        field.minZ = Mathf.FloorToInt(currentRealPosition.z - 1f);
        field.maxZ = Mathf.CeilToInt(currentRealPosition.z);
        lastViewRectGetFrame = Time.frameCount;
      }
      return field;
    }
  }

  public static void Update(Vector3 position)
  {
    camera.transform.position = new Vector3(position.x, camera.transform.position.y, position.z);
  }

  public static void Start(Camera camera)
  {
    if (InUse)
    {
      Log.Warning($"Attempting to start CameraController when it's already in use.");
      return;
    }
    InUse = true;
    CameraController.camera = camera;
    if (!Patched)
    {
      PatchOcclusionCulling();
    }
  }

  public static void Close()
  {
    InUse = false;
    camera = null;
  }

  private static void PatchOcclusionCulling()
  {
    if (!Patched)
    {
      Patched = true;
      try
      {
        getSunShadowsViewRect_MethodInfo =
          AccessTools.Method(typeof(MapDrawer), "GetSunShadowsViewRect");
        if (getSunShadowsViewRect_MethodInfo == null)
        {
          throw new NullReferenceException("MethodInfo fields");
        }
        Messages.Message("Patching CameraController to exclude from occlusion culling.",
          MessageTypeDefOf.NeutralEvent);
        HarmonyPatcher.Harmony.Patch(
          original: AccessTools.Method(typeof(DynamicDrawManager),
            nameof(DynamicDrawManager.DrawDynamicThings)),
          transpiler: new HarmonyMethod(typeof(CameraController),
            nameof(CameraViewRenderInRectTranspiler_Thing_Runtime)));
        HarmonyPatcher.Harmony.Patch(
          original: AccessTools.Method(typeof(CellRenderer), nameof(CellRenderer.RenderSpot),
            parameters: new Type[] { typeof(Vector3), typeof(Material), typeof(float) }),
          transpiler: new HarmonyMethod(typeof(CameraController),
            nameof(CameraViewRenderInRectTranspiler_TerrainVector3_Runtime)));
        HarmonyPatcher.Harmony.Patch(
          original: AccessTools.Method(typeof(CellRenderer), nameof(CellRenderer.RenderCell),
            parameters: new Type[] { typeof(IntVec3), typeof(Material) }),
          transpiler: new HarmonyMethod(typeof(CameraController),
            nameof(CameraViewRenderInRectTranspiler_TerrainIntVec3_Runtime)));
        HarmonyPatcher.Harmony.Patch(
          original: AccessTools.Method(typeof(MapDrawer), nameof(MapDrawer.DrawMapMesh)),
          postfix: new HarmonyMethod(typeof(CameraController),
            nameof(CameraViewRenderInRect_MapMesh_Runtime)));
        //-- TODO --
        //GenView.ShouldSpawnMotesAt
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Failed to patch CameraAttacher rect for occlusion culling. Attacher will not work outside main camera rect.\nException={ex}");
      }
    }
  }

  private static IEnumerable<CodeInstruction> CameraViewRenderInRectTranspiler_Thing_Runtime(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionsList = instructions.ToList();

    for (int i = 0; i < instructionsList.Count; i++)
    {
      CodeInstruction instruction = instructionsList[i];

      if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
      {
        yield return new CodeInstruction(opcode: OpCodes.Pop); //position
        yield return new CodeInstruction(opcode: OpCodes.Pop); //CellRect address
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_1); //CellRect
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 5); //thing
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CameraController),
            nameof(CameraOrCameraViewContainPosition_Thing)));
        instruction = instructionsList[++i]; //CALL : CellRect.Contains
      }

      yield return instruction;
    }
  }

  private static IEnumerable<CodeInstruction>
    CameraViewRenderInRectTranspiler_TerrainVector3_Runtime(
      IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionsList = instructions.ToList();

    for (int i = 0; i < instructionsList.Count; i++)
    {
      CodeInstruction instruction = instructionsList[i];

      if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
      {
        yield return new CodeInstruction(opcode: OpCodes.Pop); //position
        yield return new CodeInstruction(opcode: OpCodes.Pop); //CellRect address
        yield return new CodeInstruction(opcode: OpCodes.Ldsfld,
          AccessTools.Field(typeof(CellRenderer), "viewRect")); //CellRect
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0); //loc
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CameraController),
            nameof(CameraOrCameraViewContainPosition_Vector3)));
        instruction = instructionsList[++i]; //CALL : CellRect.Contains
      }

      yield return instruction;
    }
  }

  private static IEnumerable<CodeInstruction>
    CameraViewRenderInRectTranspiler_TerrainIntVec3_Runtime(
      IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionsList = instructions.ToList();

    for (int i = 0; i < instructionsList.Count; i++)
    {
      CodeInstruction instruction = instructionsList[i];

      if (instruction.Calls(AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains))))
      {
        yield return new CodeInstruction(opcode: OpCodes.Pop); //viewRect
        yield return new CodeInstruction(opcode: OpCodes.Pop); //cell
        yield return new CodeInstruction(opcode: OpCodes.Ldsfld,
          AccessTools.Field(typeof(CellRenderer), "viewRect")); //CellRect
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_0); //cell
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(CameraController),
            nameof(CameraOrCameraViewContainPosition_IntVec3)));
        instruction = instructionsList[++i]; //CALL : CellRect.Contains
      }

      yield return instruction;
    }
  }

  private static void CameraViewRenderInRect_MapMesh_Runtime(MapDrawer __instance, Map ___map,
    Section[,] ___sections)
  {
    if (InUse)
    {
      CellRect currentViewRect = VisibleSections(___map, CurrentViewRect);
      CellRect visibleSections = VisibleSections(___map, Find.CameraDriver.CurrentViewRect);
      foreach (IntVec3 intVec in currentViewRect.CellsNoOverlap(visibleSections))
      {
        Section section = ___sections[intVec.x, intVec.z];
        section.DrawSection();
      }
    }
  }

  private static bool CameraOrCameraViewContainPosition_Thing(CellRect cellRect, Thing thing)
  {
    if (InUse)
    {
      return cellRect.Contains(thing.Position) ||
        CurrentViewRect.Contains(thing.DrawPos.ToIntVec3());
    }
    return cellRect.Contains(thing.Position);
  }

  private static bool CameraOrCameraViewContainPosition_IntVec3(CellRect cellRect, IntVec3 cell)
  {
    return CameraOrCameraViewContainPosition_Vector3(cellRect,
      cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays));
  }

  private static bool CameraOrCameraViewContainPosition_Vector3(CellRect cellRect, Vector3 loc)
  {
    IntVec3 cell = loc.ToIntVec3();
    if (InUse)
    {
      return cellRect.Contains(cell) || CurrentViewRect.Contains(cell);
    }
    return cellRect.Contains(cell);
  }

  private static CellRect VisibleSections(Map map, CellRect viewRect)
  {
    CellRect sunShadowsViewRect =
      (CellRect)getSunShadowsViewRect_MethodInfo.Invoke(map.mapDrawer, new object[] { viewRect });
    sunShadowsViewRect.ClipInsideMap(map);
    IntVec2 intVec = SectionCoordsAt(sunShadowsViewRect.Min);
    IntVec2 intVec2 = SectionCoordsAt(sunShadowsViewRect.Max);
    if (intVec2.x < intVec.x || intVec2.z < intVec.z)
    {
      return CellRect.Empty;
    }
    return CellRect.FromLimits(intVec.x, intVec.z, intVec2.x, intVec2.z);
  }

  private static IntVec2 SectionCoordsAt(IntVec3 loc)
  {
    return new IntVec2(Mathf.FloorToInt((loc.x / 17)), Mathf.FloorToInt((loc.z / 17)));
  }
}