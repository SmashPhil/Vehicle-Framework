using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using UnityEngine;
using Vehicles.Rendering;
using Verse;

namespace Vehicles;

internal class Patch_Rendering : IPatchCategory
{
  private const float LineHeight = 24f;
  private const float ThingIconSize = 22f;
  private const float WindowWidth = 336f;

  private static readonly FastInvokeHandler RenderPulsingOverlayInvoker;

  private static VehiclePawn vehicleForPortrait;
  private static readonly VehiclePortrait CellDrawerPortrait = new(new VehiclePortrait.Config
  {
    expiryTime = 5 // seconds
  });

  static Patch_Rendering()
  {
    RenderPulsingOverlayInvoker = MethodInvoker.GetHandler(AccessTools.Method(typeof(OverlayDrawer),
      "RenderPulsingOverlay", parameters: [typeof(Thing), typeof(Material), typeof(int), typeof(bool)]));
  }

  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(Pawn_RotationTracker),
        nameof(Pawn_RotationTracker.UpdateRotation)),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(UpdateVehicleRotation)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawIconsVehicles)));
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
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(GenThing), nameof(GenThing.TrueCenter),
        parameters: [typeof(Thing)]),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(TrueCenterVehicle)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(PawnRenderer), "ParallelGetPreRenderResults"),
      prefix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DisableCachingPawnOverlays)));
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(OverlayDrawer), "RenderOutOfFuelOverlay"),
      prefix: new HarmonyMethod(typeof(Patch_Rendering), nameof(RenderVehicleOutOfFuelOverlay)));

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
  private static bool UpdateVehicleRotation(Pawn ___pawn)
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
  private static void DrawIconsVehicles(Rect rect, Pawn colonist)
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

  /// <summary>
  /// Draw diagonal and shifted brackets for Boats
  /// </summary>
  private static bool DrawSelectionBracketsVehicles(object obj, Material overrideMat)
  {
    VehiclePawn vehicle = obj as VehiclePawn ?? (obj as VehicleBuilding)?.vehicle;
    if (vehicle != null)
    {
      Vector3[] brackets = new Vector3[4];
      float angle = vehicle.Angle + vehicle.Transform.rotation;

      Ext_Pawn.CalculateSelectionBracketPositionsWorldForMultiCellPawns(brackets, vehicle,
        vehicle.DrawPos, vehicle.RotatedSize.ToVector2(), SelectionDrawer.SelectTimes, Vector2.one,
        angle);

      int num = Mathf.CeilToInt(angle);
      for (int i = 0; i < 4; i++)
      {
        Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
        Material material = overrideMat ? overrideMat : MaterialPresets.SelectionBracketMat;
        Graphics.DrawMesh(MeshPool.plane10, brackets[i], rotation, material, 0);
        num -= 90;
      }
      return false;
    }
    return true;
  }

  /// <summary>
  /// Divert render call to instead render full vehicle in UI
  /// </summary>
  private static bool CellInspectorDrawVehicle(Thing thing, ref int ___numLines)
  {
    if (thing is VehiclePawn vehicle)
    {
      float num = ___numLines * LineHeight;
      List<object> selectedObjects = Find.Selector.SelectedObjects;
      Rect rect = new(LineHeight / 2, num + LineHeight / 2, WindowWidth - LineHeight, LineHeight);
      if (selectedObjects.Contains(thing))
      {
        Widgets.DrawHighlight(rect);
      }
      else if (___numLines % 2 == 1)
      {
        Widgets.DrawLightHighlight(rect);
      }
      rect = new Rect(LineHeight, num + LineHeight / 2 + 1f, ThingIconSize, ThingIconSize);
      BlitRequest request = BlitRequest.For(vehicle);
      if (vehicleForPortrait != vehicle)
      {
        vehicleForPortrait = vehicle;
        CellDrawerPortrait.MarkDirty();
      }
      CellDrawerPortrait.Draw(rect, request);

      rect = new Rect(58f, num + LineHeight / 2, 370f, LineHeight);
      Widgets.Label(rect, thing.LabelMouseover);
      ___numLines++;
      return false;
    }
    return true;
  }

  private static bool ProcessVehiclePostTickVisuals(Pawn __instance, int ticksPassed,
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
      VehicleGhostUtility.DrawData data = new(vehicleDef)
      {
        center = center,
        rot = rot,
        ghostColor = ghostCol,
        altitude = drawAltitude
      };
      VehicleGhostUtility.DrawGhostOverlays(in data, baseGraphic, angle: 0);
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

  // ReSharper disable once InconsistentNaming
  private static bool RenderVehicleOutOfFuelOverlay(Thing t, OverlayDrawer __instance, Material ___OutOfFuelMat)
  {
    if (t is VehiclePawn { CompFueledTravel: not null } vehicle)
    {
      const int DrawIndex = 5;
      const bool DoAtlasOffsetIncrement = true;
      var props = vehicle.CompFueledTravel.Props;
      Material fuelMat = MaterialPool.MatFrom(props.FuelIcon ? props.FuelIcon : ThingDefOf.Chemfuel.uiIcon,
        ShaderDatabase.MetaOverlay, Color.white);
      RenderPulsingOverlayInvoker.Invoke(__instance, t, fuelMat, DrawIndex, !DoAtlasOffsetIncrement);
      RenderPulsingOverlayInvoker.Invoke(__instance, t, ___OutOfFuelMat, DrawIndex, DoAtlasOffsetIncrement);
      return false;
    }
    return true;
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