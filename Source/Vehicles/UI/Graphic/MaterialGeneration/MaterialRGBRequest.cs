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
		public Color color;
		public Color colorTwo;
		public Color colorThree;
		public Texture2D maskTex;
		public Texture2D patternTex;
		public int renderQueue;
		public List<ShaderParameter> shaderParameters;

		public MaterialRequestRGB(Texture2D tex)
		{
			shader = ShaderDatabase.Cutout;
			mainTex = tex;
			color = Color.white;
			colorTwo = Color.white;
			colorThree = Color.white;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader)
		{
			this.shader = shader;
			mainTex = tex;
			color = Color.white;
			colorTwo = Color.white;
			colorThree = Color.white;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader, Color color)
		{
			this.shader = shader;
			mainTex = tex;
			this.color = color;
			colorTwo = Color.white;
			colorThree = Color.white;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader, Color color, Color colorTwo)
		{
			this.shader = shader;
			mainTex = tex;
			this.color = color;
			this.colorTwo = colorTwo;
			colorThree = Color.white;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
		}

		public MaterialRequestRGB(Texture2D tex, Shader shader, Color color, Color colorTwo, Color colorThree)
		{
			this.shader = shader;
			mainTex = tex;
			this.color = color;
			this.colorTwo = colorTwo;
			this.colorThree = colorThree;
			maskTex = null;
			patternTex = null;
			renderQueue = 0;
			shaderParameters = null;
		}

		public MaterialRequestRGB(MaterialRequest req, Texture2D patternTex, Color colorThree)
		{
			shader = req.shader;
			mainTex = req.mainTex;
			maskTex = req.maskTex;
			color = req.color;
			colorTwo = req.colorTwo;
			this.colorThree = colorThree;
			this.patternTex = patternTex;
			renderQueue = req.renderQueue;
			shaderParameters = req.shaderParameters;
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
			return Gen.HashCombine(Gen.HashCombineInt(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(0, shader), color), colorTwo), colorThree), mainTex), maskTex), renderQueue), shaderParameters);
		}

		public override bool Equals(object obj)
		{
			return obj is MaterialRequestRGB mpr && Equals(mpr);
		}

		public bool Equals(MaterialRequestRGB other)
		{
			return other.shader == shader && other.mainTex == mainTex && other.color == color && other.colorTwo == colorTwo && other.colorThree == colorThree 
				&& other.maskTex == maskTex && other.patternTex == patternTex && other.renderQueue == renderQueue && other.shaderParameters == shaderParameters;
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
			return $"MaterialPatternedRequest({shader.name}, {mainTex.name}, {color}, {colorTwo}, {colorThree}, {maskTex}, {patternTex}, {renderQueue})";
		}
	}
}
