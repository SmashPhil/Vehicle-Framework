using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles.Rendering;
using Verse;

namespace Vehicles.World;

public class Dialog_VehicleSelector : Window
{
	private const int Columns = 4; // TODO VF-194 - Rework this dialog
	private const float ButtonPadding = 10f;
	private const float RowHeight = 90;

	private static readonly Vector2 BottomButtonSize = new(160f, 40f);

	private readonly List<VehiclePawn> availableVehicles;
	private readonly List<VehicleDef> availableVehicleDefs;

	private readonly HashSet<VehicleDef> selectedDefs = [];
	private readonly HashSet<VehiclePawn> selectedVehicles = [];

	private Vector2 scrollPosition;
	private bool showVehicleDefs;

	public Dialog_VehicleSelector()
	{
		absorbInputAroundWindow = true;
		doCloseX = true;
		forcePause = true;
		availableVehicles = Find.Maps.Select(map => map.GetDetachedMapComponent<VehiclePositionManager>())
		 .SelectMany(positionManager => positionManager.AllClaimants.Where(vehicle => vehicle.VehicleDef.canCaravan))
		 .ToList();
		availableVehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading
		 .Where(vehicleDef => vehicleDef.canCaravan)
		 .ToList();
		RecalculateHeight();
	}

	public override Vector2 InitialSize => new(UI.screenWidth / 2f, UI.screenHeight / 1.5f);

	private VehicleType SelectedType => showVehicleDefs ?
		selectedDefs.FirstOrDefault()?.type ?? VehicleType.Universal :
		selectedVehicles.FirstOrDefault()?.VehicleDef.type ?? VehicleType.Universal;

	private float ViewRectHeight { get; set; }

	private void RecalculateHeight()
	{
		int count = showVehicleDefs ? availableVehicleDefs.Count : availableVehicles.Count;
		ViewRectHeight = Mathf.CeilToInt((float)count / Columns) * RowHeight;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect labelRect = new(0f, 0f, inRect.width, 35f);
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(labelRect, "VF_SelectVehiclesForPlanner".Translate());

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;

		Rect toggleRect = new(labelRect);
		if (UIElements.ClickableLabel(toggleRect,
			showVehicleDefs ?
				"VF_RoutePlannerToggleVehicleDefs".Translate() :
				"VF_RoutePlannerToggleVehicles".Translate(), Color.grey, Color.white))
		{
			showVehicleDefs = !showVehicleDefs;
			selectedDefs.Clear();
			selectedVehicles.Clear();
			RecalculateHeight();
		}

		inRect.yMin += labelRect.height;
		Widgets.DrawMenuSection(inRect);

		Rect innerRect = new Rect(inRect).ContractedBy(1);
		innerRect.yMax = inRect.height - ButtonPadding - 5;
		DrawVehicleSelect(innerRect);

		Rect rectBottom = inRect.AtZero();
		DoBottomButtons(rectBottom);
	}

	private void DrawVehicleSelect(Rect rect)
	{
		using TextBlock anchorBlock = new(TextAnchor.MiddleLeft);

		Rect viewRect = new(0f, rect.yMin, rect.width - 16, ViewRectHeight);
		Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
		DrawVehicles(rect);
		Widgets.EndScrollView();
	}

	private void DrawVehicles(Rect rect)
	{
		const float IconPadding = 5;
		const float SliderPadding = 16;
		const float TopPadding = 30;

		int vehicleCount = showVehicleDefs ? availableVehicleDefs.Count : availableVehicles.Count;

		float rowWidth = rect.width - SliderPadding - IconPadding;
		float columnWidth = rowWidth / Columns;
		float curY = TopPadding;
		int curVehicle = 0;
		while (curVehicle < vehicleCount)
		{
			float curX = IconPadding;
			Rect rowRect = new(curX, curY, rowWidth, RowHeight);
			for (int j = 0; j < Columns && curVehicle < vehicleCount; j++)
			{
				Rect iconRect = new(curX, rowRect.y + IconPadding, RowHeight, RowHeight);
				Rect labelRect = new(iconRect.xMax, iconRect.y, columnWidth - iconRect.width, RowHeight);
				labelRect.xMin += IconPadding;
				if (showVehicleDefs)
				{
					VehicleDef vehicleDef = availableVehicleDefs[curVehicle];
					DrawVehicleDef(labelRect, iconRect, vehicleDef);
				}
				else
				{
					VehiclePawn vehicle = availableVehicles[curVehicle];
					DrawVehicle(labelRect, iconRect, vehicle);
				}
				curX = labelRect.xMax;
				curVehicle++;
			}
			curY += RowHeight;
		}
	}

