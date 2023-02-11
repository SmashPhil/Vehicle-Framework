using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Command_ActionVehicleDrawn : Command_Action
	{
		public VehicleBuildDef buildDef;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			GizmoResult result = VehicleGUI.GizmoOnGUIWithMaterial(this, new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f), buildDef);
			if (buildDef.MadeFromStuff)
			{
				Designator_Dropdown.DrawExtraOptionsIcon(topLeft, GetWidth(maxWidth));
			}
			return result;
		}
	}
}
