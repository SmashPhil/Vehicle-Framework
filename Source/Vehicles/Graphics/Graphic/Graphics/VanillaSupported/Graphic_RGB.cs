using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public abstract class Graphic_RGB : Graphic
	{
		public const string MaskSuffix = "m";
		public const int MatCount = 8;

		protected bool westFlipped;
		protected bool eastFlipped;
		protected bool eastRotated;
		protected bool southRotated;
		protected bool eastDiagonalRotated;
		protected bool westDiagonalRotated;

		protected float drawRotatedExtraAngleOffset;

		public Color colorThree = Color.white;
		public float tiles = 1;
		public Vector2 displacement = Vector2.zero;

		/// <summary>
		/// Needs to be initialized and filled in <see cref="Init(GraphicRequestRGB, bool)"/> before mask data is generated
		/// </summary>
		protected Texture2D[] textureArray;

		//folderName : <filePath, texture/mat array>
		public Texture2D[] masks;
		public Dictionary<PatternDef, Pair<string, Material[]>> maskMatPatterns = new Dictionary<PatternDef, Pair<string, Material[]>>();

		public override Material MatSingle => MatNorth;

		public override bool WestFlipped => westFlipped;
		public override bool EastFlipped => eastFlipped;
		public virtual bool EastRotated => eastRotated;
		public virtual bool SouthRotated => southRotated;
		public virtual bool EastDiagonalRotated => eastDiagonalRotated;
		public virtual bool WestDiagonalRotated => westDiagonalRotated;

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

		public virtual Texture2D TexAt(Rot8 rot)
		{
			return textureArray[rot.AsInt];
		}

		public override Mesh MeshAt(Rot4 rot)
		{
			Vector2 vector = drawSize;
			if (rot.IsHorizontal && !ShouldDrawRotated)
			{
				vector = vector.Rotated();
			}
			if ((rot == Rot4.West && WestFlipped) || (rot == Rot4.East && EastFlipped))
			{
				if (EastRotated)
				{
					return RenderHelper.NewPlaneMesh(vector, rot.AsInt);
				}
				return MeshPool.GridPlaneFlip(vector);
			}
			if ((EastRotated && rot == Rot4.East) || (SouthRotated && rot == Rot4.South))
			{
				return RenderHelper.NewPlaneMesh(vector, rot.AsInt);
			}
			return MeshPool.GridPlane(vector);
		}

		public virtual Mesh MeshAtFull(Rot8 rot)
		{
			if (!rot.IsDiagonal)
			{
				return MeshAt(rot);
			}
			if (EastDiagonalRotated)
			{
				if (rot == Rot8.NorthEast)
				{
					return MeshAt(Rot8.North);
				}
				if (rot == Rot8.SouthEast)
				{
					return MeshAt(Rot8.South);
				}
			}
			if (WestDiagonalRotated)
			{
				if (rot == Rot8.NorthWest)
				{
					return MeshAt(Rot8.North);
				}
				if (rot == Rot8.SouthWest)
				{
					return MeshAt(Rot8.South);
				}
			}
			return MeshAt(rot);
		}

		public override void Init(GraphicRequest req)
		{
			if (req.shader.SupportsRGBMaskTex())
			{
				//SmashLog.Error($"<type>Graphic_RGB</type> called <method>Init</method> with regular GraphicRequest. This will result in incorrectly colored RGB masks as <property>ColorThree</property> cannot be properly initialized.");
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
			tiles = req.tiles;
			displacement = req.displacement;
			drawSize = req.drawSize;
		}

		protected virtual Material[] GenerateMasks(GraphicRequestRGB req, PatternDef pattern)
		{
			var tmpMaskArray = new Texture2D[MatCount];
			var patternPointers = new int[MatCount] { 0, 1, 2, 3, 4, 5, 6, 7 };

			if (req.shader.SupportsRGBMaskTex() || req.shader.SupportsMaskTex())
			{
				tmpMaskArray[0] = ContentFinder<Texture2D>.Get(req.path + "_north" + MaskSuffix, false);
				tmpMaskArray[0] ??= ContentFinder<Texture2D>.Get(req.path + Graphic_Single.MaskSuffix, false); // _m for single texture to remain consistent with vanilla
				tmpMaskArray[1] = ContentFinder<Texture2D>.Get(req.path + "_east" + MaskSuffix, false);
				tmpMaskArray[2] = ContentFinder<Texture2D>.Get(req.path + "_south" + MaskSuffix, false);
				tmpMaskArray[3] = ContentFinder<Texture2D>.Get(req.path + "_west" + MaskSuffix, false);
				tmpMaskArray[4] = ContentFinder<Texture2D>.Get(req.path + "_northEast" + MaskSuffix, false);
				tmpMaskArray[5] = ContentFinder<Texture2D>.Get(req.path + "_southEast" + MaskSuffix, false);
				tmpMaskArray[6] = ContentFinder<Texture2D>.Get(req.path + "_southWest" + MaskSuffix, false);
				tmpMaskArray[7] = ContentFinder<Texture2D>.Get(req.path + "_northWest" + MaskSuffix, false);
				if (tmpMaskArray[0] is null)
				{
					if (tmpMaskArray[2] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[2];
						patternPointers[0] = 2;
					}
					else if (tmpMaskArray[1] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[1];
						patternPointers[0] = 1;
					}
					else if (tmpMaskArray[3] != null)
					{
						tmpMaskArray[0] = tmpMaskArray[3];
						patternPointers[0] = 3;
					}
				}
				if (tmpMaskArray[0] is null)
				{
					Log.Error("Failed to find any mask textures at " + req.path + " while constructing " + this.ToStringSafe());
					return null;
				}
				if (tmpMaskArray[2] is null)
				{
					tmpMaskArray[2] = tmpMaskArray[0];
					patternPointers[2] = 0;
					southRotated = DataAllowsFlip;
				}
				if (tmpMaskArray[1] is null)
				{
					if (tmpMaskArray[3] != null)
					{
						tmpMaskArray[1] = tmpMaskArray[3];
						patternPointers[1] = 3;
					}
					else
					{
						tmpMaskArray[1] = tmpMaskArray[0];
						patternPointers[1] = 0;
						eastRotated = DataAllowsFlip;
					}
				}
				if (tmpMaskArray[3] is null)
				{
					if (tmpMaskArray[1] != null)
					{
						tmpMaskArray[3] = tmpMaskArray[1];
						patternPointers[3] = 0;
						westFlipped = DataAllowsFlip;
					}
					else
					{
						tmpMaskArray[3] = tmpMaskArray[0];
						patternPointers[3] = 0;
					}
				}

				if (tmpMaskArray[4] is null)
				{
					tmpMaskArray[4] = tmpMaskArray[0];
					patternPointers[4] = 0;
					eastDiagonalRotated = DataAllowsFlip;
				}
				if (tmpMaskArray[5] is null)
				{
					tmpMaskArray[5] = tmpMaskArray[2];
					patternPointers[5] = 0;
					eastDiagonalRotated = DataAllowsFlip;
				}
				if (tmpMaskArray[6] is null)
				{
					tmpMaskArray[6] = tmpMaskArray[2];
					patternPointers[6] = 0;
					westDiagonalRotated = DataAllowsFlip;
				}
				if (tmpMaskArray[7] is null)
				{
					tmpMaskArray[7] = tmpMaskArray[0];
					patternPointers[7] = 0;
					westDiagonalRotated = DataAllowsFlip;
				}
				masks = tmpMaskArray;
			}
			
			var mats = new Material[MatCount];
			for (int i = 0; i < MatCount; i++)
			{
				MaterialRequestRGB req2 = new MaterialRequestRGB()
				{
					mainTex = textureArray[i],
					shader = pattern is SkinDef ? RGBShaderTypeDefOf.CutoutComplexSkin.Shader : req.shader,
					properties = pattern.properties,
					color = pattern.properties.colorOne ?? req.color,
					colorTwo = pattern.properties.colorTwo ?? req.colorTwo,
					colorThree = pattern.properties.colorThree ?? req.colorThree,
					tiles = req.tiles,
					displacement = req.displacement,
					maskTex = tmpMaskArray[i],
					patternTex = pattern[new Rot8(patternPointers[i])],
					shaderParameters = req.shaderParameters,
				};
				mats[i] = MaterialPoolExpanded.MatFrom(req2);
			}
			return mats;
		}

		public abstract Graphic_RGB GetColoredVersion(Shader shader, Color colorOne, Color colorTwo, Color colorThree, float tiles = 1, float displacementX = 0, float displacementY = 0);

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
