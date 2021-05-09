using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

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

		public static Material MatFrom(Texture2D srcTex, Shader shader, Color color)
		{
			return MatFrom(new MaterialRequestRGB(srcTex, shader, color));
		}

		public static Material MatFrom(Texture2D srcTex, Shader shader, Color color, int renderQueue)
		{
			return MatFrom(new MaterialRequestRGB(srcTex, shader, color)
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

		public static Material MatFrom(string texPath, Shader shader, Color color)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader, color));
		}

		public static Material MatFrom(string texPath, Shader shader, Color color, int renderQueue)
		{
			return MatFrom(new MaterialRequestRGB(ContentFinder<Texture2D>.Get(texPath, true), shader, color)
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
			if (req.maskTex != null && !req.shader.SupportsMaskTex() && !req.shader.SupportsRGBMaskTex())
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
					var patternTex = req.patternTex is null ? req.maskTex : req.patternTex;
					material.SetTexture(ShaderPropertyIDs.MaskTex, req.maskTex);
					material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
					material.SetInt(AdditionalShaderPropertyIDs.ReplaceTexture, req.replaceTex ? 1 : 0);
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
