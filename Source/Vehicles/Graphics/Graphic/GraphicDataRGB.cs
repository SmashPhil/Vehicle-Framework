using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace Vehicles
{
	public class GraphicDataRGB : GraphicData
	{
		public float tiles = 1;
		public Vector2 displacement = Vector2.zero;
		public Color colorThree = Color.white;
		public PatternDef pattern;

		private Graphic_RGB cachedRGBGraphic;

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

		public virtual void CopyFrom(GraphicDataRGB graphicData)
		{
			base.CopyFrom(graphicData);
			colorThree = graphicData.colorThree;
			pattern = graphicData.pattern ?? PatternDefOf.Default;
		}

		public virtual void CopyFrom(GraphicData graphicData, PatternDef pattern, Color colorThree)
		{
			CopyFrom(graphicData);
			this.colorThree = colorThree;
			this.pattern = pattern ?? PatternDefOf.Default;
		}
		
		public virtual void Init()
		{
			if (graphicClass is null)
			{
				cachedRGBGraphic = null;
				return;
			}
			ShaderTypeDef cutout = shaderType;
			if (cutout == null)
			{
				cutout = RGBShaderTypeDefOf.CutoutComplexRGB;
			}
			Shader shader = cutout.Shader;
			cachedRGBGraphic = GraphicDatabaseRGB.Get(graphicClass, texPath, shader, drawSize, color, colorTwo, colorThree, tiles, displacement.x, displacement.y, this, shaderParameters);
			AccessTools.Field(typeof(GraphicData), "cachedGraphic").SetValue(this, cachedRGBGraphic);
		}
	}
}
