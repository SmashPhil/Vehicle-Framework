using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimShips
{    
    [StaticConstructorOnStartup]
	public class Gizmo_RefuelableFuelTravel : Gizmo
	{
		public Gizmo_RefuelableFuelTravel()
		{
			this.order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
		{
			Rect overRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
			Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
			{
				Rect rect2;
				Rect rect = rect2 = overRect.AtZero().ContractedBy(6f);
				rect2.height = overRect.height / 2f;
				Text.Font = GameFont.Tiny;
				Widgets.Label(rect2, refuelable.Props.fuelType.LabelCap);
				Rect rect3 = rect;
				rect3.yMin = overRect.height / 2f;
				float fillPercent = refuelable.Fuel / refuelable.FuelCapacity;
				Widgets.FillableBar(rect3, fillPercent, FullBarTex, EmptyBarTex, false);
                /*if (this.refuelable.Props.targetFuelLevelConfigurable)
                {
                    float num = this.refuelable.TargetFuelLevel / this.refuelable.FuelCapacity;
                    float num2 = rect3.x + num * rect3.width - (float)TargetLevelArrow.width * 0.5f / 2f;
                    float num3 = rect3.y - (float)TargetLevelArrow.height * 0.5f;
                    GUI.DrawTexture(new Rect(num2, num3, (float)TargetLevelArrow.width * 0.5f, (float)TargetLevelArrow.height * 0.5f), TargetLevelArrow);
                }*/
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, this.refuelable.Fuel.ToString("F0") + " / " + this.refuelable.FuelCapacity.ToString("F0"));
				Text.Anchor = 0;
			}, true, false, 1f);
			return new GizmoResult(GizmoState.Clear);
		}

		// Token: 0x04002C21 RID: 11297
		public CompFueledTravel refuelable;

		// Token: 0x04002C22 RID: 11298
		private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

		// Token: 0x04002C23 RID: 11299
		private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

		// Token: 0x04002C24 RID: 11300
		private static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated", true);

		// Token: 0x04002C25 RID: 11301
		private const float ArrowScale = 0.5f;
	}
}
