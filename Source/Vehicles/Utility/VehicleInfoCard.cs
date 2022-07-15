using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;

namespace Vehicles
{
	public static class VehicleInfoCard
	{
		private static VehiclePawn vehicle;

		private static InfoCardTab tab;

		private static float listHeight;

		private static Vector2 scrollPosition;

		private static StatDrawEntry mousedOverEntry;
		private static StatDrawEntry selectedEntry;

		private static List<StatDrawEntry> cachedDrawEntries;

		private static QuickSearchWidget quickSearchWidget;

		private static HashSet<StatDef> displayedStatDefs;

		static VehicleInfoCard()
		{
			cachedDrawEntries = new List<StatDrawEntry>();
			quickSearchWidget = new QuickSearchWidget();
			displayedStatDefs = new HashSet<StatDef>();
		}

		public static void RegisterStatDef(StatDef statDef)
		{
			displayedStatDefs.Add(statDef);
		}

		public static void Init(VehiclePawn vehicle)
		{
			VehicleInfoCard.vehicle = vehicle;
			tab = InfoCardTab.Stats;
			scrollPosition = Vector2.zero;
			cachedDrawEntries.Clear();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InfoCard, KnowledgeAmount.Total);
		}

		public static void Clear()
		{
			vehicle = null;
			mousedOverEntry = null;
			selectedEntry = null;
			quickSearchWidget.Reset();
		}

		public static void Draw(Rect inRect)
		{
			Rect rect = new Rect(inRect);
			rect.height = 34f;
			Text.Font = GameFont.Medium;
			Widgets.Label(rect, vehicle.LabelCapNoCount);
			Rect rect2 = new Rect(inRect);
			rect2.yMin = rect.yMax;
			rect2.yMax -= 38f;
			Rect rect3 = rect2;
			rect3.yMin += 45f;
			List<TabRecord> list = new List<TabRecord>();
			TabRecord item = new TabRecord("TabStats".Translate(), delegate ()
			{
				tab = InfoCardTab.Stats;
			}, tab == InfoCardTab.Stats);
			list.Add(item);

			TabRecord item2 = new TabRecord("TabHealth".Translate(), delegate ()
			{
				tab = InfoCardTab.Health;
			}, tab == InfoCardTab.Health);
			//list.Add(item2);

			TabRecord item3 = new TabRecord("TabRecords".Translate(), delegate ()
			{
				tab = InfoCardTab.Records;
			}, tab == InfoCardTab.Records);
			//list.Add(item3);

			TabDrawer.DrawTabs(rect3, list, 200f);
			FillCard(rect3.ContractedBy(18f));
		}

		private static void FillCard(Rect cardRect)
		{
			switch (tab)
			{
				case InfoCardTab.Stats:
					DrawStats(cardRect);
					break;
				case InfoCardTab.Health:
					break;
				case InfoCardTab.Upgrades:
					break;
				case InfoCardTab.Records:
					break;
			}
		}

		private static void DrawStats(Rect rect)
		{
			Rect rect2 = new Rect(rect);
			rect2.width *= 0.5f;
			Rect rect3 = new Rect(rect);
			rect3.x = rect2.xMax;
			rect3.width = rect.xMax - rect3.x;
			Text.Font = GameFont.Small;
			Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, listHeight);
			Widgets.BeginScrollView(rect2, ref scrollPosition, viewRect, true);
			float num = 0f;
			string b = null;
			cachedDrawEntries = StatsToDraw().ToList();
			FinalizeCachedDrawEntries();
			mousedOverEntry = null;
			foreach (StatDrawEntry stat in cachedDrawEntries)
			{
				if (stat.category.LabelCap != b)
				{
					Widgets.ListSeparator(ref num, viewRect.width, stat.category.LabelCap);
					b = stat.category.LabelCap;
				}
				bool highlightLabel = false;
				bool lowlightLabel = false;
				if (quickSearchWidget.filter.Active)
				{
					if (quickSearchWidget.filter.Matches(stat.LabelCap))
					{
						highlightLabel = true;
					}
					else
					{
						lowlightLabel = true;
					}
				}
				num += stat.Draw(8f, num, viewRect.width - 8f, selectedEntry == stat, highlightLabel, lowlightLabel, delegate
				{
					selectedEntry = stat;
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				}, delegate
				{
					mousedOverEntry = stat;
				}, scrollPosition, rect2);
			}
			listHeight = num + 100f;
			Widgets.EndScrollView();
			Rect rect4 = rect3.ContractedBy(10f);
			Widgets.BeginGroup(rect4);
			StatDrawEntry statDrawEntry;
			if ((statDrawEntry = selectedEntry) is null)
			{
				statDrawEntry = mousedOverEntry ?? cachedDrawEntries.FirstOrDefault();
			}
			StatDrawEntry statDrawEntry2 = statDrawEntry;
			if(statDrawEntry2 != null)
			{
				StatRequest optionalReq;
				if(statDrawEntry2.hasOptionalReq)
				{
					optionalReq = statDrawEntry2.optionalReq;
				}
				else if(vehicle != null)
				{
					optionalReq = StatRequest.For(vehicle);
				}
				else
				{
					optionalReq = StatRequest.ForEmpty();
				}
				string explanation = statDrawEntry2.GetExplanationText(optionalReq);
				Rect rect5 = rect4.AtZero();
				Widgets.Label(rect5, explanation);
			}
			Widgets.EndGroup();
		}
		
		private static IEnumerable<StatDrawEntry> StatsToDraw()
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "Description".Translate(), "", vehicle.DescriptionFlavor, 99999, null, Dialog_InfoCard.DefsToHyperlinks(vehicle.def.descriptionHyperlinks));

			foreach (StatDef statDef in DefDatabase<StatDef>.AllDefs.Where(statDef => displayedStatDefs.Contains(statDef)))
			{
				float statValue = vehicle.GetStatValue(statDef, true);
				if (statDef.showOnDefaultValue || statValue != statDef.defaultBaseValue)
				{
					yield return new StatDrawEntry(statDef.category, statDef, statValue, StatRequest.For(vehicle), ToStringNumberSense.Undefined, null, false);
				}
			}
			//foreach (VehicleStatDef stat in DefDatabase<VehicleStatDef>.AllDefs.Where( statDef => statDef.Worker.ShouldShowFor)
			//{
			//	yield return new StatDrawEntry(stat.category, stat);
			//}
		}

		private static void FinalizeCachedDrawEntries()
		{
			cachedDrawEntries = cachedDrawEntries.OrderBy(drawEntry => drawEntry.category.displayOrder)
												 .ThenBy(drawEntry => drawEntry.DisplayPriorityWithinCategory)
												 .ThenByDescending(drawEntry => drawEntry.LabelCap).ToList();
		}

		private enum InfoCardTab : byte
		{
			Stats,
			Health,
			Upgrades,
			Records
		}
	}
}
