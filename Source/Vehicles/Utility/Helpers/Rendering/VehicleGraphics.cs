using System;
using System.Collections.Generic;
using System.Linq;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Rendering;

public static class VehicleGraphics
{
  private static readonly OverlayGUIRenderer overlayRenderer = new();

  /// <summary>
  /// DrawOffset for full rotation <paramref name="rot"/>
  /// </summary>
  /// <param name="graphic"></param>
  /// <param name="rot"></param>
  public static Vector3 DrawOffsetFull(this Graphic graphic, Rot8 rot)
  {
    return graphic.data.DrawOffsetFull(rot);
  }

  /// <summary>
  /// DrawOffset for full rotation <paramref name="rot"/>
  /// </summary>
  /// <param name="graphicData"></param>
  /// <param name="rot"></param>
  public static Vector3 DrawOffsetFull(this GraphicData graphicData, Rot8 rot)
  {
    Vector2 offset = VehicleDrawOffset(rot, graphicData.drawOffset.x, graphicData.drawOffset.y);
    return new Vector3(offset.x, graphicData.drawOffset.y, offset.y);
  }

  /// <summary>
  /// Calculate draw offset given offsets from center rotated alongside <paramref name="rot"/>
  /// </summary>
  public static Vector2 VehicleDrawOffset(Rot8 rot, float offsetX, float offsetY,
    float additionalRotation = 0)
  {
    return Ext_Math.RotatePointClockwise(offsetX, offsetY, rot.AsAngle + additionalRotation);
  }

  public static Rect AdjustRectToVehicleDef(VehicleDef vehicleDef, Rect rect, Rot8 rot)
  {
    Vector2 rectSize = vehicleDef.ScaleDrawRatio(rect.size);

    bool elongated = rot.IsHorizontal || rot.IsDiagonal;

    Vector2 displayOffset = vehicleDef.drawProperties.DisplayOffsetForRot(rot);
    float scaledWidth = rectSize.x;
    float scaledHeight = rectSize.y;
    if (elongated)
    {
      scaledWidth = rectSize.y;
      scaledHeight = rectSize.x;
    }
    float offsetX = (rect.width - scaledWidth) / 2 + (displayOffset.x * rect.width);
    float offsetY = (rect.height - scaledHeight) / 2 + (displayOffset.y * rect.height);

    return new Rect(rect.x + offsetX, rect.y + offsetY, scaledWidth, scaledHeight);
  }

  [Obsolete("Use VehiclePortrait with BlitRequest instead.")]
  public static void DrawVehicle(Rect rect, VehiclePawn vehicle, Rot8? rot = null,
    List<GraphicOverlay> extraOverlays = null, List<VehicleTurret> extraTurrets = null,
    List<string> excludeTurrets = null)
  {
    DrawVehicle(rect, vehicle, vehicle.patternData, rot: rot, extraOverlays: extraOverlays,
      extraTurrets: extraTurrets, excludeTurrets: excludeTurrets);
  }

  [Obsolete("Use VehiclePortrait with BlitRequest instead.")]
  public static void DrawVehicle(Rect rect, VehiclePawn vehicle, PatternData patternData,
    Rot8? rot = null, bool withoutTurrets = false, List<GraphicOverlay> extraOverlays = null,
    List<VehicleTurret> extraTurrets = null, List<string> excludeTurrets = null)
  {
    VehicleDef vehicleDef = vehicle.VehicleDef;
    try
    {
      overlayRenderer.Clear();
      if (!Mathf.Approximately(rect.width, rect.height))
      {
        Log.WarningOnce(
          "Drawing VehicleDef with non-uniform rect. VehicleDefs are best drawn in square rects which will then be adjusted to fit.",
          nameof(DrawVehicleDef).GetHashCode());
      }

      Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;
      Rect adjustedRect = AdjustRectToVehicleDef(vehicleDef, rect, rotDrawn);

      Graphic_Vehicle graphic = vehicleDef.graphicData.Graphic as Graphic_Vehicle;
      Assert.IsNotNull(graphic);
      PatternData pattern = patternData;
      pattern ??= VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName,
        vehicleDef.graphicData);

      Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);

