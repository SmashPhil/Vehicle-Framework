using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public struct MaterialRequestRGB
	{
		public Shader shader;
		public Texture2D mainTex;
		public PatternProperties properties;
		public Texture2D maskTex;
		public Texture2D patternTex;
		public int renderQueue;
		public bool isSkin;
		public List<ShaderParameter> shaderParameters;

		public Color color;
		public Color colorTwo;
		public Color colorThree;
		public float tiles;
		public Vector2 displacement;

		public MaterialRequestRGB(Texture2D tex)
		{
			shader = ShaderDatabase.Cutout;
			mainTex = tex;
			properties = new PatternProperties();
			color = Color.white;
			colorTwo = Color.white;
			colorThree = Color.white;
			tiles = 1;
			displacement = Vector2.zero;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
			isSkin = false;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader)
		{
			this.shader = shader;
			mainTex = tex;
			maskTex = null;
			properties = new PatternProperties();
			color = Color.white;
			colorTwo = Color.white;
			colorThree = Color.white;
			tiles = 1;
			displacement = Vector2.zero;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
			isSkin = false;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader, PatternProperties properties)
		{
			this.shader = shader;
			mainTex = tex;
			maskTex = null;
			this.properties = properties;
			color = properties.colorOne ?? Color.white;
			colorTwo = properties.colorTwo ?? Color.white;
			colorThree = properties.colorThree ?? Color.white;
			tiles = properties.tiles.TryGetValue("All", 1);
			displacement = Vector2.zero;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
			isSkin = false;
		}

		public MaterialRequestRGB(MaterialRequest req, Texture2D patternTex, PatternProperties properties)
		{
			shader = req.shader;
			mainTex = req.mainTex as Texture2D;
			maskTex = req.maskTex;
			this.properties = properties;
			color = properties.colorOne ?? Color.white;
			colorTwo = properties.colorTwo ?? Color.white;
			colorThree = properties.colorThree ?? Color.white;
			tiles = properties.tiles.TryGetValue("All", 1);
			displacement = Vector2.zero;
			this.patternTex = patternTex;
			renderQueue = req.renderQueue;
			shaderParameters = req.shaderParameters;
			isSkin = false;
		}

		public MaterialRequestRGB(MaterialRequest req, Texture2D patternTex, PatternProperties properties, bool isSkin)
		{
			shader = req.shader;
			mainTex = req.mainTex as Texture2D;
			maskTex = req.maskTex;
			this.properties = properties;
			color = properties.colorOne ?? Color.white;
			colorTwo = properties.colorTwo ?? Color.white;
			colorThree = properties.colorThree ?? Color.white;
			tiles = properties.tiles.TryGetValue("All", 1);
			displacement = Vector2.zero;
			this.patternTex = patternTex;
			renderQueue = req.renderQueue;
			shaderParameters = req.shaderParameters;
			this.isSkin = isSkin;
		}

		public string BaseTexPath
		{
			set
			{
				mainTex = ContentFinder<Texture2D>.Get(value, true);
			}
		}

		public override int GetHashCode()
		{
			return Gen.HashCombine(Gen.HashCombineInt(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(
				Gen.HashCombine(Gen.HashCombine(0, displacement), tiles), isSkin), color), colorTwo), colorThree), mainTex), maskTex), renderQueue), shaderParameters);
		}

		public override bool Equals(object obj)
		{
			return obj is MaterialRequestRGB mpr && Equals(mpr);
		}

		public bool Equals(MaterialRequestRGB other)
		{
			return other.shader == shader && other.mainTex == mainTex && other.properties.colorOne == properties.colorOne && 
				other.properties.colorTwo == properties.colorTwo && other.properties.colorThree == properties.colorThree 
				&& other.maskTex == maskTex && other.patternTex == patternTex && other.renderQueue == renderQueue && 
				other.shaderParameters == shaderParameters && other.isSkin == isSkin && other.tiles == tiles && other.displacement == displacement;
		}

		public static bool operator ==(MaterialRequestRGB lhs, MaterialRequestRGB rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(MaterialRequestRGB lhs, MaterialRequestRGB rhs)
		{
			return !(lhs == rhs);
		}

		public override string ToString()
		{
			return $"MaterialRGBRequest({shader.name}, {mainTex.name}, {properties}, {maskTex}, {patternTex}, {renderQueue}, {isSkin})";
		}
	}
}
