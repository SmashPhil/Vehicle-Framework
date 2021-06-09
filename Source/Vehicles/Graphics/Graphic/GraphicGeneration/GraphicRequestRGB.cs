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

		public GraphicRequestRGB(Type graphicClass, string path, Shader shader, Vector2 drawSize, Color color, Color colorTwo, Color colorThree, float tiles, Vector2 displacement, GraphicDataRGB graphicData, int renderQueue, List<ShaderParameter> shaderParameters)
		{
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

		public GraphicRequestRGB(GraphicRequest req)
		{
			graphicClass = req.graphicClass;
			path = req.path;
			shader = req.shader;
			drawSize = req.drawSize;
			color = req.color;
			colorTwo = req.colorTwo;
			colorThree = Color.blue;
			tiles = 1;
			displacement = Vector2.zero;
			graphicData = req.graphicData as GraphicDataRGB;
			renderQueue = req.renderQueue;
			shaderParameters = req.shaderParameters;
		}

		public string Summary => $"Type: {graphicClass}\nPath: {path}\nShader: {shader}\nDrawSize: {drawSize}\nColors: {color}|{colorTwo}|{colorThree}\nGraphicData: {graphicData}\nRenderQueue: {renderQueue}\nParams Count: {shaderParameters?.Count.ToString() ?? "Null"}";

		public override int GetHashCode()
		{
			if (path == null)
			{
				path = BaseContent.BadTexPath;
			}
			return Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombineStruct(
				Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(
					Gen.HashCombine(0, displacement), tiles), graphicClass), path), shader), drawSize), color), colorTwo), colorThree), graphicData), renderQueue), shaderParameters);
		}

		public override bool Equals(object obj)
		{
			return obj is GraphicRequestRGB rgb && Equals(rgb);
		}

		public bool Equals(GraphicRequestRGB other)
		{
			return graphicClass == other.graphicClass && path == other.path && shader == other.shader && drawSize == other.drawSize && 
				color == other.color && colorTwo == other.colorTwo && colorThree == other.colorThree && tiles == other.tiles && displacement == other.displacement &&
				graphicData == other.graphicData && renderQueue == other.renderQueue && shaderParameters == other.shaderParameters;
		}

		public static bool operator ==(GraphicRequestRGB lhs, GraphicRequestRGB rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(GraphicRequestRGB lhs, GraphicRequestRGB rhs)
		{
			return !(lhs == rhs);
		}
	}
}
