using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public static class VehicleGhostUtility
{
  // TODO 1.7 - Rename to proper name convention
  // ReSharper disable once InconsistentNaming
  public static readonly Color whiteGhostColor = new(1, 1, 1, 0.5f);
  public static readonly Color RedGhostColor = new(1, 0, 0, 0.5f);

  private static readonly Dictionary<int, Graphic> cachedGhostGraphics = [];

  [Obsolete("Use DrawAt instead.")]
  public static void DrawGhostVehicle([NotNull] VehiclePawn vehicle, IntVec3 center, Rot8 rot,
    Color ghostCol, AltitudeLayer drawAltitude)
  {
    DrawData data = new(vehicle)
    {
      center = center,
      rot = rot,
      altitude = drawAltitude,
      ghostColor = ghostCol
    };
    DrawAt(in data);
  }

  [Obsolete("Use DrawAt instead.")]
  public static void DrawGhostVehicleDef(IntVec3 center, Rot8 rot, VehicleDef vehicleDef,
    Color ghostCol, AltitudeLayer drawAltitude, VehiclePawn vehicle = null)
  {
    if (vehicle != null)
    {
      // TODO 1.7 - Remove optional param when refactoring methods in this class
      DrawGhostVehicle(vehicle, center, rot, ghostCol, drawAltitude);
      return;
    }
    DrawData data = new(vehicleDef)
    {
      center = center,
      rot = rot,
      altitude = drawAltitude,
      ghostColor = ghostCol
    };
    DrawAt(in data);
  }

  public static void DrawAt(ref readonly DrawData drawData)
  {
    Graphic baseGraphic = drawData.vehicleDef.graphic;
    Graphic graphic = GhostUtility.GhostGraphicFor(baseGraphic, drawData.vehicleDef, drawData.ghostColor);
    Rot8 baseRot = drawData.rot;
    float baseAngle = drawData.rot.AsRotationAngle;
    // If diagonal rotated from North / South, vanilla draw needs adjustment in order to use the right graphics
    if (baseRot.IsDiagonal)
    {
      switch (baseRot.AsInt)
      {
        case 4:
          baseRot = Rot8.North;
          baseAngle = 45;
          break;
        case 5:
          baseRot = Rot8.South;
          baseAngle = -45;
          break;
        case 6:
          baseRot = Rot8.South;
          baseAngle = 45;
          break;
        case 7:
          baseRot = Rot8.North;
          baseAngle = -45;
          break;
      }
    }
    graphic.DrawFromDef(drawData.DrawPos, baseRot, drawData.vehicleDef, baseAngle);
    DrawGhostOverlays(in drawData, baseGraphic, baseAngle, baseRot);
  }

  public static void DrawGhostOverlays(ref readonly DrawData data, Graphic baseGraphic, float angle, Rot8? baseRot = null)
  {
    VehicleDef vehicleDef = data.vehicleDef;
    Rot4 drawRot = baseRot ?? data.rot;
    Vector3 drawPos = data.DrawPos;
    if (!Mathf.Approximately(angle, 0))
    {
      Vector3 offset = baseGraphic.DrawOffset(drawRot).RotatedBy(angle);
      if ((Rot4)data.rot == Rot4.East)
      {
        offset *= -1f;
      }
      drawPos += offset;
    }
    foreach ((Graphic graphic, float rotation) in vehicleDef.GhostGraphicOverlaysFor(data.ghostColor))
    {
      float extraRotation = angle + rotation;
      graphic.DrawWorker(drawPos, drawRot, vehicleDef, data.vehicle, extraRotation);
    }
    DrawGhostTurretTextures(data);
  }

  // TODO 1.7 - Compact method signature with struct param
  [Obsolete("Use overload with DrawData parameter.")]
  public static void DrawGhostOverlays(IntVec3 center, Rot8 rot, VehicleDef vehicleDef,
    Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null, Rot8? baseRot = null, float baseAngle = 0f)
  {
    DrawData data = new(vehicleDef)
    {
      center = center,
      rot = rot,
      ghostColor = ghostCol,
      altitude = drawAltitude
    };
    DrawGhostOverlays(in data, baseGraphic, baseAngle, baseRot);
  }

  private static IEnumerable<(Graphic graphic, float rotation)> GhostGraphicOverlaysFor(
    this VehicleDef vehicleDef, Color ghostColor)
  {
    int num = 0;
    num = Gen.HashCombine(num, vehicleDef);
    num = Gen.HashCombineStruct(num, ghostColor);
    foreach (GraphicOverlay graphicOverlay in vehicleDef.drawProperties.overlays)
    {
      int hash = Gen.HashCombine(num, graphicOverlay.data.graphicData);
      if (!cachedGhostGraphics.TryGetValue(hash, out Graphic graphic))
      {
        graphic = graphicOverlay.Graphic;
        GraphicData graphicData = new();
        graphicData.CopyFrom(graphic.data);
        graphicData.drawOffsetWest =
          graphic.data.drawOffsetWest; //TEMPORARY - Bug in vanilla copies South over to West
        graphicData.shadowData = null;
        graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
        _ = graphicData.Graphic;

        graphic = GraphicDatabase.Get(graphic.GetType(), graphic.path,
          ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white,
          graphicData, null);

        cachedGhostGraphics.Add(hash, graphic);
      }
      yield return (graphic, graphicOverlay.data.rotation);
    }
  }

  private static void DrawGhostTurretTextures(in DrawData data)
  {
    VehicleDef vehicleDef = data.vehicleDef;
    if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is not { } props)
      return;

    foreach (VehicleTurret turret in props.turrets)
    {
      if (turret.NoGraphic)
        continue;

      if (!turret.parentKey.NullOrEmpty())
        continue;

      turret.ResolveGraphics(vehicleDef);
      try
      {
        Rot8 rot = data.rot;
        float extraRotation = turret.defaultAngleRotated;
        VehicleTurret parent = turret.attachedTo;
        while (parent != null)
        {
          extraRotation += turret.attachedTo.defaultAngleRotated;
          parent = parent.attachedTo;
        }
        Vector3 turretDrawLoc = turret.DrawPosition(rot);
        Vector3 turretLoc = data.DrawPos + turretDrawLoc;
        Graphic graphic = GhostGraphicFor(turret, data);
        Mesh cannonMesh = graphic.MeshAt(rot);
        Graphics.DrawMesh(cannonMesh, turretLoc, (extraRotation + rot.AsAngle).ToQuat(), graphic.MatAt(rot), layer: 0);
        if (!turret.TurretGraphics.NullOrEmpty())
        {
          DrawTurretGhostOverlays(turret, data, turretLoc, extraRotation);
        }
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Failed to render Cannon=\"{turret.def.defName}\" for VehicleDef=\"{vehicleDef.defName}\".\nException={ex}");
      }
    }
  }

  private static void DrawTurretGhostOverlays(VehicleTurret turret, in DrawData data,
    Vector3 turretLoc, float extraRotation)
  {
    foreach (VehicleTurret.TurretDrawData turretDrawData in turret.TurretGraphics)
    {
      Graphic graphic = GhostGraphicFor(turretDrawData, data);
      Vector3 rootPos = data.DrawPos + turretDrawData.DrawOffset(data.rot, parentRotation: 0, extraRotation);
      Mesh cannonMesh = graphic.MeshAt(data.rot);
      Graphics.DrawMesh(cannonMesh, rootPos, (extraRotation + data.rot.AsAngle).ToQuat(), graphic.MatAt(data.rot),
        layer: 0);
    }
  }

  private static Graphic_Turret GhostGraphicFor(VehicleTurret turret, in DrawData data)
  {
    int hash = 0;
    hash = Gen.HashCombine(hash, data.vehicleDef);
    hash = Gen.HashCombine(hash, turret);
    hash = Gen.HashCombineStruct(hash, data.ghostColor);
    if (!cachedGhostGraphics.TryGetValue(hash, out Graphic graphic))
    {
      turret.ResolveGraphics(data.vehicleDef, true);
      graphic = turret.Graphic;

      GraphicData graphicData = new();
      graphicData.CopyFrom(graphic.data);
      // There's a bug in RimWorld where South copies over to West
      graphicData.drawOffsetWest = graphic.data.drawOffsetWest;
      graphicData.shadowData = null;
      graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
      _ = graphicData.Graphic;

      graphic = (Graphic_Turret)GraphicDatabase.Get(graphic.GetType(), graphic.path,
        ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, data.ghostColor, Color.white, graphicData,
        null);

      cachedGhostGraphics.Add(hash, graphic);
    }
    return (Graphic_Turret)graphic;
  }

  private static Graphic_Turret GhostGraphicFor(VehicleTurret.TurretDrawData turretDrawData, in DrawData data)
  {
    int hash = 0;
    hash = Gen.HashCombine(hash, data.vehicleDef);
    hash = Gen.HashCombine(hash, turretDrawData);
    hash = Gen.HashCombineStruct(hash, data.ghostColor);
    if (!cachedGhostGraphics.TryGetValue(hash, out Graphic graphic))
    {
      graphic = turretDrawData.graphic;

      GraphicData graphicData = new();
      graphicData.CopyFrom(graphic.data);
      // There's a bug in RimWorld where South copies over to West
      graphicData.drawOffsetWest = graphic.data.drawOffsetWest;
      graphicData.shadowData = null;
      graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
      _ = graphicData.Graphic;

      graphic = (Graphic_Turret)GraphicDatabase.Get(graphic.GetType(), graphic.path,
        ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, data.ghostColor, Color.white, graphicData,
        null);

      cachedGhostGraphics.Add(hash, graphic);
    }
    return (Graphic_Turret)graphic;
  }

  [PublicAPI]
  public struct DrawData
  {
    public required IntVec3 center;
    public Rot8 rot = Rot8.North;
    public Color ghostColor = whiteGhostColor;
    public AltitudeLayer altitude;

    public readonly VehiclePawn vehicle;
    public readonly VehicleDef vehicleDef;

    public Vector3? drawPosOverride;

    public DrawData(VehiclePawn vehicle)
    {
      this.vehicle = vehicle;
      this.vehicleDef = vehicle.VehicleDef;
    }

    public DrawData(VehicleDef vehicleDef)
    {
      this.vehicleDef = vehicleDef;
    }

    public Vector3 DrawPos
    {
      get
      {
        if (drawPosOverride != null)
          return drawPosOverride.Value;

        return vehicle != null ?
          Ext_Vehicles.TrueCenter(center, rot, vehicleDef.Size, altitude.AltitudeFor()) :
               GenThing.TrueCenter(center, rot, vehicleDef.Size, altitude.AltitudeFor());
      }
    }
  }
}
