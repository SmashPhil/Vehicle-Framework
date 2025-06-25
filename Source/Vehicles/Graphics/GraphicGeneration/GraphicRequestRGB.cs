using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public struct GraphicRequestRGB : IEquatable<GraphicRequestRGB>
	{
		public IMaterialCacheTarget target;
		public Type graphicClass;
		public string path;
		public Shader shader;
		public Vector2 drawSize;
		public Color color;
		public Color colorTwo;
		public Color colorThree;
		public float tiles;
		public Vector2 displacement;
		public GraphicDataRGB graphicData;
		public int renderQueue;
		public List<ShaderParameter> shaderParameters;

		public GraphicRequestRGB(IMaterialCacheTarget target, Type graphicClass, string path, Shader shader, 
			Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles, Vector2 displacement, 
			GraphicDataRGB graphicData, int renderQueue, List<ShaderParameter> shaderParameters)
		{
			this.target = target;
			this.graphicClass = graphicClass;
			this.path = path;
			this.shader = shader;
			this.drawSize = drawSize;
			this.color = color;
			this.colorTwo = colorTwo;
			this.colorThree = colorThree;
			this.tiles = tiles;
			this.displacement = displacement;
			this.graphicData = graphicData;
			this.renderQueue = renderQueue;
			this.shaderParameters = (shaderParameters.NullOrEmpty() ? null : shaderParameters);
		}

		public string Summary => $"Target: {target}\nType: {graphicClass}\nPath: {path}\nShader: {shader}\n" +
			$"DrawSize: {drawSize}\nColors: {color}|{colorTwo}|{colorThree}\nGraphicData: {graphicData}\n" +
			$"RenderQueue: {renderQueue}\nParams Count: {shaderParameters?.Count.ToString() ?? "Null"}";

		public override int GetHashCode()
		{
			if (path == null)
			{
				path = BaseContent.BadTexPath;
			}
			return target.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return obj is GraphicRequestRGB rgb && Equals(rgb);
		}

		public bool Equals(GraphicRequestRGB other)
		{
			return other.target == target;
		}

		public static bool operator ==(GraphicRequestRGB lhs, GraphicRequestRGB rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(GraphicRequestRGB lhs, GraphicRequestRGB rhs)
		{
			return !(lhs == rhs);
		}

		public static implicit operator GraphicRequest(GraphicRequestRGB req)
		{
			return new GraphicRequest(req.graphicClass, req.path, req.shader, req.drawSize, req.color, req.colorTwo, 
				req.graphicData, req.renderQueue, req.shaderParameters, null);
		}
	}
}
