using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Command_ActionHighlighter : Command_Action
	{
		public Action mouseOver;

		public override void GizmoUpdateOnMouseover()
		{
			base.GizmoUpdateOnMouseover();
			if (mouseOver != null)
			{
				mouseOver();
			}
		}
	}
}
