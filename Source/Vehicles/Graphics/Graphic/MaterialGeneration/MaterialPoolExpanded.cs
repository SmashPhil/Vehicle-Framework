using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	class MaterialPoolExpanded
	{
		private static readonly Dictionary<MaterialRequestRGB, Material> matDictionary = new Dictionary<MaterialRequestRGB, Material>();

		public static Material MatFrom(string texPath, bool reportFailure)
		{
			if (texPath is null || texPath == "null")
			{
				return null;
			}
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, reportFailure)));
		}

		public static Material MatFrom(string texPath)
		{
			if (texPath is null || texPath == "null")
			{
				return null;
			}
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true)));
		}

		public static Material MatFrom(Texture2D srcTex)
		{
			return MatFrom(new MaterialRequestRGB(srcTex));
		}

		public static Material MatFrom(Texture2D srcTex, Shader shader, PatternProperties properties)
		{
			return MatFrom(new MaterialRequestRGB(srcTex, shader, properties));
		}

		public static Material MatFrom(Texture2D srcTex, Shader shader, PatternProperties properties, int renderQueue)
		{
			return MatFrom(new MaterialRequestRGB(srcTex, shader, properties)
			{
				renderQueue = renderQueue
			});
		}

		public static Material MatFrom(string texPath, Shader shader)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader));
		}

		public static Material MatFrom(string texPath, Shader shader, int renderQueue)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader)
			{
				renderQueue = renderQueue
			});
		}

		public static Material MatFrom(string texPath, Shader shader, PatternProperties properties)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader, properties));
		}

		public static Material MatFrom(string texPath, Shader shader, PatternProperties properties, int renderQueue)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader, properties)
			{
				renderQueue = renderQueue
			});
		}

		public static Material MatFrom(MaterialRequestRGB req)
		{
			if (!UnityData.IsInMainThread)
			{
				Log.Error("Tried to get a material from a different thread.");
				return null;
			}
			if (req.mainTex == null)
			{
				Log.Error("MatFrom with null sourceTex.");
				return BaseContent.BadMat;
			}
			if (req.shader == null)
			{
				Log.Warning("Matfrom with null shader.");
				return BaseContent.BadMat;
			}
			if (req.maskTex != null && !req.shader.SupportsRGBMaskTex())
			{
				Log.Error("MaterialRequest has maskTex but shader does not support it. req=" + req.ToString());
				req.maskTex = null;
			}
			if (!matDictionary.TryGetValue(req, out Material material))
			{
				material = new Material(req.shader)
				{
					name = req.shader.name + "_" + req.mainTex.name,
					mainTexture = req.mainTex,
					color = req.color
				};
				if (req.maskTex != null)
				{
					var patternTex = req.patternTex is null ? PatternDefOf.Default[Rot8.North] : req.patternTex;
					if (!req.properties.IsDefault)
					{
						patternTex = Ext_Texture.WrapTexture(patternTex, TextureWrapMode.Repeat);
						float tiles = req.tiles;
						if (req.properties.tiles.TryGetValue("All", out float allTiles))
						{
							tiles *= allTiles;
						}
						if (tiles != 0)
						{
							material.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);
						}
						//Add case for vehicle defName
						if (req.properties.equalize)
						{
							float scaleX = 1;
							float scaleY = 1;
							if (req.mainTex.width > req.mainTex.height)
							{
								scaleY = (float)req.mainTex.height / req.mainTex.width;
							}
							else
							{
								scaleX = (float)req.mainTex.width / req.mainTex.height;
							}
							material.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
							material.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);
						}
						if (req.properties.dynamicTiling)
						{
							material.SetFloat(AdditionalShaderPropertyIDs.DisplacementX, req.displacement.x);
							material.SetFloat(AdditionalShaderPropertyIDs.DisplacementY, req.displacement.y);
						}
					}
					material.SetTexture(ShaderPropertyIDs.MaskTex, req.maskTex);
					material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
					material.SetFloat(AdditionalShaderPropertyIDs.ReplaceTexture, req.isSkin ? 1 : 0);
					material.SetColor(AdditionalShaderPropertyIDs.ColorOne, req.color);
					material.SetColor(ShaderPropertyIDs.ColorTwo, req.colorTwo);
					material.SetColor(AdditionalShaderPropertyIDs.ColorThree, req.colorThree);
				}
				if (req.renderQueue != 0)
				{
					material.renderQueue = req.renderQueue;
				}
				if (!req.shaderParameters.NullOrEmpty())
				{
					for (int i = 0; i < req.shaderParameters.Count; i++)
					{
						req.shaderParameters[i].Apply(material);
					}
				}
				matDictionary.Add(req, material);
			}
			return material;
		}
	}
}
