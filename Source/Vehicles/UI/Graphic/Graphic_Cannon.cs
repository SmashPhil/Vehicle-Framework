﻿using System;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Graphic_Cannon : Graphic_RGB
	{
		public static string MaskSuffix = "_m";

		protected int matCount;

		protected string graphicPath;
		protected Material material;

		private Texture2D mainTex;

		public override Material MatSingle => maskMatPatterns.FirstOrDefault().Value.Second?[0];

		public override void Init(GraphicRequestRGB req, bool cacheResults = true)
		{
			base.Init(req);
			if (req.path.NullOrEmpty())
			{
				throw new ArgumentNullException("folderPath");
			}
			if (req.shader == null)
			{
				throw new ArgumentNullException("shader");
			}
			mainTex = ContentFinder<Texture2D>.Get(req.path);

			if (mainTex is null)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Graphic_Cannon cannot init: No texture found at path " + req.path);
				graphicPath = BaseContent.BadGraphic.path;
				material = BaseContent.BadMat;
				return;
			}
			else
			{
				foreach (PatternDef pattern in DefDatabase<PatternDef>.AllDefs)
				{
					maskMatPatterns.Add(pattern, new Pair<string, Material[]>(req.path, GenerateMasks(req, pattern)));
				}
			}
		}

		protected override Material[] GenerateMasks(GraphicRequestRGB req, PatternDef pattern)
		{
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
				maskTex = maskTex,
				patternTex = pattern?[Rot8.North],
				shaderParameters = req.shaderParameters
			};
			for (int i = 0; i < 8; i++)
			{
				mats[i] = MaterialPoolExpanded.MatFrom(mReq);
			}
			return mats;
		}

		public override Material MatAt(Rot4 rot, Thing thing = null)
		{
			if (thing is null || !(thing is VehiclePawn vehicle))
			{
				return base.MatAt(rot, thing);
			}
			if (!Shader.SupportsRGBMaskTex() || vehicle.pattern is null)
			{
				return maskMatPatterns[PatternDefOf.Default].Second[0];
			}
			if (maskMatPatterns.TryGetValue(vehicle.pattern, out var values))
			{
				return values.Second[0];
			}
			else
			{
				Log.Error($"{VehicleHarmony.LogLabel} Key {vehicle.pattern.defName} not found in {GetType()} for {vehicle}.");
				string folders = string.Empty;
				foreach(var item in maskMatPatterns)
				{
					folders += $"Item: {item.Key} Destination: {item.Value.First}\n";
				}
				Debug.Message($"{VehicleHarmony.LogLabel} Additional Information:\n" +
					$"MatCount: {maskMatPatterns.Count}\n" +
					$"{folders}");
			}
			return BaseContent.BadMat;
		}

		public virtual Material MatAt(VehiclePawn vehicle, int index = 0)
		{
			return null;
		}

		public virtual Material MatAt(PatternDef pattern, int index = 0)
		{
			if(pattern != null && maskMatPatterns.TryGetValue(pattern, out var values))
			{
				return values.Second[index];
			}
			else
			{
				Log.Error($"{VehicleHarmony.LogLabel} Key {pattern?.defName ?? "[Null]"} not found in {GetType()}.");
				string folders = string.Empty;
				foreach(var item in maskMatPatterns)
				{
					folders += $"Item: {item.Key} Destination: {item.Value.First}\n";
				}
				Debug.Message($"{VehicleHarmony.LogLabel} Additional Information:\n" +
					$"MatCount: {maskMatPatterns.Count}\n" +
					$"{folders}");
			}
			return BaseContent.BadMat;
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Cannon>(path, newShader, drawSize, newColor, newColorTwo, DataRGB);
		}

		public override Graphic_RGB GetColoredVersion(Shader shader, Color colorOne, Color colorTwo, Color colorThree)
		{
			return GraphicDatabaseRGB.Get<Graphic_Cannon>(path, shader, drawSize, colorOne, colorTwo, colorThree, DataRGB);
		}
	}
}