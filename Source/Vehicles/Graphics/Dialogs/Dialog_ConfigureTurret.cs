using System;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public class Dialog_ConfigureTurret : Window
{
	private const float RowHeight = 24;
	private const float RowPadding = 5;

	private static readonly Vector2 ButtonSize = new(160, 40);

	private readonly QuickSearchWidget filter = new();

	private readonly VehicleTurret turret;
	private Vector2 scrollPosition;

	public Dialog_ConfigureTurret(VehicleTurret turret)
	{
		this.turret = turret;
		resizeable = true;
		draggable = true;
	}

	private float ListHeight { get; set; }

	public override Vector2 InitialSize => new(300, 400);

	private void RecacheSettings()
	{
		ListHeight = 0;

		if (turret.def.ammunition == null)
			return;

		ListHeight += turret.def.ammunition.AllowedThingDefs.Count() * (RowHeight + RowPadding);
	}

	public override void DoWindowContents(Rect inRect)
	{
		const float SectionPadding = 5;

		Assert.IsNotNull(turret);

		using TextBlock tb = new(GameFont.Small, TextAnchor.MiddleLeft);
		if (turret.def.ammunition == null)
		{
			DrawSimpleSlider(inRect);
			return;
		}

		Rect filterRect = new(inRect.position, size: QuickSearchSize);
		filter.OnGUI(filterRect);

		Rect buttonBarRect = new(inRect.x, filterRect.yMax + SectionPadding, inRect.width, ButtonSize.y);

		Rect outRect = inRect with { yMin = buttonBarRect.yMax + SectionPadding };
		Rect viewRect = outRect with { height = ListHeight, width = outRect.width - GenUI.ScrollBarWidth };
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

		Rect entryRect = viewRect with { height = RowHeight };
		foreach (ThingDef thingDef in turret.def.ammunition.AllowedThingDefs)
		{
			Rect rowRect = entryRect;
			bool enabled = turret.loadConfig.IsEnabled(thingDef);
			bool checkOn = enabled;
			Rect cbRect = rowRect with { width = rowRect.height };
			UIElements.CheckboxButton(cbRect, ref checkOn);
			if (checkOn != enabled)
			{
				turret.loadConfig.SetEnabled(thingDef, checkOn);
			}
			rowRect.xMin += UIElements.CheckboxSize;

			Rect iconRect = rowRect with { width = rowRect.height };
			Widgets.ThingIcon(iconRect, thingDef);
			TooltipHandler.TipRegion(iconRect, thingDef.LabelCap);

			rowRect.xMin += iconRect.width;

			int count = turret.loadConfig.Get(thingDef);
			int max = Mathf.RoundToInt(turret.vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) /
				thingDef.GetStatValueAbstract(StatDefOf.Mass));
			int newCount =
				UIElements.HorizontalSlider(rowRect, "VF_SetReloadLevel".Translate(count),
					count, min: 0, max: max);
			if (!Mathf.Approximately(count, newCount))
			{
				turret.loadConfig.Set(thingDef, newCount);
			}
			entryRect.y += RowHeight + RowPadding;
		}

		Widgets.EndScrollView();
	}

	private void DrawSimpleSlider(Rect rect)
	{
	}
}