using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Graphic_Vehicle : Graphic_RGB
	{
		public override int MatCount => 8;

		public IEnumerable<Rot8> RotationsRenderableByUI
		{
			get
			{
				yield return Rot8.North;
				yield return Rot8.South; //Can be rotated in UI if necessary
				if (!eastFlipped)
				{
					yield return Rot8.East;
				}
				if (!westFlipped)
				{
					yield return Rot8.West;
				}
			}
		}

		public override void Init(GraphicRequestRGB req)
		{
			base.Init(req);
			materials = RGBMaterialPool.GetAll(req.target);
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			Log.Warning($"Retrieving {GetType()} Colored Graphic from vanilla GraphicDatabase which will result in redundant graphic creation.");
			return GraphicDatabase.Get<Graphic_Vehicle>(path, newShader, drawSize, newColor, newColorTwo, DataRGB);
		}
	}
}
