using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Command_Action_PawnDrawer : Command_Action
	{
		public Pawn pawn;

		public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
		{
			if (pawn.Dead)
			{
				return;
			}
			Rect iconRect = new Rect(rect);
			Vector2 size = ColonistBarColonistDrawer.PawnTextureSize * iconDrawScale;
			rect = new Rect(rect.x + rect.width / 2 - size.x / 2, rect.y + rect.height / 2 - size.y / 1.75f, size.x, size.y).ContractedBy(1f);
			GUI.DrawTexture(rect, PortraitsCache.Get(pawn, ColonistBarColonistDrawer.PawnTextureSize, Rot4.South, ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));
			Widgets.DrawTextureFitted(iconRect, VehicleTex.UnloadIcon, 1);
		}
	}
}
