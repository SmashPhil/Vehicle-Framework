using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles.World;

// ReSharper disable once InconsistentNaming
// It's cleaner to just stick with RimWorld's naming convention when deriving from their types.
[StaticConstructorOnStartup]
public class WITab_AerialVehicle_Health : WITab_AerialVehicle
{
	private const float RowHeight = 40f;
	private const float PawnLabelHeight = 18f;
	private const float PawnLabelColumnWidth = 100f;
	private const float SpaceAroundIcon = 4f;
	private const float PawnCapacityColumnWidth = 100f;
	private const float BeCarriedIfSickColumnWidth = 40f;
	private const float IconSize = 24f;

	private static readonly List<PawnCapacityDef> CapacitiesToDisplayTmp = [];

	private Vector2 scrollPosition;
	private float scrollViewHeight;
	private Pawn specificHealthTabForPawn;
	private bool compactMode;

	public WITab_AerialVehicle_Health()
	{
		labelKey = "TabCaravanHealth";
	}

	private static List<PawnCapacityDef> CapacitiesToDisplay
	{
		get
		{
			CapacitiesToDisplayTmp.Clear();
			foreach (PawnCapacityDef pawnCapacityDef in DefDatabase<PawnCapacityDef>.AllDefsListForReading)
			{
				if (pawnCapacityDef.showOnCaravanHealthTab)
				{
					CapacitiesToDisplayTmp.Add(pawnCapacityDef);
				}
			}
			CapacitiesToDisplayTmp.SortBy(CapacityDefOrder);
			return CapacitiesToDisplayTmp;

			static int CapacityDefOrder(PawnCapacityDef capacityDef)
			{
				return capacityDef.listOrder;
			}
		}
	}

	private float SpecificHealthTabWidth
	{
		get
		{
			EnsureSpecificHealthTabForPawnValid();
			return specificHealthTabForPawn.DestroyedOrNull() ? 0 : 630f;
		}
	}

	protected override void CloseTab()
	{
		base.CloseTab();
		VehicleTabHelper_Health.Clear();
	}

	protected override void FillTab()
	{
		EnsureSpecificHealthTabForPawnValid();
		Text.Font = GameFont.Small;
		Rect outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
		Rect viewRect = new(0f, 0f, outRect.width - GenUI.ScrollBarWidth, scrollViewHeight);
		float curY = 0f;
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		DoColumnHeaders();
		DoRows(ref curY, viewRect, outRect);

		if (Event.current.type == EventType.Layout)
		{
			scrollViewHeight = curY + 30f;
		}
		Widgets.EndScrollView();
	}

	protected override void UpdateSize()
	{
		EnsureSpecificHealthTabForPawnValid();
		base.UpdateSize();
		size = GetRawSize(false);
		if (size.x + SpecificHealthTabWidth > UI.screenWidth)
		{
			compactMode = true;
			size = GetRawSize(true);
			return;
		}
		compactMode = false;
	}

