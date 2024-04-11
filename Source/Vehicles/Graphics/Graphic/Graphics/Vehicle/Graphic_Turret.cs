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

		public override Material MatNorth => MatSingle;

		public override Material MatEast => MatSingle;

		public override Material MatSouth => MatSingle;

		public override Material MatWest => MatSingle;

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

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Turret>(path, newShader, drawSize, newColor, newColorTwo, DataRGB);
		}
	}
}
