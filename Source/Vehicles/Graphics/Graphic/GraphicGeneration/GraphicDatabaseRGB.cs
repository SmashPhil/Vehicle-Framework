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
		private static readonly Dictionary<IMaterialCacheTarget, Graphic_RGB> allGraphics = new Dictionary<IMaterialCacheTarget, Graphic_RGB>();

		public static Graphic_RGB Get(IMaterialCacheTarget target, Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles = 1, float displacementX = 0, float displacementY = 0, GraphicDataRGB data = null, List<ShaderParameter> shaderParameters = null)
		{
			GraphicRequestRGB graphicRequest = new GraphicRequestRGB(target, graphicClass, path, shader, drawSize, color, colorTwo, colorThree, tiles, new Vector2(displacementX, displacementY), data, 0, shaderParameters);
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
				return (Graphic_RGB)GenGeneric.InvokeStaticGenericMethod(typeof(GraphicDatabaseRGB), graphicRequest.graphicClass, "GetInner", new object[]
				{
					graphicRequest
				});
			}
			catch (Exception ex)
			{
				Log.Error($"Exception getting {graphicClass} at {path}. Exception=\"{ex}\"");
			}
			return null;
		}

		private static T GetInner<T>(GraphicRequestRGB req) where T : Graphic_RGB, new()
		{
			if (!allGraphics.TryGetValue(req.target, out Graphic_RGB graphic))
			{
				graphic = Activator.CreateInstance<T>();
				graphic.Init(req);
				allGraphics.Add(req.target, graphic);
			}
			return (T)graphic;
		}

		public static void Clear()
		{
			allGraphics.Clear();
		}
	}
}
