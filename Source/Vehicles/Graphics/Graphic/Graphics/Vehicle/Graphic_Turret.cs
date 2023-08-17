using System;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Graphic_Turret : Graphic_RGB
	{
		public static string TurretMaskSuffix = "_m";

		protected int matCount;

		protected string graphicPath;
		protected Material material;

		public override Material MatSingle => materials[0];

		public override int MatCount => 1;

		public override void Init(GraphicRequestRGB req)
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
			//Texture2D mainTex = ContentFinder<Texture2D>.Get(req.path);
			//textures = new Texture2D[] { mainTex };
			materials = RGBMaterialPool.GetAll(req.target);
		}

		protected override void GetMasks(string path, Shader shader)
		{
			patternPointers = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };

			Texture2D maskTex = ContentFinder<Texture2D>.Get(path + TurretMaskSuffix, false);
			masks = Enumerable.Repeat(maskTex, MatCount).ToArray();
		}

		public virtual Material MatAtFull(Rot8 rot)
		{
			if (materials.OutOfBounds(rot.AsInt))
			{
				return BaseContent.BadMat;
			}
			return materials[rot.AsInt];
		}

		public virtual Material MatAt(PatternDef pattern, int index = 0)
		{
			if (pattern != null && maskMatPatterns.TryGetValue(pattern, out var values))
			{
				return values.materials[index];
			}
			else
			{
				Log.Error($"{VehicleHarmony.LogLabel} Key {pattern?.defName ?? "[Null]"} not found in {GetType()}.");
				string folders = string.Empty;
				foreach ((PatternDef patternDef, (string texPath, Material[] materials)) in maskMatPatterns)
				{
					folders += $"Item: {patternDef} Destination: {texPath}\n";
				}
				Debug.Message($"{VehicleHarmony.LogLabel} Additional Information:\n" +
					$"MatCount: {maskMatPatterns.Count}\n" +
					$"{folders}");
			}
			return BaseContent.BadMat;
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Turret>(path, newShader, drawSize, newColor, newColorTwo, DataRGB);
		}
	}
}