      Material material = null;
      if (graphic.Shader.SupportsRGBMaskTex())
      {
        material = RGBMaterialPool.Get(vehicleDef, rotDrawn);
        RGBMaterialPool.SetProperties(vehicleDef, pattern, graphic.TexAt, graphic.MaskAt);
      }

      if (vehicle.CompVehicleTurrets != null)
      {
        // NOTE: Temporary fix until Ludeon fixes vanilla bug with matrix rotations inside GUI groups
        if (!withoutTurrets)
        {
          foreach (RenderData turretRenderData in RetrieveAllTurretSettingsGraphicsProperties(
            rect, vehicleDef, rotDrawn,
            vehicle.CompVehicleTurrets.Turrets.OrderBy(t => t.drawLayer), pattern,
            excludeTurrets: excludeTurrets))
          {
            overlayRenderer.Add(turretRenderData);
          }
          if (!extraTurrets.NullOrEmpty())
          {
            foreach (RenderData turretRenderData in RetrieveAllTurretSettingsGraphicsProperties(
              rect, vehicleDef, rotDrawn, extraTurrets.OrderBy(t => t.drawLayer), pattern,
              excludeTurrets: excludeTurrets))
            {
              overlayRenderer.Add(turretRenderData);
            }
          }
        }
      }

      foreach (RenderData renderData in RetrieveAllOverlaySettingsGraphicsProperties(rect,
        vehicle, rotDrawn, pattern: pattern, extraOverlays: extraOverlays))
      {
        overlayRenderer.Add(renderData);
      }

      overlayRenderer.FinalizeForRendering();

      overlayRenderer.RenderLayer(GUILayer.Lower);

      DrawVehicleFitted(adjustedRect, angle, mainTex, material);

