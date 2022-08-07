using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class GraphicDataRGB : GraphicDataLayered
	{
		public Color colorThree = Color.white;

		public float tiles = 1;
		public Vector2 displacement = Vector2.zero;

		public PatternDef pattern;

		private Graphic_RGB cachedRGBGraphic;

		public GraphicDataRGB()
		{
		}

		public new Graphic_RGB Graphic
		{
			get
			{
				if (cachedRGBGraphic is null)
				{
					Init();
				}
				return cachedRGBGraphic;
			}
		}

		public virtual void CopyDrawData(GraphicDataRGB graphicData)
		{
			color = graphicData.color;
			colorTwo = graphicData.colorTwo;
			colorThree = graphicData.colorThree;

			tiles = graphicData.tiles;
			displacement = graphicData.displacement;

			pattern = graphicData.pattern ?? PatternDefOf.Default;
		}

		public override void CopyFrom(GraphicDataLayered graphicData)
		{
			base.CopyFrom(graphicData);
			if (graphicData is GraphicDataRGB graphicDataRGB)
			{
				colorThree = graphicDataRGB.colorThree;
				pattern = graphicDataRGB.pattern ?? PatternDefOf.Default;
			}
		}

		public virtual void CopyFrom(GraphicDataLayered graphicData, PatternDef pattern, Color colorThree)
		{
			CopyFrom(graphicData);
			if (graphicData is GraphicDataRGB)
			{
				this.colorThree = colorThree;
				this.pattern = pattern ?? PatternDefOf.Default;
			}
		}

		public virtual void Init()
		{
			if (graphicClass is null)
			{
				cachedRGBGraphic = null;
				return;
			}
			ShaderTypeDef shaderTypeDef = pattern is SkinDef ? RGBShaderTypeDefOf.CutoutComplexSkin : shaderType;
			if (shaderTypeDef == null)
			{
				color = Color.white;
				colorTwo = Color.white;
				colorThree = Color.white;
				shaderTypeDef = ShaderTypeDefOf.Cutout;
			}
			if (!VehicleMod.settings.main.useCustomShaders)
			{
				shaderTypeDef = shaderTypeDef.Shader.SupportsMaskTex() ? ShaderTypeDefOf.CutoutComplex : ShaderTypeDefOf.Cutout;
			}
			Shader shader = shaderTypeDef.Shader;
			cachedRGBGraphic = GraphicDatabaseRGB.Get(graphicClass, texPath, shader, drawSize, color, colorTwo, colorThree, tiles, displacement.x, displacement.y, this, shaderParameters);
			AccessTools.Field(typeof(GraphicData), "cachedGraphic").SetValue(this, cachedRGBGraphic);
		}

		public override string ToString()
		{
			return $"({texPath}, {color}, {colorTwo}, {colorThree}, {pattern}, {tiles}, {displacement})";
		}
	}
}
