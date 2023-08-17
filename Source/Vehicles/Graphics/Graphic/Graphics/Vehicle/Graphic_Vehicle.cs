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

		public Material MatAtFull(Rot8 rot)
		{
			if (materials.OutOfBounds(rot.AsInt))
			{
				return BaseContent.BadMat;
			}
			return materials[rot.AsInt];
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