      overlayRenderer.RenderLayer(GUILayer.Upper);
    }
    catch (Exception ex)
    {
      Log.Error(
        $"Exception thrown while trying to draw Graphics VehicleDef=\"{vehicleDef?.defName ?? "Null"}\"\nException={ex}");
    }
    finally
    {
      overlayRenderer.Clear();
    }
  }

  /// <summary>
  /// Draw <paramref name="vehicleDef"/>
  /// </summary>
  /// <remarks><paramref name="material"/> may overwrite material used for vehicle</remarks>
  /// <param name="rect"></param>
  /// <param name="vehicleDef"></param>
  /// <param name="material"></param>
  public static void DrawVehicleDef(Rect rect, VehicleDef vehicleDef,
    PatternData patternData = null, Rot8? rot = null, bool withoutTurrets = false,
    List<GraphicOverlay> extraOverlays = null, List<VehicleTurret> extraTurrets = null,
    List<string> excludeTurrets = null)
  {
    try
    {
      overlayRenderer.Clear();
      if (rect.width != rect.height)
      {
        Log.WarningOnce(
          "Drawing VehicleDef with non-uniform rect. VehicleDefs are best drawn in square rects which will then be adjusted to fit.",
          nameof(DrawVehicleDef).GetHashCode());
      }

      Rot8 rotDrawn = rot ?? vehicleDef.drawProperties.displayRotation;
      Rect adjustedRect = AdjustRectToVehicleDef(vehicleDef, rect, rotDrawn);

      Graphic_Vehicle graphic = vehicleDef.graphicData.Graphic as Graphic_Vehicle;

      PatternData pattern = patternData;
      if (pattern is null)
      {
        pattern = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName,
          vehicleDef.graphicData);
      }

      Texture2D mainTex = VehicleTex.VehicleTexture(vehicleDef, rotDrawn, out float angle);

      Material material = null;
      if (graphic.Shader.SupportsRGBMaskTex())
      {
        material = RGBMaterialPool.Get(vehicleDef, rotDrawn);
        RGBMaterialPool.SetProperties(vehicleDef, pattern, graphic.TexAt, graphic.MaskAt);
      }

      if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is
        CompProperties_VehicleTurrets props)
      {
        if (!withoutTurrets)
        {
          foreach (RenderData turretRenderData in RetrieveAllTurretSettingsGraphicsProperties(
            rect, vehicleDef, rotDrawn, props.turrets.OrderBy(t => t.drawLayer), pattern,
            excludeTurrets: excludeTurrets))
          {
            overlayRenderer.Add(turretRenderData);
          }
          if (!extraTurrets.NullOrEmpty())
          {
            foreach (RenderData turretRenderData in RetrieveAllTurretSettingsGraphicsProperties(
              rect, vehicleDef, rotDrawn, extraTurrets.OrderBy(t => t.drawLayer), pattern,
              excludeTurrets: excludeTurrets))
            {
              overlayRenderer.Add(turretRenderData);
            }
          }
        }
      }

      foreach (RenderData renderData in RetrieveAllOverlaySettingsGraphicsProperties(rect,
        vehicleDef, rotDrawn, pattern: pattern, extraOverlays: extraOverlays))
      {
        overlayRenderer.Add(renderData);
      }

      overlayRenderer.FinalizeForRendering();

      overlayRenderer.RenderLayer(GUILayer.Lower);

      DrawVehicleFitted(adjustedRect, angle, mainTex, material);

      overlayRenderer.RenderLayer(GUILayer.Upper);
    }
    catch (Exception ex)
    {
      SmashLog.Error(
        $"Exception thrown while trying to draw Graphics <type>VehicleDef</type>=\"{vehicleDef?.defName ?? "Null"}\" Exception={ex}");
    }
    finally
    {
      overlayRenderer.Clear();
    }
  }

  public static IEnumerable<RenderData> RetrieveAllOverlaySettingsGraphicsProperties(Rect rect,
    VehiclePawn vehicle, Rot8 rot, PatternData pattern = null,
    List<GraphicOverlay> extraOverlays = null)
  {
    foreach (GraphicOverlay graphicOverlay in vehicle.DrawTracker.overlayRenderer
     .AllOverlaysListForReading)
    {
      if (graphicOverlay.data.renderUI)
      {
        yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicle.VehicleDef, rot,
          graphicOverlay, pattern: pattern);
      }
    }
    if (!extraOverlays.NullOrEmpty())
    {
      foreach (GraphicOverlay graphicOverlay in extraOverlays)
      {
        if (graphicOverlay.data.renderUI)
        {
          yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicle.VehicleDef, rot,
            graphicOverlay, pattern: pattern);
        }
      }
    }
  }

  public static IEnumerable<RenderData> RetrieveAllOverlaySettingsGraphicsProperties(Rect rect,
    VehicleDef vehicleDef, Rot8 rot, PatternData pattern = null,
    List<GraphicOverlay> extraOverlays = null)
  {
    foreach (GraphicOverlay graphicOverlay in vehicleDef.drawProperties.overlays)
    {
      if (graphicOverlay.data.renderUI)
      {
        yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicleDef, rot,
          graphicOverlay, pattern: pattern);
      }
    }
    if (!extraOverlays.NullOrEmpty())
    {
      foreach (GraphicOverlay graphicOverlay in extraOverlays)
      {
        if (graphicOverlay.data.renderUI)
        {
          yield return RetrieveOverlaySettingsGraphicsProperties(rect, vehicleDef, rot,
            graphicOverlay, pattern: pattern);
        }
      }
    }
  }

  public static RenderData RetrieveOverlaySettingsGraphicsProperties(Rect rect,
    VehicleDef vehicleDef, Rot8 rot, GraphicOverlay graphicOverlay, PatternData pattern)
  {
    Rect overlayRect = OverlayRect(rect, vehicleDef, graphicOverlay, rot);
    Graphic graphic = graphicOverlay.Graphic;

    Material material = null;
    Texture2D texture;
    if (graphic is Graphic_Rgb graphicRgb)
    {
      texture = graphicRgb.TexAt(rot);
      material = RGBMaterialPool.Get(graphicOverlay, rot);
      if (graphicRgb.Shader.SupportsRGBMaskTex())
      {
        RGBMaterialPool.SetProperties(graphicOverlay, pattern, graphicRgb.TexAt, graphicRgb.MaskAt);
      }
    }
    else if (graphic.Shader.SupportsMaskTex())
    {
      material = graphic.MatAt(rot);
      texture = material.mainTexture as Texture2D;
    }
    else
    {
      // Drawing UI through VehicleGraphics is deprecated, but in order to not spam errors on non-rgb overlays
      // we need some kind of fallback.
      Log.WarningOnce("Unable to fetch texture for graphic overlay drawing. Using slower fallback.",
        Gen.HashCombineInt(vehicleDef.GetHashCode(), "VehicleGraphics.OverlayProperties".GetHashCode()));
      texture = ContentFinder<Texture2D>.Get(graphicOverlay.data.graphicData.texPath);
    }
    return new RenderData(overlayRect, texture, material,
      graphicOverlay.data.graphicData.DrawOffsetFull(rot).y, graphicOverlay.data.rotation);
  }

  /// <summary>
  /// Retrieve <seealso cref="VehicleTurret"/> GUI data for rendering, adjusted by settings UI properties for <paramref name="vehicleDef"/>
  /// </summary>
  /// <param name="rect"></param>
  /// <param name="vehicleDef"></param>
  /// <param name="turrets"></param>
  /// <param name="patternData"></param>
  /// <param name="rot"></param>
  public static IEnumerable<RenderData> RetrieveAllTurretSettingsGraphicsProperties(Rect rect,
    VehicleDef vehicleDef, Rot8 rot, IEnumerable<VehicleTurret> turrets, PatternData patternData,
    List<string> excludeTurrets = null)
  {
    foreach (VehicleTurret turret in turrets)
    {
      VehicleTurret turretRef = turret.reference ?? turret;
      if (!turret.parentKey.NullOrEmpty())
      {
        //continue; //Attached turrets temporarily disabled from rendering
      }
      if (!turret.NoGraphic)
      {
        yield return RetrieveTurretSettingsGraphicsProperties(rect, vehicleDef, rot, turretRef,
          patternData);
      }
      if (excludeTurrets != null && (excludeTurrets.Contains(turret.key) ||
        excludeTurrets.Contains(turret.parentKey)))
      {
        continue; //Skip if optional parameter contains turret's or parent turret's key.
      }
      if (!turretRef.TurretGraphics.NullOrEmpty())
      {
        foreach (VehicleTurret.TurretDrawData turretDrawData in
          turretRef
           .TurretGraphics) //Use turrets from def to avoid writing over vehicle instance's material
        {
          Rect turretRect = TurretRect(rect, vehicleDef, turretRef, rot);
          Material material = null;
          if (patternData != null && turretDrawData.graphic.Shader.SupportsRGBMaskTex())
          {
            material = RGBMaterialPool.Get(turretDrawData, Rot8.North);
            RGBMaterialPool.SetProperties(turretDrawData, patternData,
              turretDrawData.graphic.TexAt, turretDrawData.graphic.MaskAt);
          }
          else if (turretDrawData.graphic.Shader.SupportsMaskTex())
          {
            material = turretDrawData.graphic.MatAt(Rot8.North);
          }
          yield return new RenderData(turretRect, turretDrawData.graphic.TexAt(Rot8.North),
            material,
            turretDrawData.graphicData.DrawOffsetFull(rot).y + turretRef.DrawLayerOffset,
            turretRef.defaultAngleRotated + rot.AsAngle);
        }
      }
    }
  }

  public static RenderData RetrieveTurretSettingsGraphicsProperties(Rect rect,
    VehicleDef vehicleDef, Rot8 rot, VehicleTurret turret, PatternData patternData)
  {
    Rect turretRect = TurretRect(rect, vehicleDef, turret, rot);
    Material material = null;
    if (patternData != null && turret.Graphic.Shader.SupportsRGBMaskTex())
    {
      material = RGBMaterialPool.Get(turret, Rot8.North);
      RGBMaterialPool.SetProperties(turret, patternData, turret.Graphic.TexAt,
        turret.Graphic.MaskAt);
    }
    else if (turret.Graphic.Shader.SupportsMaskTex())
    {
      material = turret.Graphic.MatAt(Rot8.North);
    }
    return new RenderData(turretRect, turret.Texture, material,
      turret.GraphicData.DrawOffsetFull(rot).y + turret.DrawLayerOffset,
      turret.defaultAngleRotated + rot.AsAngle);
  }

  /// <summary>
  /// Retrieves VehicleTurret Rect adjusted to <paramref name="rect"/> of where it's being rendered.
  /// </summary>
  /// <remarks>Scales up / down relative to drawSize of <paramref name="vehicleDef"/>. Best used inside GUI Group</remarks>
  internal static Rect TurretRect(Rect rect, VehicleDef vehicleDef, VehicleTurret turret,
    Rot8 rot, float iconScale = 1)
  {
    //Ensure CannonGraphics are up to date (only required upon changes to default pattern from mod settings)
    turret.ResolveGraphics(vehicleDef);
    return turret.ScaleUIRectFor(vehicleDef, rect, rot, iconScale: iconScale);
  }

  /// <summary>
  /// Retrieve GraphicOverlay adjusted to <paramref name="rect"/> of where it's being rendered.
  /// </summary>
  internal static Rect OverlayRect(Rect rect, VehicleDef vehicleDef,
    GraphicOverlay graphicOverlay, Rot8 rot, float scale = 1)
  {
    GraphicDataRGB data = graphicOverlay.data.graphicData;
    Vector2 size = vehicleDef.ScaleDrawRatio(data, rot, rect.size, iconScale: scale);
    Vector2 basePos = rect.position + (rect.size - size) * 0.5f;
    Vector3 off = data.DrawOffsetForRot(rot);
    Vector2 original = new(data.drawSize.x, data.drawSize.y);
    Vector2 scaleFactors = new(size.x / original.x, size.y / original.y);
    Vector2 pos = basePos + new Vector2(off.x * scaleFactors.x, -off.z * scaleFactors.y);
    return new Rect(pos, size);
  }

  public static void DrawVehicleFitted(Rect rect, VehicleDef vehicleDef, Rot4 rot,
    Material material)
  {
    Texture2D vehicleIcon = VehicleTex.VehicleTexture(vehicleDef, rot, out float angle);
    Rect texCoords = new Rect(0, 0, 1, 1);
    Vector2 texProportions = vehicleDef.graphicData.drawSize;
    if (rot.IsHorizontal)
    {
      float x = texProportions.x;
      texProportions.x = texProportions.y;
      texProportions.y = x;
    }
    Widgets.DrawTextureFitted(rect, vehicleIcon, GenUI.IconDrawScale(vehicleDef), texProportions,
      texCoords, angle, material);
  }

  public static void DrawVehicleFitted(Rect rect, float angle, Texture2D texture,
    Material material)
  {
    Widgets.DrawTextureFitted(rect, texture, 1, new Vector2(texture.width, texture.height),
      new Rect(0f, 0f, 1f, 1f), angle, material);
  }

  private enum GUILayer
  {
    Lower,
    Upper
  }

  public readonly struct RenderData : IComparable<RenderData>
  {
    public readonly Rect rect;
    public readonly Texture mainTex;
    public readonly Material material;
    public readonly float layer;
    public readonly float angle;

    public RenderData(Rect rect, Texture mainTex, Material material, float layer, float angle)
    {
      this.rect = rect;
      this.mainTex = mainTex;
      this.material = material;
      this.layer = layer;
      this.angle = angle;
    }

    public static RenderData Invalid => new RenderData(Rect.zero, null, null, -1, 0);

    readonly int IComparable<RenderData>.CompareTo(RenderData other)
    {
      if (layer < other.layer) return -1;
      if (layer > other.layer) return 1;
      return 0;
    }
  }

  private class OverlayGUIRenderer
  {
    private readonly List<RenderData> renderDataLower = [];
    private readonly List<RenderData> renderDataUpper = [];

    public void Add(RenderData renderData)
    {
      if (renderData.layer < 0)
      {
        renderDataLower.Add(renderData);
      }
      else
      {
        renderDataUpper.Add(renderData);
      }
    }

    public void Clear()
    {
      renderDataLower.Clear();
      renderDataUpper.Clear();
    }

    public void FinalizeForRendering()
    {
      renderDataLower.Sort();
      renderDataUpper.Sort();
    }

    public void RenderLayer(GUILayer layer)
    {
      switch (layer)
      {
        case GUILayer.Lower:
        {
          foreach (RenderData renderData in renderDataLower)
          {
            UIElements.DrawTextureWithMaterialOnGUI(renderData.rect, renderData.mainTex,
              renderData.material, renderData.angle);
          }
        }
        break;
        case GUILayer.Upper:
        {
          foreach (RenderData renderData in renderDataUpper)
          {
            UIElements.DrawTextureWithMaterialOnGUI(renderData.rect, renderData.mainTex,
              renderData.material, renderData.angle);
          }
        }
        break;
        default:
          throw new NotImplementedException(nameof(GUILayer));
      }
    }
  }
}