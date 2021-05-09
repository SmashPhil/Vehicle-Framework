using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Graphic_CannonAnimate : Graphic_Cannon
	{
		protected string[] graphicPaths;
		protected Graphic_Cannon[] subGraphics;
		protected Texture2D[] subTextures;

		public Texture2D[] SubTextures => subTextures;

		public override Material MatSingle => subGraphics[0].MatSingle;

		public int AnimationFrameCount
		{
			get
			{
				return graphicPaths.Length;
			}
		}

		public override void Init(GraphicRequestRGB req, bool cacheResults = true)
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
			var files = ContentFinder<Texture2D>.GetAllInFolder(req.path);

			List<Texture2D> list = (from x in files
									where !x.name.EndsWith(MaskSuffix)
									orderby x.name
									select x).ToList();
			List<Texture2D> listM = (from x in files
									where x.name.EndsWith(MaskSuffix)
									orderby x.name
									select x).ToList();
			if (list.NullOrEmpty())
			{
				Log.Error("Collection cannot init: No textures found at path " + req.path);
				graphicPaths = new string[]
				{
					BaseContent.BadGraphic.path
				};
				return;
			}
			graphicPaths = new string[list.Count];
			subGraphics = new Graphic_Cannon[list.Count];
			subTextures = list.ToArray();
			if(list.Count != listM.Count && !listM.NullOrEmpty())
			{
				Log.Error($"{VehicleHarmony.LogLabel} Could not apply masks for animation classes at path {req.path}. " +
					$"Mask and texture count do not match up." +
					$"\nMainTextures: {list.Count} Masks: {listM.Count}");
				graphicPaths = new string[]
				{
					BaseContent.BadGraphic.path
				};
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				string fullPath = string.Concat(req.path, '/', list[i].name);
				graphicPaths[i] = fullPath;
				subGraphics[i] = GraphicDatabaseRGB.Get<Graphic_Cannon>(fullPath, req.shader, req.drawSize, req.color, req.colorTwo, req.colorThree, DataRGB, req.shaderParameters) as Graphic_Cannon;
			}
		}

		protected override Material[] GenerateMasks(GraphicRequestRGB req, PatternDef pattern)
		{
			Texture2D mainTex = ContentFinder<Texture2D>.Get(req.path);
			Texture2D maskTex = ContentFinder<Texture2D>.Get(req.path + MaskSuffix, false);
			masks = Enumerable.Repeat(maskTex, MatCount).ToArray();
			var mats = new Material[MatCount];
			MaterialRequestRGB mReq = new MaterialRequestRGB()
			{
				mainTex = mainTex,
				shader = req.shader,
				color = color,
				colorTwo = colorTwo,
				colorThree = colorThree,
				replaceTex = pattern.replaceTex,
				maskTex = req.shader.SupportsMaskTex() || req.shader.SupportsRGBMaskTex() ? maskTex : null,
				patternTex = pattern?[Rot8.North],
				shaderParameters = req.shaderParameters
			};
			mats[0] = MaterialPoolExpanded.MatFrom(mReq);
			return mats;
		}

		public Graphic SubGraphicCycle(int index, Shader newShader, Color colorOne, Color colorTwo, Color colorThree)
		{
			if(index > (graphicPaths.Length - 1) )
			{
				Log.Warning($"Graphic Retrieval for Graphic_CannonAnimate indexed past maximum length of {graphicPaths.Length}. Self correcting.");
				while (index > (graphicPaths.Length - 1))
				{
					index -= (graphicPaths.Length - 1);
				}
			}
			return GraphicDatabaseRGB.Get<Graphic_Cannon>(graphicPaths[index], newShader, drawSize, colorOne, colorTwo, colorThree, DataRGB);
		}

		public Material SubMaterialCycle(PatternDef pattern, int index)
		{
			if(index > (subGraphics.Length - 1) )
			{
				Log.Warning($"Graphic Retrieval for Graphic_CannonAnimate indexed past maximum length of {subGraphics.Length}. Self correcting.");
				while (index > (subGraphics.Length - 1))
				{
					index -= (subGraphics.Length - 1);
				}
			}
			return subGraphics[index].maskMatPatterns.TryGetValue(pattern, subGraphics[index].maskMatPatterns[PatternDefOf.Default]).Second[0];
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_CannonAnimate>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

		public override Graphic_RGB GetColoredVersion(Shader shader, Color colorOne, Color colorTwo, Color colorThree)
		{
			return GraphicDatabaseRGB.Get<Graphic_CannonAnimate>(path, shader, drawSize, colorOne, colorTwo, colorThree, DataRGB);
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"AnimationCount(path=",
				path,
				", count=",
				maskMatPatterns.Count,
				")"
			});
		}
	}
}
