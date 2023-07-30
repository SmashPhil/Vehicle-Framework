using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Dialog_VehicleSelector : Window
	{
		private List<VehiclePawn> availableVehicles = new List<VehiclePawn>();
		private List<VehicleDef> availableVehicleDefs = new List<VehicleDef>();

		private HashSet<VehicleDef> storedVehicleDefs = new HashSet<VehicleDef>();

		private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private Vector2 scrollPosition = new Vector2();

		private static float ButtonPadding = 10f;

		private static bool showVehicleDefs = Prefs.DevMode;

		public Dialog_VehicleSelector()
		{
			absorbInputAroundWindow = true;
			doCloseX = true;
			forcePause = true;
			availableVehicles = Find.Maps.Select(m => m.mapPawns).SelectMany(v => v.AllPawnsSpawned.Where(p => p is VehiclePawn vehicle && vehicle.VehicleDef.vehicleType != VehicleType.Air)).Cast<VehiclePawn>().ToList();
			availableVehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading;
		}
		
		public override Vector2 InitialSize => new Vector2(Verse.UI.screenWidth / 2, Verse.UI.screenHeight/1.5f);

		public override void DoWindowContents(Rect inRect)
		{
			Rect labelRect = new Rect(0f, 0f, inRect.width, 35f);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(labelRect, "VF_SelectVehiclesForPlanner".Translate());

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;

			Rect toggleRect = new Rect(labelRect);
			if (UIElements.ClickableLabel(toggleRect, showVehicleDefs ? "VF_RoutePlannerToggleVehicleDefs".Translate() : "VF_RoutePlannerToggleVehicles".Translate(), Color.grey, Color.white))
			{
				showVehicleDefs = !showVehicleDefs;
			}

			inRect.yMin += labelRect.height;
			Widgets.DrawMenuSection(inRect);

			Rect windowRect = new Rect(inRect).ContractedBy(1);
			windowRect.yMax = inRect.height - ButtonPadding - 5;
			DrawVehicleSelect(windowRect);

			Rect rectBottom = inRect.AtZero();
			DoBottomButtons(rectBottom);
		}

		private void DrawVehicleSelect(Rect rect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;

			Rect viewRect = new Rect(0f, rect.yMin, rect.width - ButtonPadding*2, rect.yMax);
			
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			if (showVehicleDefs)
			{
				DrawVehicleDefs(rect);
			}
			else
			{
				DrawVehicles(rect);
			}

			Widgets.EndScrollView();
			Text.Anchor = TextAnchor.UpperLeft;
		}

		private void DrawVehicles(Rect rect)
		{
			float num3 = 30f;
			for (int i = 0; i < availableVehicles.Count; i++)
			{
				VehiclePawn vehicle = availableVehicles[i];

				Rect iconRect = new Rect(5f, num3 + 5f, 30f, 30f);
				Rect rowRect = new Rect(iconRect.x, iconRect.y, rect.width, 30f);

				if (i % 2 == 1)
				{
					Widgets.DrawLightHighlight(rowRect);
				}
				rowRect.x = iconRect.width + 10f;

				if (vehicle.VehicleDef.properties.generateThingIcon)
				{
					Rect texCoords = new Rect(0, 0, 1, 1);
					Vector2 texProportions = vehicle.VehicleDef.graphicData.drawSize;
					float x = texProportions.x;
					texProportions.x = texProportions.y;
					texProportions.y = x;
					VehicleGUI.DrawVehicleDefOnGUI(iconRect, vehicle.VehicleDef);
				}
				else
				{
					Widgets.ButtonImageFitted(iconRect, VehicleTex.CachedTextureIcons[vehicle.VehicleDef]);
				}

				Widgets.Label(rowRect, vehicle.LabelShortCap);

				bool flag = storedVehicleDefs.Contains(vehicle.VehicleDef);

				Vector2 checkposition = new Vector2(rect.width - iconRect.width * 1.5f, rowRect.y + 5f);
				Widgets.Checkbox(checkposition, ref flag);
				if (flag && !storedVehicleDefs.Contains(vehicle.VehicleDef))
				{
					storedVehicleDefs.Add(vehicle.VehicleDef);
				}
				else if (!flag && storedVehicleDefs.Contains(vehicle.VehicleDef))
				{
					storedVehicleDefs.Remove(vehicle.VehicleDef);
				}

				num3 += rowRect.height;
			}
		}

		private void DrawVehicleDefs(Rect rect)
		{
			float num3 = 30f;
			for (int i = 0; i < availableVehicleDefs.Count; i++)
			{
				VehicleDef vehicleDef = availableVehicleDefs[i];

				Rect iconRect = new Rect(5f, num3 + 5f, 30f, 30f);
				Rect rowRect = new Rect(iconRect.x, iconRect.y, rect.width, 30f);

				if (i % 2 == 1)
				{
					Widgets.DrawLightHighlight(rowRect);
				}
				rowRect.x = iconRect.width + 10f;

				if (vehicleDef.properties.generateThingIcon)
				{
					Rect texCoords = new Rect(0, 0, 1, 1);
					Vector2 texProportions = vehicleDef.graphicData.drawSize;
					float x = texProportions.x;
					texProportions.x = texProportions.y;
					texProportions.y = x;
					VehicleGUI.DrawVehicleDefOnGUI(iconRect, vehicleDef);
				}
				else
				{
					Widgets.ButtonImageFitted(iconRect, VehicleTex.CachedTextureIcons[vehicleDef]);
				}

				Widgets.Label(rowRect, vehicleDef.defName);

				bool flag = storedVehicleDefs.Contains(vehicleDef);

				Vector2 checkposition = new Vector2(rect.width - iconRect.width * 1.5f, rowRect.y + 5f);
				Widgets.Checkbox(checkposition, ref flag);
				if (flag && !storedVehicleDefs.Contains(vehicleDef))
				{
					storedVehicleDefs.Add(vehicleDef);
				}
				else if (!flag && storedVehicleDefs.Contains(vehicleDef))
				{
					storedVehicleDefs.Remove(vehicleDef);
				}

				num3 += rowRect.height;
			}
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect rect2 = new Rect(rect.width - BottomButtonSize.x - ButtonPadding - 15f, rect.height - ButtonPadding, BottomButtonSize.x, BottomButtonSize.y);

			if (Widgets.ButtonText(rect2, "VF_StartVehicleRoutePlanner".Translate()))
			{
				if (storedVehicleDefs.NotNullAndAny(v => v.vehicleType == VehicleType.Sea) && storedVehicleDefs.NotNullAndAny(v => v.vehicleType == VehicleType.Land))
				{
					Messages.Message("VF_LandAndSeaRoutePlannerRestriction".Translate(), MessageTypeDefOf.RejectInput);
					return;
				}

				VehicleRoutePlanner.Instance.vehicleDefs = storedVehicleDefs.ToList();
				VehicleRoutePlanner.Instance.InitiateRoutePlanner();
				Close();
			}
			Rect rect3 = new Rect(rect2.x - BottomButtonSize.x - ButtonPadding, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
			if(Widgets.ButtonText(rect3, "CancelButton".Translate()))
			{
				VehicleRoutePlanner.Instance.Stop();
				Close();
			}
		}
	}
}
