using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Command_Highlight : Command_Action
	{
		public List<IntVec3> highlightCells;
		public Func<Map, IntVec3, bool> validator;
		public Map map;

		public override void GizmoUpdateOnMouseover()
		{
			base.GizmoUpdateOnMouseover();
			if (!highlightCells.NullOrEmpty())
			{
				Color highlightColor = highlightCells.Any(c => validator != null && !validator(map, c)) ? Color.red : Color.white;
				GenDraw.DrawFieldEdges(highlightCells, highlightColor);
			}
		}
	}
}
