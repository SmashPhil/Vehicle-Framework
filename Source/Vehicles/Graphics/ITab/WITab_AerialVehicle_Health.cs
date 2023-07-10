using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
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

		private Vector2 scrollPosition;
		private float scrollViewHeight;
		private Pawn specificHealthTabForPawn;
		private bool compactMode;

		private static List<PawnCapacityDef> capacitiesToDisplay = new List<PawnCapacityDef>();
		
		//private static readonly Texture2D BeCarriedIfSickIcon = ContentFinder<Texture2D>.Get("UI/Icons/CarrySick");

		public WITab_AerialVehicle_Health()
		{
			labelKey = "TabCaravanHealth";
		}

		private List<PawnCapacityDef> CapacitiesToDisplay
		{
			get
			{
				capacitiesToDisplay.Clear();
				List<PawnCapacityDef> allDefsListForReading = DefDatabase<PawnCapacityDef>.AllDefsListForReading;
				for (int i = 0; i < allDefsListForReading.Count; i++)
				{
					if (allDefsListForReading[i].showOnCaravanHealthTab)
					{
						capacitiesToDisplay.Add(allDefsListForReading[i]);
					}
				}
				capacitiesToDisplay.SortBy((PawnCapacityDef x) => x.listOrder);
				return capacitiesToDisplay;
			}
		}

		private float SpecificHealthTabWidth
		{
			get
			{
				EnsureSpecificHealthTabForPawnValid();
				if (specificHealthTabForPawn.DestroyedOrNull())
				{
					return 0f;
				}
				return 630f;
			}
		}

		protected override void FillTab()
		{
			EnsureSpecificHealthTabForPawnValid();
			Text.Font = GameFont.Small;
			Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
			Rect rect2 = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
			float num = 0f;
			Widgets.BeginScrollView(rect, ref scrollPosition, rect2, true);
			DoColumnHeaders(ref num);
			DoRows(ref num, rect2, rect);
			if (Event.current.type == EventType.Layout)
			{
				scrollViewHeight = num + 30f;
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
				Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificHealthTabWidth, tabRect.height);
				Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
				{
					if (localSpecificHealthTabForPawn.DestroyedOrNull())
					{
						return;
					}
					HealthCardUtility.DrawPawnHealthCard(new Rect(Vector2.zero, rect.size), localSpecificHealthTabForPawn, false, true, localSpecificHealthTabForPawn);
					if (Widgets.CloseButtonFor(rect.AtZero()))
					{
						specificHealthTabForPawn = null;
						SoundDefOf.TabClose.PlayOneShotOnCamera(null);
					}
				}, true, false, 1f, null);
			}
		}

		private void DoColumnHeaders(ref float curY)
		{
			if (!compactMode)
			{
				float num = 135f;
				Text.Anchor = TextAnchor.UpperCenter;
				GUI.color = Widgets.SeparatorLabelColor;
				Widgets.Label(new Rect(num, 3f, PawnLabelColumnWidth, PawnLabelColumnWidth), "Pain".Translate());
				num += PawnLabelColumnWidth;
				List<PawnCapacityDef> list = CapacitiesToDisplay;
				for (int i = 0; i < list.Count; i++)
				{
					Widgets.Label(new Rect(num, 3f, PawnLabelColumnWidth, PawnLabelColumnWidth), list[i].LabelCap.Truncate(PawnLabelColumnWidth, null));
					num += PawnLabelColumnWidth;
				}
				//Rect rect = new Rect(num + 8f, 0f, BeCarriedIfSickIconSize, BeCarriedIfSickIconSize);
				//GUI.DrawTexture(rect, BeCarriedIfSickIcon);
				//TooltipHandler.TipRegionByKey(rect, "BeCarriedIfSickTip");
				num += RowHeight;
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
		}

		private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
		{
			List<Pawn> pawns = Pawns;
			if (specificHealthTabForPawn != null && !pawns.Contains(specificHealthTabForPawn))
			{
				specificHealthTabForPawn = null;
			}
			bool flag = false;
			for (int i = 0; i < pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (pawn.IsColonist)
				{
					if (!flag)
					{
						Widgets.ListSeparator(ref curY, scrollViewRect.width, "CaravanColonists".Translate());
						flag = true;
					}
					DoRow(ref curY, scrollViewRect, scrollOutRect, pawn);
				}
			}
			bool flag2 = false;
			for (int j = 0; j < pawns.Count; j++)
			{
				Pawn pawn2 = pawns[j];
				if (!pawn2.IsColonist)
				{
					if (!flag2)
					{
						Widgets.ListSeparator(ref curY, scrollViewRect.width, ModsConfig.BiotechActive ? "CaravanPrisonersAnimalsAndMechs".Translate() : "CaravanPrisonersAndAnimals".Translate());
						flag2 = true;
					}
					DoRow(ref curY, scrollViewRect, scrollOutRect, pawn2);
				}
			}
		}

		private Vector2 GetRawSize(bool compactMode)
		{
			float num = PawnCapacityColumnWidth;
			if (!compactMode)
			{
				num += PawnCapacityColumnWidth;
				num += CapacitiesToDisplay.Count * PawnCapacityColumnWidth;
				num += RowHeight;
			}
			Vector2 result;
			result.x = 127f + num + 16f;
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
			Rect rect3 = new Rect(SpaceAroundIcon, (rect.height - 27f) / 2f, 27f, 27f);
			Widgets.ThingIcon(rect3, p, 1f, null, false);
			Rect bgRect = new Rect(rect3.xMax + SpaceAroundIcon, 11f, PawnLabelColumnWidth, PawnLabelHeight);
			GenMapUI.DrawPawnLabel(p, bgRect, 1f, PawnLabelColumnWidth, null, GameFont.Small, false, false);
			float num = bgRect.xMax;
			if (!compactMode)
			{
				if (p.RaceProps.IsFlesh)
				{
					Rect rect4 = new Rect(num, 0f, PawnLabelColumnWidth, RowHeight);
					DoPain(rect4, p);
				}
				num += PawnLabelColumnWidth;
				List<PawnCapacityDef> list = CapacitiesToDisplay;
				for (int i = 0; i < list.Count; i++)
				{
					Rect rect5 = new Rect(num, 0f, PawnCapacityColumnWidth, RowHeight);
					if ((p.RaceProps.Humanlike && !list[i].showOnHumanlikes) || (p.RaceProps.Animal && !list[i].showOnAnimals) || (p.RaceProps.IsMechanoid && !list[i].showOnMechanoids) || !PawnCapacityUtility.BodyCanEverDoCapacity(p.RaceProps.body, list[i]))
					{
						num += PawnCapacityColumnWidth;
					}
					else
					{
						DoCapacity(rect5, p, list[i]);
						num += PawnCapacityColumnWidth;
					}
				}
			}
			if (!compactMode)
			{
				//Vector2 vector = new Vector2(num + 8f, 8f);
				//Widgets.Checkbox(vector, ref p.health.beCarriedByCaravanIfSick, IconSize, false, true, null, null);
				//TooltipHandler.TipRegionByKey(new Rect(vector, new Vector2(IconSize, IconSize)), "BeCarriedIfSickTip");
				num += BeCarriedIfSickColumnWidth;
			}
			if (p.Downed && !p.ageTracker.CurLifeStage.alwaysDowned)
			{
				GUI.color = new Color(1f, 0f, 0f, 0.5f);
				Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
				GUI.color = Color.white;
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
			if (specificHealthTabForPawn != null && (specificHealthTabForPawn.Destroyed || !SelAerialVehicle.vehicle.AllPawnsAboard.Contains(specificHealthTabForPawn)))
			{
				specificHealthTabForPawn = null;
			}
		}
	}
}
