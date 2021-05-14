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
		private static readonly Dictionary<GraphicRequestRGB, Graphic_RGB> allGraphics = new Dictionary<GraphicRequestRGB, Graphic_RGB>();

		public static Graphic_RGB Get<T>(string path) where T : Graphic_RGB, new()
		{
			return GetInner<T>(new GraphicRequestRGB(typeof(T), path, ShaderDatabase.Cutout, Vector2.one, Color.white, Color.white, Color.white, 1, null, 0, null));
		}

		public static Graphic_RGB Get<T>(string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles = 1, GraphicDataRGB data = null, List<ShaderParameter> shaderParameters = null)
		{
			return Get(typeof(T), path, shader, drawSize, color, colorTwo, colorThree, tiles, data, shaderParameters);
		}

		public static Graphic_RGB Get(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles = 1, GraphicDataRGB data = null, List<ShaderParameter> shaderParameters = null)
		{
			GraphicRequestRGB graphicRequest = new GraphicRequestRGB(graphicClass, path, shader, drawSize, color, colorTwo, colorThree, tiles, data, 0, shaderParameters);
			if (graphicRequest.graphicClass == typeof(Graphic_Vehicle))
			{
				return GetInner<Graphic_Vehicle>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_Cannon))
			{
				return GetInner<Graphic_Cannon>(graphicRequest);
			}
			if (graphicRequest.graphicClass == typeof(Graphic_CannonAnimate))
			{
				return GetInner<Graphic_CannonAnimate>(graphicRequest);
			}
			try
			{
				return (Graphic_RGB)GenGeneric.InvokeStaticGenericMethod(typeof(GraphicDatabaseRGB), graphicRequest.graphicClass, "GetInner", new object[]
				{
					graphicRequest
				});
			}
			catch (Exception ex)
			{
				Log.Error(string.Concat(new object[]
				{
					"Exception getting ",
					graphicClass,
					" at ",
					path,
					": ",
					ex.ToString()
				}));
			}
			return null;
		}

		private static T GetInner<T>(GraphicRequestRGB req) where T : Graphic_RGB, new()
		{
			if (!allGraphics.TryGetValue(req, out Graphic_RGB graphic))
			{
				graphic = Activator.CreateInstance<T>();
				graphic.Init(req);
				allGraphics.Add(req, graphic);
			}
			return (T)graphic;
		}

		public static void Clear()
		{
			allGraphics.Clear();
		}
	}
}