	protected override void ExtraOnGUI()
	{
		EnsureSpecificHealthTabForPawnValid();
		base.ExtraOnGUI();
		Pawn localSpecificHealthTabForPawn = specificHealthTabForPawn;
		if (localSpecificHealthTabForPawn != null)
		{
			Rect tabRect = TabRect;
			float specificHealthTabWidth = SpecificHealthTabWidth;
			Rect rect = new(tabRect.xMax - 1f, tabRect.yMin, specificHealthTabWidth, tabRect.height);
			Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
			{
				if (localSpecificHealthTabForPawn.DestroyedOrNull())
				{
					return;
				}
				HealthCardUtility.DrawPawnHealthCard(new Rect(Vector2.zero, rect.size),
					localSpecificHealthTabForPawn, false, true, localSpecificHealthTabForPawn);
				if (Widgets.CloseButtonFor(rect.AtZero()))
				{
					specificHealthTabForPawn = null;
					SoundDefOf.TabClose.PlayOneShotOnCamera();
				}
			});
		}
	}

	private void DoColumnHeaders()
	{
		if (!compactMode)
		{
			float num = 135f;
			using TextBlock headerBlock = new(TextAnchor.UpperCenter, Widgets.SeparatorLabelColor);
			Widgets.Label(new Rect(num, 3f, PawnLabelColumnWidth, PawnLabelColumnWidth),
				"Pain".Translate());
			num += PawnLabelColumnWidth;
			foreach (PawnCapacityDef pawnCapacityDef in CapacitiesToDisplay)
			{
				Widgets.Label(new Rect(num, 3f, PawnLabelColumnWidth, PawnLabelColumnWidth),
					pawnCapacityDef.LabelCap.Truncate(PawnLabelColumnWidth));
				num += PawnLabelColumnWidth;
			}
		}
	}

	private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
	{
		List<Pawn> pawns = Pawns;
		if (specificHealthTabForPawn != null && !pawns.Contains(specificHealthTabForPawn))
		{
			specificHealthTabForPawn = null;
		}
		bool separatorDrawn = false;
		foreach (Pawn pawn in pawns)
		{
			if (pawn.IsColonist)
			{
				if (!separatorDrawn)
				{
					Widgets.ListSeparator(ref curY, scrollViewRect.width, "CaravanColonists".Translate());
					separatorDrawn = true;
				}
				DoRow(ref curY, scrollViewRect, scrollOutRect, pawn);
			}
		}
		bool miscSeparatorDrawn = false;
		foreach (Pawn pawn in pawns)
		{
			if (!pawn.IsColonist)
			{
				if (!miscSeparatorDrawn)
				{
					Widgets.ListSeparator(ref curY, scrollViewRect.width,
						ModsConfig.BiotechActive ?
							"CaravanPrisonersAnimalsAndMechs".Translate() :
							"CaravanPrisonersAndAnimals".Translate());
					miscSeparatorDrawn = true;
				}
				DoRow(ref curY, scrollViewRect, scrollOutRect, pawn);
			}
		}
	}

	private Vector2 GetRawSize(bool compactMode)
	{
		float width = PawnCapacityColumnWidth;
		if (!compactMode)
		{
			width += PawnCapacityColumnWidth;
			width += CapacitiesToDisplay.Count * PawnCapacityColumnWidth;
			width += RowHeight;
		}
		Vector2 result;
		result.x = 127f + width + GenUI.ScrollBarWidth;
		result.y = Mathf.Min(550f, PaneTopY - 30f);
		return result;
	}

	private void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Pawn p)
	{
		float num = scrollPosition.y - RowHeight;
		float num2 = scrollPosition.y + scrollOutRect.height;
		if (curY > num && curY < num2)
		{
			DoRow(new Rect(0f, curY, viewRect.width, RowHeight), p);
		}
		curY += RowHeight;
	}

	private void DoRow(Rect rect, Pawn p)
	{
		Widgets.BeginGroup(rect);
		Rect rect2 = rect.AtZero();
		AerialVehicleTabHelper.DoAbandonButton(rect2, p, SelAerialVehicle);
		rect2.width -= IconSize;
		Widgets.InfoCardButton(rect2.width - IconSize, (rect.height - IconSize) / 2f, p);
		rect2.width -= IconSize;
		CaravanThingsTabUtility.DoOpenSpecificTabButton(rect2, p, ref specificHealthTabForPawn);
		rect2.width -= IconSize;
		if (Mouse.IsOver(rect2))
		{
			Widgets.DrawHighlight(rect2);
		}
		Rect rect3 = new(SpaceAroundIcon, (rect.height - 27f) / 2f, 27f, 27f);
		Widgets.ThingIcon(rect3, p);
		Rect bgRect = new(rect3.xMax + SpaceAroundIcon, 11f, PawnLabelColumnWidth,
			PawnLabelHeight);
		GenMapUI.DrawPawnLabel(p, bgRect, 1f, PawnLabelColumnWidth, null, GameFont.Small, false, false);
		float num = bgRect.xMax;
		if (!compactMode)
		{
			if (p.RaceProps.IsFlesh)
			{
				Rect rect4 = new(num, 0f, PawnLabelColumnWidth, RowHeight);
				DoPain(rect4, p);
			}
			num += PawnLabelColumnWidth;
			foreach (PawnCapacityDef pawnCapacityDef in CapacitiesToDisplay)
			{
				Rect rect5 = new(num, 0f, PawnCapacityColumnWidth, RowHeight);
				if ((p.RaceProps.Humanlike && !pawnCapacityDef.showOnHumanlikes) ||
					(p.RaceProps.Animal && !pawnCapacityDef.showOnAnimals) ||
					(p.RaceProps.IsMechanoid && !pawnCapacityDef.showOnMechanoids) ||
					!PawnCapacityUtility.BodyCanEverDoCapacity(p.RaceProps.body, pawnCapacityDef))
				{
					num += PawnCapacityColumnWidth;
				}
				else
				{
					DoCapacity(rect5, p, pawnCapacityDef);
					num += PawnCapacityColumnWidth;
				}
			}
		}
		if (p.Downed && !p.ageTracker.CurLifeStage.alwaysDowned)
		{
			using TextBlock colorBlock = new(new Color(1f, 0f, 0f, 0.5f));
			Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
		}
		Widgets.EndGroup();
	}

	private void DoPain(Rect rect, Pawn pawn)
	{
		Pair<string, Color> painLabel = HealthCardUtility.GetPainLabel(pawn);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		GUI.color = painLabel.Second;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, painLabel.First);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		if (Mouse.IsOver(rect))
		{
			string painTip = HealthCardUtility.GetPainTip(pawn);
			TooltipHandler.TipRegion(rect, painTip);
		}
	}

	private void DoCapacity(Rect rect, Pawn pawn, PawnCapacityDef capacity)
	{
		Pair<string, Color> efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(pawn, capacity);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		GUI.color = efficiencyLabel.Second;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, efficiencyLabel.First);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		if (Mouse.IsOver(rect))
		{
			string pawnCapacityTip = HealthCardUtility.GetPawnCapacityTip(pawn, capacity);
			TooltipHandler.TipRegion(rect, pawnCapacityTip);
		}
	}

	public override void Notify_ClearingAllMapsMemory()
	{
		base.Notify_ClearingAllMapsMemory();
		specificHealthTabForPawn = null;
	}

	private void EnsureSpecificHealthTabForPawnValid()
	{
		if (specificHealthTabForPawn != null && (specificHealthTabForPawn.Destroyed ||
			!SelAerialVehicle.Vehicle.AllPawnsAboard.Contains(specificHealthTabForPawn)))
		{
			specificHealthTabForPawn = null;
		}
	}
}