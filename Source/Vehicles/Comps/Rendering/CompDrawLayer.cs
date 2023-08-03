using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[Obsolete]
	public class CompDrawLayer : ThingComp
	{
		private List<Graphic> graphicInt;
		private float extraRotation = 0;

		public CompProperties_DrawLayer Props => props as CompProperties_DrawLayer;

		public List<Graphic> GraphicList
		{
			get
			{
				if (graphicInt.NullOrEmpty())
				{
					graphicInt = new List<Graphic>();
					foreach (GraphicData data in Props.graphicDatas)
					{
						graphicInt.Add(data.Graphic.GetColoredVersion(data.Graphic.Shader, data.color, data.colorTwo));
					}
				}
				return graphicInt;
			}
		}

		protected float AngleFromRot(Graphic graphic)
		{
			if (graphic.ShouldDrawRotated)
			{
				float num = parent.Rotation.AsAngle;
				num += graphic.DrawRotatedExtraAngleOffset;
				if ((parent.Rotation == Rot4.West && graphic.WestFlipped) || (parent.Rotation == Rot4.East && graphic.EastFlipped))
				{
					num += 180f;
				}
				return num;
			}
			return 0f;
		}

		protected Quaternion QuatFromRot(Graphic graphic)
		{
			float num = AngleFromRot(graphic);
			if (num == 0f)
			{
				return Quaternion.identity;
			}
			return Quaternion.AngleAxis(num, Vector3.up);
		}

		public override void PostDraw()
		{
			foreach (Graphic graphic in GraphicList)
			{
				Mesh mesh = graphic.MeshAt(parent.Rotation);
				Quaternion quaternion = QuatFromRot(graphic);
				Vector3 loc = parent.DrawPos;
				if (extraRotation != 0f)
				{
					quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
				}
				loc += graphic.DrawOffset(parent.Rotation);
				Material mat = graphic.MatAt(parent.Rotation);
				Graphics.DrawMesh(mesh, loc, quaternion, mat, 0);
				if (graphic.ShadowGraphic != null)
				{
					graphic.ShadowGraphic.DrawWorker(loc, parent.Rotation, parent.def, null, extraRotation);
				}
			}
		}
	}
}
