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

		private HashSet<VehiclePawn> storedVehicles = new HashSet<VehiclePawn>();

		private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private Vector2 scrollPosition = new Vector2();

		private static float ButtonPadding = 10f;

		public Dialog_VehicleSelector()
		{
			absorbInputAroundWindow = true;
			doCloseX = true;
			forcePause = true;
			availableVehicles = Find.Maps.Select(m => m.mapPawns).SelectMany(v => v.AllPawnsSpawned.Where(p => p is VehiclePawn vehicle && vehicle.VehicleDef.vehicleType != VehicleType.Air)).Cast<VehiclePawn>().ToList();
		}
		
		public override Vector2 InitialSize => new Vector2(Verse.UI.screenWidth / 2, Verse.UI.screenHeight/1.5f);

		public override void DoWindowContents(Rect inRect)
		{
			Rect labelRect = new Rect(0f, 0f, inRect.width, 35f);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(labelRect, "SelectVehiclesForPlanner".Translate());

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;

			inRect.yMin += labelRect.height;
			Widgets.DrawMenuSection(inRect);

			Rect windowRect = new Rect(inRect);
			windowRect.yMax = inRect.height - BottomButtonSize.y - ButtonPadding;
			DrawVehicleSelect(inRect);

			Rect rectBottom = inRect.AtZero();
			DoBottomButtons(rectBottom);
		}

		private void DrawVehicleSelect(Rect rect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;

			Rect viewRect = new Rect(0f, rect.yMin, rect.width - ButtonPadding*2, rect.yMax);
			
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			float num = scrollPosition.y - 30f;
			float num2 = scrollPosition.y + rect.height;
			float num3 = 30f;

			for(int i = 0; i < availableVehicles.Count; i++)
			{
				VehiclePawn vehicle = availableVehicles[i];

				Rect iconRect = new Rect(5f, num3 + 5f, 30f, 30f);
				Rect rowRect = new Rect(iconRect.x, iconRect.y, rect.width, 30f);
				
				if(i % 2 == 1)
				{
					Widgets.DrawLightHighlight(rowRect);
				}
				rowRect.x = iconRect.width + 10f;

				if(vehicle.VehicleDef.properties.generateThingIcon)
				{
					Rect texCoords = new Rect(0, 0, 1, 1);
					Vector2 texProportions = vehicle.VehicleDef.graphicData.drawSize;
					float x = texProportions.x;
					texProportions.x = texProportions.y;
					texProportions.y = x;
					Widgets.DrawTextureFitted(iconRect, VehicleTex.VehicleTexture(vehicle.VehicleDef, Rot8.East), GenUI.IconDrawScale(vehicle.VehicleDef), texProportions, 
						texCoords, 0, vehicle.VehicleGraphic.MatAt(Rot8.East, vehicle.pattern));
				}
				else
				{
					Widgets.ButtonImageFitted(iconRect, VehicleTex.CachedTextureIcons[vehicle.VehicleDef]);
				}
				
				Widgets.Label(rowRect, vehicle.LabelShortCap);
 
				bool flag = storedVehicles.Contains(vehicle);

				Vector2 checkposition = new Vector2(rect.width - iconRect.width * 1.5f, rowRect.y + 5f);
				Widgets.Checkbox(checkposition, ref flag);
				if(flag && !storedVehicles.Contains(vehicle))
				{
					storedVehicles.Add(vehicle);
				}
				else if(!flag && storedVehicles.Contains(vehicle))
				{
					storedVehicles.Remove(vehicle);
				}  

				num3 += rowRect.height;
			}

			Widgets.EndScrollView();
			Text.Anchor = TextAnchor.UpperLeft;
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect rect2 = new Rect(rect.width - BottomButtonSize.x - ButtonPadding - 15f, rect.height - ButtonPadding, BottomButtonSize.x, BottomButtonSize.y);

			if(Widgets.ButtonText(rect2, "StartVehicleRoutePlanner".Translate()))
			{
				if(storedVehicles.NotNullAndAny(v => v.VehicleDef.vehicleType == VehicleType.Sea) && storedVehicles.NotNullAndAny(v => v.VehicleDef.vehicleType == VehicleType.Land))
				{
					Messages.Message("LandAndSeaRoutePlannerRestriction".Translate(), MessageTypeDefOf.RejectInput);
					return;
				}

				VehicleRoutePlanner.Instance.vehicles = storedVehicles.ToList();
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
