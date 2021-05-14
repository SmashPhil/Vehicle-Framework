using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Vehicles.Defs;

namespace Vehicles
{
	public class GraphicDataRGB : GraphicData
	{
		public float tiles = 1;
		public Color colorThree = Color.white;
		public PatternDef pattern;

		private Graphic_RGB cachedGraphic;

		public new Graphic_RGB Graphic
		{
			get
			{
				if (cachedGraphic is null)
				{
					Init();
				}
				return cachedGraphic;
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
				cachedGraphic = null;
				return;
			}
			ShaderTypeDef cutout = shaderType;
			if (cutout == null)
			{
				cutout = RGBShaderTypeDefOf.CutoutComplexRGB;
			}
			Shader shader = cutout.Shader;
			cachedGraphic = GraphicDatabaseRGB.Get(graphicClass, texPath, shader, drawSize, color, colorTwo, colorThree, tiles, this, shaderParameters);
		}
	}
}