	private void DrawVehicle(Rect rowRect, Rect iconRect, VehiclePawn vehicle)
	{
		const float CheckboxSize = 24;

		Vector2 texProportions = vehicle.VehicleDef.graphicData.drawSize;
		(texProportions.x, texProportions.y) = (texProportions.y, texProportions.x);
		VehicleGui.DrawVehicleOnGUI(iconRect, BlitRequest.For(vehicle));
		Widgets.Label(rowRect, vehicle.LabelShortCap);

		bool checkOn = selectedVehicles.Contains(vehicle);
		Vector2 checkposition = new(rowRect.xMax - CheckboxSize, rowRect.y + 5f);
		bool disabled = SelectedType != VehicleType.Universal && vehicle.VehicleDef.type != SelectedType;
		Widgets.Checkbox(checkposition, ref checkOn, disabled: disabled);
		if (checkOn != selectedVehicles.Contains(vehicle))
		{
			if (checkOn)
				selectedVehicles.Add(vehicle);
			else
				selectedVehicles.Remove(vehicle);
		}
	}

	private void DrawVehicleDef(Rect rowRect, Rect iconRect, VehicleDef vehicleDef)
	{
		const float CheckboxSize = 24;

		Vector2 texProportions = vehicleDef.graphicData.drawSize;
		(texProportions.x, texProportions.y) = (texProportions.y, texProportions.x);
		VehicleGui.DrawVehicleOnGUI(iconRect, BlitRequest.For(vehicleDef));
		Widgets.Label(rowRect, vehicleDef.LabelCap);

		bool checkOn = selectedDefs.Contains(vehicleDef);
		Vector2 checkposition = new(rowRect.xMax - CheckboxSize, rowRect.y + 5f);
		bool disabled = SelectedType != VehicleType.Universal && vehicleDef.type != SelectedType;
		Widgets.Checkbox(checkposition, ref checkOn, disabled: disabled);
		if (checkOn != selectedDefs.Contains(vehicleDef))
		{
			if (checkOn)
				selectedDefs.Add(vehicleDef);
			else
				selectedDefs.Remove(vehicleDef);
		}
	}

	private void DoBottomButtons(Rect rect)
	{
		Rect buttonRect = new(rect.width - BottomButtonSize.x - ButtonPadding - 15f, rect.height - ButtonPadding,
			BottomButtonSize.x, BottomButtonSize.y);

		if (Widgets.ButtonText(buttonRect, "VF_StartVehicleRoutePlanner".Translate()))
		{
			VehicleRoutePlanner planner = Find.World.GetComponent<VehicleRoutePlanner>();

			if (showVehicleDefs)
			{
				planner.Start(selectedDefs.ToList());
			}
			else
			{
				List<Pawn> vehicles = selectedVehicles.Cast<Pawn>().ToList();
				planner.Start(new VehicleCaravanInfo(vehicles,
					CollectionsMassCalculator.MassUsage(vehicles, IgnorePawnsInventoryMode.DontIgnore),
					selectedVehicles.Sum(vehicle => vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity)), PlanetTile.Invalid));
			}
			Close();
		}
		Rect cancelBtnRect = new(buttonRect.x - BottomButtonSize.x - ButtonPadding, buttonRect.y,
			BottomButtonSize.x, BottomButtonSize.y);
		if (Widgets.ButtonText(cancelBtnRect, "CancelButton".Translate()))
		{
			Find.World.GetComponent<VehicleRoutePlanner>().Stop();
			Close();
		}
	}
}