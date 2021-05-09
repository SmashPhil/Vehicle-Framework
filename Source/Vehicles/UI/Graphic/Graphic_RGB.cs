using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public abstract class Graphic_RGB : Graphic
	{
		public const int MatCount = 8;

		protected bool westFlipped;
		protected bool eastFlipped;
		protected bool eastDiagonalFlipped;
		protected bool westDiagonalFlipped;

		protected float drawRotatedExtraAngleOffset;

		public Color colorThree = Color.white;

		protected Texture2D[] textureArray;

		//folderName : <filePath, texture/mat array>
		public Texture2D[] masks;
		public Dictionary<PatternDef, Pair<string, Material[]>> maskMatPatterns = new Dictionary<PatternDef, Pair<string, Material[]>>();

		public override Material MatSingle => MatNorth;

		public override bool WestFlipped => westFlipped;
		public override bool EastFlipped => eastFlipped;
		public override bool ShouldDrawRotated => (data is null || data.drawRotated) && (MatEast == MatNorth || MatWest == MatNorth);
		public override float DrawRotatedExtraAngleOffset => drawRotatedExtraAngleOffset;

		public virtual GraphicDataRGB DataRGB
		{
			get
			{
				if (data is GraphicDataRGB rgbData)
				{
					return rgbData;
				}
				SmashLog.ErrorOnce($"Unable to retrieve <field>DataRGB</field> for <type>{GetType()}</type>. GraphicData type = {data?.GetType().ToString() ?? "Null"}", GetHashCode());
				return null;
			}
		}

		public override void Init(GraphicRequest req)
		{
			if (req.shader.SupportsRGBMaskTex())
			{
				SmashLog.Error($"<type>Graphic_RGB</type> called <method>Init</method> with regular GraphicRequest. This will result in incorrectly colored RGB masks as <property>ColorThree</property> cannot be properly initialized.");
			}
			Init(new GraphicRequestRGB(req), false);
		}

		public virtual void Init(GraphicRequestRGB req, bool cacheResults = true)
		{
			if (cacheResults is true)
			{
				masks = new Texture2D[MatCount];
				maskMatPatterns = new Dictionary<PatternDef, Pair<string, Material[]>>();
			}
			data = req.graphicData;
			path = req.path;
			color = req.color;
			colorTwo = req.colorTwo;
			colorThree = req.colorThree;
			drawSize = req.drawSize;
		}

		protected virtual Material[] GenerateMasks(GraphicRequestRGB req, PatternDef pattern)
		{
			var tmpMaskArray = new Texture2D[MatCount];
			if (req.shader.SupportsRGBMaskTex())
			{
				tmpMaskArray[0] = ContentFinder<Texture2D>.Get(req.path + "_northm", true);
				tmpMaskArray[1] = ContentFinder<Texture2D>.Get(req.path + "_eastm", false);
				tmpMaskArray[2] = ContentFinder<Texture2D>.Get(req.path + "_southm", false);
				tmpMaskArray[3] = ContentFinder<Texture2D>.Get(req.path + "_westm", false);
				tmpMaskArray[4] = ContentFinder<Texture2D>.Get(req.path + "_northEastm", false);
				tmpMaskArray[5] = ContentFinder<Texture2D>.Get(req.path + "_southEastm", false);
				tmpMaskArray[6] = ContentFinder<Texture2D>.Get(req.path + "_southWestm", false);
				tmpMaskArray[7] = ContentFinder<Texture2D>.Get(req.path + "_northWestm", false);

				if (tmpMaskArray[0] is null)
				{
					if (tmpMaskArray[2] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[2];
					}
					else if (tmpMaskArray[1] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[1];
					}
					else if (tmpMaskArray[3] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[3];
					}
				}
				if (tmpMaskArray[2] is null)
				{
					tmpMaskArray[2] = tmpMaskArray[0];
				}
				if (tmpMaskArray[1] is null)
				{
					if (tmpMaskArray[3] != null)
					{
						tmpMaskArray[1] = tmpMaskArray[3];
					}
					else
					{
						tmpMaskArray[1] = tmpMaskArray[0];
					}
				}
				if (tmpMaskArray[3] is null)
				{
					if (tmpMaskArray[1] != null)
					{
						tmpMaskArray[3] = tmpMaskArray[1];
					}
					else
					{
						tmpMaskArray[3] = tmpMaskArray[0];
					}
				}

				if(tmpMaskArray[5] is null)
				{
					if(tmpMaskArray[4] != null)
					{
						tmpMaskArray[5] = tmpMaskArray[4];
						eastDiagonalFlipped = DataAllowsFlip;
					}
					else
					{
						tmpMaskArray[5] = tmpMaskArray[1];
					}
				}
				if(tmpMaskArray[6] is null)
				{
					if(tmpMaskArray[7] != null)
					{
						tmpMaskArray[6] = tmpMaskArray[7];
						westDiagonalFlipped = DataAllowsFlip;
					}
					else
					{
						tmpMaskArray[6] = tmpMaskArray[3];
					}
				}
				if(tmpMaskArray[4] is null)
				{
					if(tmpMaskArray[5] != null)
					{
						tmpMaskArray[4] = tmpMaskArray[5];
						eastDiagonalFlipped = DataAllowsFlip;
					}
					else
					{
						tmpMaskArray[4] = tmpMaskArray[1];
					}
				}
				if(tmpMaskArray[7] is null)
				{
					if(tmpMaskArray[6] != null)
					{
						tmpMaskArray[7] = tmpMaskArray[6];
						westDiagonalFlipped = DataAllowsFlip;
					}
					else
					{
						tmpMaskArray[7] = tmpMaskArray[3];
					}
				}
				masks = tmpMaskArray;
			}
			
			var mats = new Material[MatCount];
			for (int i = 0; i < MatCount; i++)
			{
				MaterialRequestRGB req2 = new MaterialRequestRGB()
				{
					mainTex = textureArray[i],
					shader = req.shader,
					color = color,
					colorTwo = colorTwo,
					colorThree = colorThree,
					replaceTex = pattern.replaceTex,
					maskTex = tmpMaskArray[i],
					patternTex = pattern[new Rot8(i)],
					shaderParameters = req.shaderParameters
				};
				mats[i] = MaterialPoolExpanded.MatFrom(req2);
			}
			return mats;
		}

		public abstract Graphic_RGB GetColoredVersion(Shader shader, Color colorOne, Color colorTwo, Color colorThree);

		public override string ToString()
		{
			return $"{GetType()} (initPath={path}, color={color}, colorTwo={colorTwo}, colorThree={colorThree})";
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(0, path), color), colorTwo), colorThree);
		}
	}
}
