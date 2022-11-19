using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Command_HighlightRadius : Command_ActionHighlighter
	{
		public int radius;
		public bool needsLOS;
		public IntVec3 center;
		public Color color;

		public override void GizmoUpdateOnMouseover()
		{
			base.GizmoUpdateOnMouseover();
			if (radius > 0)
			{
				if (needsLOS)
				{
					GenDraw.DrawFieldEdges(RocketTakeoff.CellsToBurn(center, Find.CurrentMap, radius).ToList(), color);
				}
				else
				{
					GenDraw.DrawRadiusRing(center, radius, color);
				}
			}
		}
	}
}
