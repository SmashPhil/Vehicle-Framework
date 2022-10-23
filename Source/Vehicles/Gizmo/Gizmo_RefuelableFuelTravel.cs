using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{    
	[StaticConstructorOnStartup]
	public class Gizmo_RefuelableFuelTravel : Gizmo
	{
		private const float ArrowScale = 0.5f;

		public CompFueledTravel refuelable;

		public Gizmo_RefuelableFuelTravel()
		{
			Order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect overRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
			Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
			{
				Rect rect2;
				Rect rect = rect2 = overRect.AtZero().ContractedBy(6f);
				rect2.height = overRect.height / 2f;
				Text.Font = GameFont.Tiny;
				Widgets.Label(rect2, refuelable.Props.electricPowered ? "VehicleElectric".Translate() : refuelable.Props.fuelType.LabelCap);
				Rect rect3 = rect;
				rect3.yMin = overRect.height / 2f;
				float fillPercent = refuelable.Fuel / refuelable.FuelCapacity;
				Widgets.FillableBar(rect3, fillPercent, VehicleTex.FullBarTex, VehicleTex.EmptyBarTex, false);
				/*if (this.refuelable.Props.targetFuelLevelConfigurable)
				{
					float num = this.refuelable.TargetFuelLevel / this.refuelable.FuelCapacity;
					float num2 = rect3.x + num * rect3.width - (float)TargetLevelArrow.width * ArrowScale / 2f;
					float num3 = rect3.y - (float)TargetLevelArrow.height * ArrowScale;
					GUI.DrawTexture(new Rect(num2, num3, (float)TargetLevelArrow.width * ArrowScale, (float)TargetLevelArrow.height * ArrowScale), TargetLevelArrow);
				}*/
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, refuelable.Fuel.ToString("F0") + " / " + refuelable.FuelCapacity.ToString("F0"));
				Text.Anchor = 0;
			}, true, false, 1f);
			return new GizmoResult(GizmoState.Clear);
		}
	}
}
