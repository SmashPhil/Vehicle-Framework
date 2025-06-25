using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using LudeonTK;

namespace Vehicles
{
	public class Graphic_DynamicShadow : Graphic
	{
		private readonly ShadowData shadowData;

		private Mesh shadowMesh;

		public Graphic_DynamicShadow(Texture2D texture, ShadowData shadowData)
		{
			this.shadowData = shadowData;
			if (shadowData == null)
			{
				throw new ArgumentNullException(nameof(shadowData));
			}
			shadowMesh = DynamicShadows.GetShadowMesh(texture, shadowData);
		}

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			if (shadowMesh != null && shadowData != null && (Find.CurrentMap == null || !loc.ToIntVec3().InBounds(Find.CurrentMap) || !Find.CurrentMap.roofGrid.Roofed(loc.ToIntVec3())) && DebugViewSettings.drawShadows)
			{
				Vector3 position = loc + shadowData.offset;
				position.y = AltitudeLayer.Shadows.AltitudeFor();
				Graphics.DrawMesh(shadowMesh, position, rot.AsQuat, MatBases.SunShadowFade, 0);
			}
		}

		public override void Print(SectionLayer layer, Thing thing, float extraRotation)
		{
			
		}

		public override string ToString()
		{
			return $"Graphic_DynamicShadow({shadowData})";
		}
	}
}
