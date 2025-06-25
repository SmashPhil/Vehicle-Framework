using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
  public static class GraphicDatabaseRGB
  {
    private static readonly Dictionary<IMaterialCacheTarget, Graphic_Rgb> allGraphics = [];

    public static Graphic_Rgb Get(IMaterialCacheTarget target, Type graphicClass, string path,
      Shader shader,
      Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles = 1,
      float displacementX = 0,
      float displacementY = 0, GraphicDataRGB data = null,
      List<ShaderParameter> shaderParameters = null)
    {
      GraphicRequestRGB graphicRequest = new GraphicRequestRGB(target, graphicClass, path, shader,
        drawSize,
        color, colorTwo, colorThree, tiles, new Vector2(displacementX, displacementY), data, 0,
        shaderParameters);
      try
      {
        if (graphicRequest.graphicClass == typeof(Graphic_Vehicle))
        {
          return GetInner<Graphic_Vehicle>(graphicRequest);
        }
        if (graphicRequest.graphicClass == typeof(Graphic_Turret))
        {
          return GetInner<Graphic_Turret>(graphicRequest);
        }
        return (Graphic_Rgb)GenGeneric.InvokeStaticGenericMethod(typeof(GraphicDatabaseRGB),
          graphicRequest.graphicClass, "GetInner", [graphicRequest]);
      }
      catch (Exception ex)
      {
        Log.Error($"Exception getting {graphicClass} at {path}. Exception=\"{ex}\"");
      }
      return null;
    }

    private static T GetInner<T>(GraphicRequestRGB req) where T : Graphic_Rgb, new()
    {
      if (!allGraphics.TryGetValue(req.target, out Graphic_Rgb graphic))
      {
        graphic = Activator.CreateInstance<T>();
        graphic.Init(req);
        allGraphics.Add(req.target, graphic);
      }
      return (T)graphic;
    }

    public static bool Remove(IMaterialCacheTarget target)
    {
      return allGraphics.Remove(target);
    }

    public static void Clear()
    {
      allGraphics.Clear();
    }
  }
}