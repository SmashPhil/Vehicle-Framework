using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class Gizmo_VehicleCaravanInfo : Gizmo
	{
		public VehicleCaravan caravan;

		public Gizmo_VehicleCaravanInfo(VehicleCaravan caravan)
		{
			this.caravan = caravan;
			Order = -100f;
		}

		public override float GetWidth(float maxWidth)
		{
			return Mathf.Min(520f, maxWidth);
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			if (!caravan.Spawned)
			{
				return new GizmoResult(GizmoState.Clear);
			}
			Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
			Widgets.DrawWindowBackground(rect);
			Widgets.BeginGroup(rect);
			{
				int? ticksToArrive = caravan.vehiclePather.Moving ? new int?(VehicleCaravanPathingHelper.EstimatedTicksToArrive(caravan, true)) : null;
				StringBuilder stringBuilder = new StringBuilder();
				float tilesPerDay = VehicleCaravanTicksPerMoveUtility.ApproxTilesPerDay(caravan, stringBuilder);
				DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(caravan.MassUsage, caravan.MassCapacity, caravan.MassCapacityExplanation, tilesPerDay, 
					stringBuilder.ToString(), caravan.DaysWorthOfFood, caravan.forage.ForagedFoodPerDay, 
					caravan.forage.ForagedFoodPerDayExplanation, caravan.Visibility, caravan.VisibilityExplanation), info2: null, caravan.Tile, ticksToArrive, lastMassFlashTime: -9999f,
					rect.AtZero(), multiline: true);
			}
			Widgets.EndGroup();
			GenUI.AbsorbClicksInRect(rect);
			return new GizmoResult(GizmoState.Clear);
		}

		public static void DrawCaravanInfo(CaravanUIUtility.CaravanInfo info, CaravanUIUtility.CaravanInfo? info2, int currentTile, int? ticksToArrive, float lastMassFlashTime, Rect rect, bool lerpMassColor = true, string extraDaysWorthOfFoodTipInfo = null, bool multiline = false)
		{
			/*
			CaravanUIUtility.tmpInfo.Clear();
			TaggedString taggedString = $"{info.massUsage.ToStringEnsureThreshold(info.massCapacity, 0)} / {info.massCapacity:F0} {"kg".Translate()}";
			
			TaggedString taggedString2 = null;
			if (info2 != null)
			{
				taggedString2 = $"{info2.Value.massUsage.ToStringEnsureThreshold(info2.Value.massCapacity, 0)} / {info2.Value.massCapacity:F0} {"kg".Translate()}";
			}
			TaggedString taggedString3 = taggedString2;
			CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("Mass".Translate(), taggedString, CaravanUIUtility.GetMassColor(info.massUsage, info.massCapacity, lerpMassColor), CaravanUIUtility.GetMassTip(info.massUsage, info.massCapacity, info.massCapacityExplanation, (info2 != null) ? new float?(info2.Value.massUsage) : null, (info2 != null) ? new float?(info2.Value.massCapacity) : null, (info2 != null) ? info2.Value.massCapacityExplanation : null), taggedString3, (info2 != null) ? CaravanUIUtility.GetMassColor(info2.Value.massUsage, info2.Value.massCapacity, lerpMassColor) : Color.white, lastMassFlashTime));
			if (info.extraMassUsage != -1f)
			{
				TaggedString taggedString4 = info.extraMassUsage.ToStringEnsureThreshold(info.extraMassCapacity, 0) + " / " + info.extraMassCapacity.ToString("F0") + " " + "kg".Translate();
				TaggedString taggedString5;
				if (info2 == null)
				{
					taggedString5 = null;
				}
				else
				{
					string str3 = info2.Value.extraMassUsage.ToStringEnsureThreshold(info2.Value.extraMassCapacity, 0);
					string str4 = " / ";
					CaravanUIUtility.CaravanInfo value = info2.Value;
					taggedString5 = str3 + str4 + value.extraMassCapacity.ToString("F0") + " " + "kg".Translate();
				}
				TaggedString taggedString6 = taggedString5;
				CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("CaravanMass".Translate(), taggedString4, CaravanUIUtility.GetMassColor(info.extraMassUsage, info.extraMassCapacity, true), CaravanUIUtility.GetMassTip(info.extraMassUsage, info.extraMassCapacity, info.extraMassCapacityExplanation, (info2 != null) ? new float?(info2.Value.extraMassUsage) : null, (info2 != null) ? new float?(info2.Value.extraMassCapacity) : null, (info2 != null) ? info2.Value.extraMassCapacityExplanation : null), taggedString6, (info2 != null) ? CaravanUIUtility.GetMassColor(info2.Value.extraMassUsage, info2.Value.extraMassCapacity, true) : Color.white, -9999f));
			}
			string text = "CaravanMovementSpeedTip".Translate();
			if (!info.tilesPerDayExplanation.NullOrEmpty())
			{
				text = text + "\n\n" + info.tilesPerDayExplanation;
			}
			if (info2 != null && !info2.Value.tilesPerDayExplanation.NullOrEmpty())
			{
				text = text + "\n\n-----\n\n" + info2.Value.tilesPerDayExplanation;
			}
			CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("CaravanMovementSpeed".Translate(), CaravanUIUtility.GetMovementSpeedLabel(info.tilesPerDay, info.massUsage > info.massCapacity, info2 != null), CaravanUIUtility.GetMovementSpeedColor(info.tilesPerDay, info.massUsage > info.massCapacity), text, (info2 != null) ? CaravanUIUtility.GetMovementSpeedLabel(info2.Value.tilesPerDay, info2.Value.massUsage > info2.Value.massCapacity, true) : null, (info2 != null) ? CaravanUIUtility.GetMovementSpeedColor(info2.Value.tilesPerDay, info2.Value.massUsage > info2.Value.massCapacity) : Color.white, -9999f));
			CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("DaysWorthOfFood".Translate(), CaravanUIUtility.GetDaysWorthOfFoodLabel(info.daysWorthOfFood, multiline), CaravanUIUtility.GetDaysWorthOfFoodColor(info.daysWorthOfFood, ticksToArrive), "DaysWorthOfFoodTooltip".Translate() + extraDaysWorthOfFoodTipInfo + "\n\n" + VirtualPlantsUtility.GetVirtualPlantsStatusExplanationAt(currentTile, Find.TickManager.TicksAbs), (info2 != null) ? CaravanUIUtility.GetDaysWorthOfFoodLabel(info2.Value.daysWorthOfFood, multiline) : null, (info2 != null) ? CaravanUIUtility.GetDaysWorthOfFoodColor(info2.Value.daysWorthOfFood, ticksToArrive) : Color.white, -9999f));
			string text2 = info.foragedFoodPerDay.Second.ToString("0.#");
			string text3;
			if (info2 == null)
			{
				text3 = null;
			}
			else
			{
				CaravanUIUtility.CaravanInfo value = info2.Value;
				text3 = value.foragedFoodPerDay.Second.ToString("0.#");
			}
			string text4 = text3;
			TaggedString taggedString7 = "ForagedFoodPerDayTip".Translate();
			taggedString7 += "\n\n" + info.foragedFoodPerDayExplanation;
			if (info2 != null)
			{
				taggedString7 += "\n\n-----\n\n" + info2.Value.foragedFoodPerDayExplanation;
			}
			if (info.foragedFoodPerDay.Second <= 0f)
			{
				if (info2 == null)
				{
					goto IL_6A6;
				}
				CaravanUIUtility.CaravanInfo value = info2.Value;
				if (value.foragedFoodPerDay.Second <= 0f)
				{
					goto IL_6A6;
				}
			}
			string text5 = multiline ? "\n" : " ";
			if (info2 == null)
			{
				text2 = string.Concat(new string[]
				{
			text2,
			text5,
			"(",
			info.foragedFoodPerDay.First.label,
			")"
				});
			}
			else
			{
				string[] array = new string[5];
				array[0] = text4;
				array[1] = text5;
				array[2] = "(";
				int num = 3;
				CaravanUIUtility.CaravanInfo value = info2.Value;
				array[num] = value.foragedFoodPerDay.First.label.Truncate(50f, null);
				array[4] = ")";
				text4 = string.Concat(array);
			}
		IL_6A6:
			CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("ForagedFoodPerDay".Translate(), text2, Color.white, taggedString7, text4, Color.white, -9999f));
			string text6 = "CaravanVisibilityTip".Translate();
			if (!info.visibilityExplanation.NullOrEmpty())
			{
				text6 = text6 + "\n\n" + info.visibilityExplanation;
			}
			if (info2 != null && !info2.Value.visibilityExplanation.NullOrEmpty())
			{
				text6 = text6 + "\n\n-----\n\n" + info2.Value.visibilityExplanation;
			}
			CaravanUIUtility.tmpInfo.Add(new TransferableUIUtility.ExtraInfo("Visibility".Translate(), info.visibility.ToStringPercent(), GenUI.LerpColor(CaravanUIUtility.VisibilityColor, info.visibility), text6, (info2 != null) ? info2.Value.visibility.ToStringPercent() : null, (info2 != null) ? GenUI.LerpColor(CaravanUIUtility.VisibilityColor, info2.Value.visibility) : Color.white, -9999f));
			TransferableUIUtility.DrawExtraInfo(CaravanUIUtility.tmpInfo, rect);
			*/
		}
	}
}
