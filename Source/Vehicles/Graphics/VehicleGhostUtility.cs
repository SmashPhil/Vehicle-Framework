using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

public static class VehicleGhostUtility
{
  public static readonly Color whiteGhostColor = new(1, 1, 1, 0.5f);

  private static readonly Dictionary<int, Graphic> cachedGhostGraphics = [];

  public static void DrawGhostVehicleDef(IntVec3 center, Rot8 rot, VehicleDef vehicleDef,
    Color ghostCol, AltitudeLayer drawAltitude, VehiclePawn vehicle = null)
  {
    Graphic baseGraphic = vehicleDef.graphic;
    Graphic graphic = GhostUtility.GhostGraphicFor(baseGraphic, vehicleDef, ghostCol);
    Vector3 loc = GenThing.TrueCenter(center, rot, vehicleDef.Size, drawAltitude.AltitudeFor());

    Rot8 baseRot = rot;
    float baseAngle = rot.AsRotationAngle;
    //If diagonal rotated from North / South, vanilla draw needs adjustment in order to use the right graphics
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
    graphic.DrawFromDef(loc, baseRot, vehicleDef, baseAngle);

    DrawGhostOverlays(center, rot, vehicleDef, baseGraphic, ghostCol, drawAltitude,
      thing: vehicle, baseRot: baseRot, baseAngle: baseAngle);
  }

  // Public method signatures have been changed. Please add a stub if necessary
  // Vehicle Map Framework patches will be affected, so I'll make it that absorbs the changes if the PR is accepted
  // public static void DrawGhostOverlays(IntVec3 center, Rot8 rot, VehicleDef vehicleDef,
  //   Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
  // {
  //   _ = baseGraphic;
  //   DrawGhostOverlays(center, rot, vehicleDef, ghostCol, drawAltitude, thing);
  // }
  
  public static void DrawGhostOverlays(IntVec3 center, Rot8 rot, VehicleDef vehicleDef,
    Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null, Rot8? baseRot = null, float baseAngle = 0f)
  {
    Rot4 drawRot = baseRot ?? rot;
    Vector3 loc = GenThing.TrueCenter(center, drawRot, vehicleDef.Size, drawAltitude.AltitudeFor());
    if (baseAngle != 0f)
    {
      Vector3 offset = baseGraphic.DrawOffset(drawRot).RotatedBy(baseAngle);
      if ((Rot4)rot == Rot4.East)
      {
        offset *= -1f;
      }
      loc += offset;
    }
    foreach ((Graphic graphic, float rotation) in vehicleDef.GhostGraphicOverlaysFor(ghostCol))
    {
      float extraRotation = baseAngle + rotation;
      graphic.DrawWorker(loc, drawRot, vehicleDef, thing, extraRotation);
    }
    if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is not null)
    {
      vehicleDef.DrawGhostTurretTextures(loc, drawRot, ghostCol);
    }
  }

  private static Graphic_Turret GhostGraphicFor(this VehicleDef vehicleDef, VehicleTurret turret,
    Color ghostColor)
  {
    int num = 0;
    num = Gen.HashCombine(num, vehicleDef);
    num = Gen.HashCombine(num, turret);
    num = Gen.HashCombineStruct(num, ghostColor);
    if (!cachedGhostGraphics.TryGetValue(num, out Graphic graphic))
    {
      turret.ResolveGraphics(vehicleDef, true);
      graphic = turret.Graphic;

      GraphicData graphicData = new GraphicData();
      graphicData.CopyFrom(graphic.data);
      graphicData.drawOffsetWest =
        graphic.data.drawOffsetWest; //TEMPORARY - Bug in vanilla copies South over to West
      graphicData.shadowData = null;
      graphicData.shaderType = ShaderTypeDefOf.EdgeDetect;
      _ = graphicData.Graphic;

      graphic = (Graphic_Turret)GraphicDatabase.Get(graphic.GetType(), graphic.path,
        ShaderTypeDefOf.EdgeDetect.Shader, graphic.drawSize, ghostColor, Color.white, graphicData,
        null);

      cachedGhostGraphics.Add(num, graphic);
    }
    return (Graphic_Turret)graphic;
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
        GraphicData graphicData = new GraphicData();
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

  private static void DrawGhostTurretTextures(this VehicleDef vehicleDef, Vector3 loc, Rot8 rot,
    Color ghostColor)
  {
    if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is { } props)
    {
      foreach (VehicleTurret turret in props.turrets)
      {
        if (turret.NoGraphic)
        {
          continue;
        }
        if (!turret.parentKey.NullOrEmpty())
        {
          continue;
        }

        turret.ResolveGraphics(vehicleDef);

        try
        {
          float locationRotation = turret.defaultAngleRotated + rot.AsAngle;
          if (turret.attachedTo != null)
          {
            locationRotation += turret.attachedTo.defaultAngleRotated; // + rot.AsAngle;
          }
          Vector3 turretDrawLoc = turret.DrawPosition(rot);
          Vector3 turretLoc = loc + turretDrawLoc;

          if (!turret.NoGraphic)
          {
            Graphic graphic = vehicleDef.GhostGraphicFor(turret, ghostColor);
            Mesh cannonMesh = graphic.MeshAt(rot);
            Graphics.DrawMesh(cannonMesh, turretLoc, locationRotation.ToQuat(),
              graphic.MatAt(rot), 0);
          }
          //DrawTurretGhostOverlays(vehicleDef, turret, ghostColor, turretLoc, rot, locationRotation);
        }
        catch (Exception ex)
        {
          Log.Error(
            $"Failed to render Cannon=\"{turret.def.defName}\" for VehicleDef=\"{vehicleDef.defName}\", Exception: {ex}");
        }
      }
    }
  }

  private static void DrawTurretGhostOverlays(VehicleDef vehicleDef, VehicleTurret turret,
    Color ghostColor, Vector3 drawPos, Rot8 rot, float extraRotation)
  {
    if (!turret.TurretGraphics.NullOrEmpty())
    {
      for (int i = 0; i < turret.TurretGraphics.Count; i++)
      {
        Graphic graphic = vehicleDef.GhostGraphicFor(turret, ghostColor);
        VehicleTurret.TurretDrawData turretDrawData = turret.TurretGraphics[i];
        Vector3 rootPos = drawPos + turretDrawData.DrawOffset(rot, 0, extraRotation);
        Mesh cannonMesh = graphic.MeshAt(rot);
        Graphics.DrawMesh(cannonMesh, rootPos, extraRotation.ToQuat(), graphic.MatAt(rot), 0);
      }
    }
  }
}