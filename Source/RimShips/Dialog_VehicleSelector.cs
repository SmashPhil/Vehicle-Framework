using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class Dialog_VehicleSelector : Window
    {
        public Dialog_VehicleSelector()
        {
            absorbInputAroundWindow = true;
            doCloseX = true;
            forcePause = true;
            availableVehicles = Find.Maps.Select(m => m.mapPawns).SelectMany(v => v.AllPawnsSpawned.Where(p => p.IsVehicle())).Cast<VehiclePawn>().ToList();
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

                if(vehicle.GetComp<CompVehicle>().Props.generateThingIcon)
                {
                    Widgets.ThingIcon(iconRect, vehicle);
                }
                else
                {
                    Widgets.ButtonImageFitted(iconRect, TexCommandVehicles.CachedTextureIcons[vehicle.def]);
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
                if(storedVehicles.Any(v => v.GetComp<CompVehicle>().Props.vehicleType == VehicleType.Sea) && storedVehicles.Any(v => v.GetComp<CompVehicle>().Props.vehicleType == VehicleType.Land))
                {
                    Messages.Message("LandAndSeaRoutePlannerRestriction".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                Find.World.GetComponent<VehicleRoutePlanner>().vehicles = storedVehicles.ToList();
                Find.World.GetComponent<VehicleRoutePlanner>().InitiateRoutePlanner();
                Close();
            }
            Rect rect3 = new Rect(rect2.x - BottomButtonSize.x - ButtonPadding, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
            if(Widgets.ButtonText(rect3, "CancelButton".Translate()))
            {
                Find.World.GetComponent<VehicleRoutePlanner>().Stop();
                Close();
            }
        }

        private List<VehiclePawn> availableVehicles = new List<VehiclePawn>();

        private HashSet<VehiclePawn> storedVehicles = new HashSet<VehiclePawn>();

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private Vector2 scrollPosition = new Vector2();

        private static float ButtonPadding = 10f;
    }
}
