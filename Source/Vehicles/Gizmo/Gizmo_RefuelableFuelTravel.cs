using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{    
	[StaticConstructorOnStartup]
	public class Gizmo_RefuelableFuelTravel : Gizmo
	{
		private const float ConfigureSize = 20;
		private const float ArrowSize = 14;

		private readonly CompFueledTravel refuelable;

		public Gizmo_RefuelableFuelTravel(CompFueledTravel refuelable)
		{
			this.refuelable = refuelable;
			Order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
			{
				GUIState.Push();
				{
					Rect rect = overRect.AtZero().ContractedBy(6f);
					Rect labelRect = new Rect(rect)
					{
						height = overRect.height / 2f
					};

					Text.Font = GameFont.Tiny;

					Widgets.Label(labelRect, refuelable.Props.electricPowered ? "VehicleElectric".Translate() : refuelable.Props.fuelType.LabelCap);

					Rect configureRect = new Rect(labelRect.xMax - ConfigureSize, labelRect.y, ConfigureSize, ConfigureSize);
					if (Widgets.ButtonImage(configureRect, VehicleTex.Settings))
					{
						ShowConfigureWindow();
					}
					rect.yMin = overRect.height / 2f;

					float fillPercent = refuelable.Fuel / refuelable.FuelCapacity;
					Widgets.FillableBar(rect, fillPercent, VehicleTex.FullBarTex, VehicleTex.EmptyBarTex, false);

					float num = refuelable.TargetFuelLevel / refuelable.FuelCapacity;
					float num2 = rect.x + num * rect.width - ArrowSize / 2;
					float num3 = rect.y - ArrowSize;
					GUI.DrawTexture(new Rect(num2, num3, ArrowSize, ArrowSize), UIElements.TargetLevelArrow);

					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.MiddleCenter;

					Widgets.Label(rect, refuelable.Fuel.ToString("F0") + " / " + refuelable.FuelCapacity.ToString("F0"));
				}
				GUIState.Pop();
			}, true, false, 1f);
			return new GizmoResult(GizmoState.Clear);
		}

		private void ShowConfigureWindow()
		{
			int min = 0;
			int max = Mathf.RoundToInt(refuelable.FuelCapacity);
			int startingValue = Mathf.RoundToInt(refuelable.TargetFuelLevel);

			Func<int, string> textGetter = (int x) => "SetTargetFuelLevel".Translate(x);

			Dialog_Slider dialog_Slider = new Dialog_Slider(textGetter, min, max, delegate (int value)
			{
				refuelable.TargetFuelLevel = value;
			}, startingValue);
			Find.WindowStack.Add(dialog_Slider);
		}
	}
}
