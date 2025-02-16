using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Graphic_RGB : Graphic
	{
		public const string MaskSuffix = "m";

		private static readonly string[] pathExtensions = ["_north", "_east", "_south", "_west",
																	     "_northEast","_southEast","_southWest","_northWest" ];
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
		/// Needs to be initialized and filled in <see cref="Init(GraphicRequestRGB)"/> before mask data is generated
		/// </summary>
		protected Texture2D[] textures;
		protected Texture2D[] masks;

		//folderName : <filePath, texture/mat array>
		public Material[] materials;
		public int[] patternPointers;

		public virtual int MatCount => 8;

		public override bool WestFlipped => westFlipped;

		public override bool EastFlipped => eastFlipped;

		public bool EastRotated => eastRotated;

		public bool SouthRotated => southRotated;

		public bool EastDiagonalRotated => eastDiagonalRotated;

		public bool WestDiagonalRotated => westDiagonalRotated;

		public bool DiagonalRotated => EastDiagonalRotated || WestDiagonalRotated;

		public override bool ShouldDrawRotated => (data is null || data.drawRotated) && (MatEast == MatNorth || MatWest == MatNorth);

		public override float DrawRotatedExtraAngleOffset => drawRotatedExtraAngleOffset;

		public override Material MatSingle => MatNorth;

		public override Material MatNorth
		{
			get
			{
				return materials[0];
			}
		}

		public override Material MatEast
		{
			get
			{
				return materials[1];
			}
		}

		public override Material MatSouth
		{
			get
			{
				return materials[2];
			}
		}

		public override Material MatWest
		{
			get
			{
				return materials[3];
			}
		}

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
			return textures[rot.AsInt];
		}

		public virtual Texture2D MaskAt(Rot8 rot)
		{
			return masks[rot.AsInt];
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

		public Material MatAtFull(Rot8 rot)
		{
			if (materials.OutOfBounds(rot.AsInt))
			{
				return BaseContent.BadMat;
			}
			return materials[rot.AsInt];
		}

		public override void Init(GraphicRequest req)
		{
			masks = new Texture2D[MatCount];
			materials = new Material[MatCount];

			if (req.shader.SupportsRGBMaskTex())
			{
				Log.Warning($"Calling Non-RGB Init with shader type that supports RGB Shaders. Req={req.path} ShaderType={req.graphicData.shaderType}");
			}
			CopyData(req);
			GetTextures(req.path);
			if (req.shader.SupportsMaskTex() || req.shader.SupportsRGBMaskTex())
			{
				GetMasks(req.path, req.shader);
			}
			for (int i = 0; i < masks.Length; i++)
			{
				MaterialRequest matReq2 = new()
				{
					mainTex = textures[i],
					maskTex = masks[i],
					shader = req.shader,
					color = req.color,
					colorTwo = req.colorTwo,
					renderQueue = req.renderQueue,
					shaderParameters = req.shaderParameters
				};
				materials[i] = MaterialPool.MatFrom(matReq2);
			}
		}

		public virtual void Init(GraphicRequestRGB req)
		{
			if (!req.shader.SupportsRGBMaskTex())
			{
				Log.Warning($"Calling RGB Init with unsupported shader type. Req={req}");
			}

			masks = new Texture2D[MatCount];
			materials = new Material[MatCount];
			
			CopyData(req);
			colorThree = req.colorThree;
			tiles = req.tiles;
			displacement = req.displacement;

			GetTextures(req.path);

			if (req.shader.SupportsMaskTex() || req.shader.SupportsRGBMaskTex())
			{
				GetMasks(req.path, req.shader);
			}
		}

		private void CopyData(GraphicRequest req)
		{
			data = req.graphicData;
			path = req.path;
			maskPath = req.maskPath;
			color = req.color;
			colorTwo = req.colorTwo;
			drawSize = req.drawSize;
		}

		protected virtual void GetTextures(string path)
		{
			textures = new Texture2D[MatCount];

			for (int i = 0; i < MatCount; i++)
			{
				textures[i] = ContentFinder<Texture2D>.Get(path + pathExtensions[i], false); //Autosizes depending on the MatCount
			}

			if (!textures[0])
			{
				textures[0] = ContentFinder<Texture2D>.Get(path, false);
			}

			if (MatCount >= 4)
			{
				if (!textures[0])
				{
					if (textures[2])
					{
						textures[0] = textures[2];
						drawRotatedExtraAngleOffset = 180f;
					}
					else if (textures[1])
					{
						textures[0] = textures[1];
						drawRotatedExtraAngleOffset = -90f;
					}
					else if (textures[3])
					{
						textures[0] = textures[3];
						drawRotatedExtraAngleOffset = 90f;
					}
				}
				if (!textures[0])
				{
					Log.Error("Failed to find any textures at " + path + " while constructing " + this.ToStringSafe());
					return;
				}
				if (!textures[2])
				{
					textures[2] = textures[0];
					southRotated = DataAllowsFlip;
				}
				if (!textures[1])
				{
					if (textures[3])
					{
						textures[1] = textures[3];
						eastFlipped = DataAllowsFlip;
					}
					else
					{
						textures[1] = textures[0];
						eastRotated = DataAllowsFlip;
					}
				}
				if (!textures[3])
				{
					if (textures[1])
					{
						textures[3] = textures[1];
						westFlipped = DataAllowsFlip;
					}
					else
					{
						textures[3] = textures[0];
					}
				}
			}
			if (MatCount == 8)
			{
				if (!textures[4])
				{
					if (textures[7])
					{
						textures[4] = textures[7];
					}
					else
					{
						textures[4] = textures[0];
					}
					eastDiagonalRotated = DataAllowsFlip;
				}
				if (!textures[5])
				{
					if (textures[6])
					{
						textures[5] = textures[6];
					}
					else
					{
						textures[5] = textures[2];
					}
					eastDiagonalRotated = DataAllowsFlip;
				}
				if (!textures[6])
				{
					if (textures[5])
					{
						textures[6] = textures[5];
					}
					else
					{
						textures[6] = textures[2];
					}
					westDiagonalRotated = DataAllowsFlip;
				}
				if (!textures[7])
				{
					if (textures[4])
					{
						textures[7] = textures[4];
					}
					else
					{
						textures[7] = textures[0];
					}
					westDiagonalRotated = DataAllowsFlip;
				}
			}
		}

		protected virtual void GetMasks(string path, Shader shader)
		{
			patternPointers = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
			if (shader.SupportsRGBMaskTex() || shader.SupportsMaskTex())
			{
				for (int i = 0; i < MatCount; i++)
				{
					masks[i] ??= ContentFinder<Texture2D>.Get(path + pathExtensions[i] + MaskSuffix, false); //Autosizes depending on the MatCount
				}
				if (!masks[0])
				{
					masks[0] = ContentFinder<Texture2D>.Get(path + Graphic_Single.MaskSuffix, false); // _m for single texture to remain consistent with vanilla
				}

				if (MatCount >= 4)
				{
					if (!masks[0])
					{
						if (masks[2])
						{
							masks[0] = masks[2];
							patternPointers[0] = 2;
						}
						else if (masks[1])
						{
							masks[0] = masks[1];
							patternPointers[0] = 1;
						}
						else if (masks[3])
						{
							masks[0] = masks[3];
							patternPointers[0] = 3;
						}
					}
					if (!masks[0])
					{
						return;
					}
					if (!masks[2])
					{
						masks[2] = masks[0];
						patternPointers[2] = 0;
					}
					if (!masks[1])
					{
						if (masks[3])
						{
							masks[1] = masks[3];
							patternPointers[1] = 3;
						}
						else
						{
							masks[1] = masks[0];
							patternPointers[1] = 0;
						}
					}
					if (!masks[3])
					{
						masks[3] = masks[1];
						patternPointers[3] = 1;
					}
				}
				
				if (MatCount == 8)
				{
					if (!masks[4])
					{
						masks[4] = masks[0];
						patternPointers[4] = 0;
						eastDiagonalRotated = DataAllowsFlip;
					}
					if (!masks[5])
					{
						masks[5] = masks[2];
						patternPointers[5] = 2;
						eastDiagonalRotated = DataAllowsFlip;
					}
					if (!masks[6])
					{
						masks[6] = masks[2];
						patternPointers[6] = 2;
						westDiagonalRotated = DataAllowsFlip;
					}
					if (!masks[7])
					{
						masks[7] = masks[0];
						patternPointers[7] = 0;
						westDiagonalRotated = DataAllowsFlip;
					}
				}
			}
		}

		public virtual void DrawWorker(Vector3 loc, Rot8 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			Mesh mesh = MeshAtFull(rot);
			Quaternion quaternion = QuatFromRot(rot);
			if (EastDiagonalRotated && (rot == Rot8.NorthEast || rot == Rot8.SouthEast) || (WestDiagonalRotated && (rot == Rot8.NorthWest || rot == Rot8.SouthWest)))
			{
				quaternion *= Quaternion.Euler(-Vector3.up);
			}
			if (extraRotation != 0f)
			{
				quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
			}
			if (data != null && data.addTopAltitudeBias)
			{
				quaternion *= Quaternion.Euler(Vector3.left * 2f);
			}
			loc += DrawOffset(rot);
			Material mat = MatAtFull(rot);
			DrawMeshInt(mesh, loc, quaternion, mat);
			if (ShadowGraphic != null)
			{
				ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
			}
		}

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
