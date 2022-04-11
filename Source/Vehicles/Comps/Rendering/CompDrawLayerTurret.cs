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
	public class CompDrawLayerTurret : ThingComp
	{
		private List<Graphic> graphicInt;

		public CompProperties_DrawLayerTurret Props => props as CompProperties_DrawLayerTurret;

		public List<Graphic> GraphicList
		{
			get
			{
				if (graphicInt.NullOrEmpty())
				{
					graphicInt = new List<Graphic>();
					foreach (GraphicData data in Props.graphicDatas)
					{
						graphicInt.Add(data.Graphic.GetColoredVersion(data.Graphic.Shader, parent.DrawColor, parent.DrawColorTwo));
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

		public void DrawExtra(Vector3 loc, float angle)
		{
			foreach (Graphic graphic in GraphicList)
			{
				Vector3 gLoc = loc;
				Mesh mesh = graphic.MeshAt(parent.Rotation);
				Quaternion quaternion = Quaternion.identity * Quaternion.Euler(Vector3.up * angle);
				gLoc += graphic.DrawOffset(parent.Rotation);
				Material mat = graphic.MatAt(parent.Rotation);
				Graphics.DrawMesh(mesh, gLoc, quaternion, mat, 0);
				if (graphic.ShadowGraphic != null)
				{
					graphic.ShadowGraphic.DrawWorker(gLoc, parent.Rotation, parent.def, null, angle);
				}
			}
		}
	}
}
