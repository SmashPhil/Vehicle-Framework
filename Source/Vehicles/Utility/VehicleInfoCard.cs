using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using SmashTools;
using static Verse.Dialog_InfoCard;
using System.Reflection;

namespace Vehicles
{
	public static class VehicleInfoCard
	{
		private static VehiclePawn vehicle;
		private static VehicleDef vehicleDef;
		private static Dialog_InfoCard infoCard;

		private static float listHeight;
		private static float rightPanelHeight;

		private static Vector2 scrollPosition = Vector2.zero;
		private static Vector2 scrollPositionRightPanel = Vector2.zero;
		private static ScrollPositioner scrollPositioner = new ScrollPositioner();

		private static VehicleStatDrawEntry selectedEntry;
		private static VehicleStatDrawEntry mousedOverEntry;
		private static List<VehicleStatDrawEntry> cachedDrawEntries = new List<VehicleStatDrawEntry>();
		internal static List<StatDef> displayedStatDefs = new List<StatDef>();

		private static FieldInfo tabFieldInfo;

		static VehicleInfoCard()
		{
			tabFieldInfo = AccessTools.Field(typeof(Dialog_InfoCard), "tab");
		}

		public static void RegisterStatDef(StatDef statDef)
		{
			displayedStatDefs.Add(statDef);
		}

		public static void Init(VehiclePawn vehicle, Dialog_InfoCard infoCard)
		{
			VehicleInfoCard.vehicle = vehicle;
			VehicleInfoCard.vehicleDef = vehicle.VehicleDef;
			VehicleInfoCard.infoCard = infoCard;
			Reset();
		}

		public static void Init(VehicleDef vehicleDef, Dialog_InfoCard infoCard)
		{
			VehicleInfoCard.vehicleDef = vehicleDef;
			VehicleInfoCard.infoCard = infoCard;
			Reset();
		}

		public static void Reset()
		{
			infoCard.CommonSearchWidget.Reset();
			tabFieldInfo.SetValue(infoCard, InfoCardTab.Stats);
			scrollPosition = Vector2.zero;
			cachedDrawEntries.Clear();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InfoCard, KnowledgeAmount.Total);
		}

		private static bool Matching(VehicleStatDrawEntry drawEntry)
		{
			return infoCard.CommonSearchWidget.filter.Matches(drawEntry.LabelCap);
		}

		public static void Clear()
		{
			vehicle = null;
			vehicleDef = null;
			infoCard = null;
			mousedOverEntry = null;
			selectedEntry = null;
		}

		public static void DrawFor(Rect rect, VehicleDef vehicleDef, Dialog_InfoCard infoCard, InfoCardTab tab)
		{
			if (VehicleInfoCard.vehicleDef != vehicleDef)
			{
				Clear();
				Init(vehicleDef, infoCard);
			}
			Draw(rect.ContractedBy(18), tab);
		}

		public static void DrawFor(Rect rect, VehiclePawn vehicle, Dialog_InfoCard infoCard, InfoCardTab tab)
		{
			if (VehicleInfoCard.vehicle != vehicle)
			{
				Clear();
				Init(vehicle, infoCard);
			}
			Draw(rect.ContractedBy(18), tab);
		}

		public static void Draw(Rect rect, InfoCardTab tab)
		{
			if (vehicle != null || vehicleDef != null)
			{
				Rect labelRect = new Rect(rect);
				labelRect.height = 34f;
				Text.Font = GameFont.Medium;
				string label = vehicle != null ? vehicle.LabelCapNoCount : vehicleDef.LabelCap.ToString();
				Widgets.Label(labelRect, label);
				Rect tabRect = new Rect(rect)
				{
					yMin = labelRect.yMax + 45,
					yMax = rect.yMax - 20
				};
				List<TabRecord> list = new List<TabRecord>();
				TabRecord statsTab = new TabRecord("TabStats".Translate(), delegate ()
				{
					tabFieldInfo.SetValue(infoCard, InfoCardTab.Stats);
				}, tab == InfoCardTab.Stats);
				list.Add(statsTab);

				TabRecord healthTab = new TabRecord("TabHealth".Translate(), delegate ()
				{
					tabFieldInfo.SetValue(infoCard, InfoCardTab.Health);
				}, tab == InfoCardTab.Health);
				list.Add(healthTab);

				TabRecord recordsTab = new TabRecord("TabRecords".Translate(), delegate ()
				{
					tabFieldInfo.SetValue(infoCard, InfoCardTab.Records);
				}, tab == InfoCardTab.Records);
				list.Add(recordsTab);
				//TabDrawer.DrawTabs(tabRect, list, 200f);
				FillCard(tabRect.ContractedBy(18f), tab);
			}
		}

		private static void FillCard(Rect rect, InfoCardTab tab)
		{
			switch (tab)
			{
				case InfoCardTab.Stats:
					DrawStatsReport(rect);
					break;
				case InfoCardTab.Health:
					DrawHealthScreen();
					break;
				case InfoCardTab.Records:
					break;
			}
		}
		
		private static void DrawHealthScreen()
		{

		}

		public static bool StatListContains(this List<VehicleStatModifier> modList, VehicleStatDef statDef)
		{
			if (!modList.NullOrEmpty())
			{
				for (int i = 0; i < modList.Count; i++)
				{
					if (modList[i].statDef == statDef)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static VehicleStatDrawEntry DescriptionEntry()
		{
			string description = vehicle != null ? vehicle.DescriptionFlavor : vehicleDef.description;
			return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasicsImportant, "Description".Translate(), 
				string.Empty, description, 99999, hyperlinks: Dialog_InfoCard.DefsToHyperlinks(vehicleDef.descriptionHyperlinks));
		}

		private static IEnumerable<VehicleStatDrawEntry> StatsToDraw()
		{
			yield return DescriptionEntry();

			foreach (VehicleStatDef statDef in DefDatabase<VehicleStatDef>.AllDefsListForReading.Where(statDef => statDef.Worker.ShouldShowFor(vehicleDef)))
			{
				float statValue;
				if (vehicle != null)
				{
					statValue = vehicle.GetStatValue(statDef);
				}
				else
				{
					statValue = vehicleDef.GetStatValueAbstract(statDef);
				}
				yield return new VehicleStatDrawEntry(statDef.category, statDef, statValue);
			}
		}

		private static void FinalizeCachedDrawEntries(IEnumerable<VehicleStatDrawEntry> stats)
		{
			cachedDrawEntries = [.. stats.OrderBy(drawEntry => drawEntry.CategoryDisplayOrder)
									 .ThenByDescending(drawEntry => drawEntry.DisplayPriorityWithinCategory)
									 .ThenBy(drawEntry => drawEntry.LabelCap)];
			infoCard.CommonSearchWidget.noResultsMatched = !cachedDrawEntries.Any();
			if (selectedEntry != null)
			{
				selectedEntry = cachedDrawEntries.FirstOrDefault((VehicleStatDrawEntry drawEntry) => drawEntry.Matching(selectedEntry));
			}
			if (infoCard.CommonSearchWidget.filter.Active)
			{
				foreach (VehicleStatDrawEntry drawEntry in cachedDrawEntries)
				{
					if (Matching(drawEntry))
					{
						selectedEntry = drawEntry;
						scrollPositioner.Arm(true);
						break;
					}
				}
			}
		}

		private static void DrawStatsReport(Rect rect)
		{
			TryRecacheEntries();
			DrawStatsWorker(rect);
		}

		private static void TryRecacheEntries()
		{
			if (cachedDrawEntries.NullOrEmpty())
			{
				cachedDrawEntries.AddRange(StatsToDraw().Where(statDrawEntry => statDrawEntry.ShouldDisplay));
				cachedDrawEntries.AddRange(vehicleDef.SpecialDisplayStats(vehicle).Where(statDrawEntry => statDrawEntry.ShouldDisplay));
				FinalizeCachedDrawEntries(cachedDrawEntries);
			}
		}

		public static void SelectEntry(int index)
		{
			if (index < 0 || index > cachedDrawEntries.Count)
			{
				return;
			}
			SelectEntry(cachedDrawEntries[index], true);
		}

		public static void SelectEntry(VehicleStatDef statDef, bool playSound = false)
		{
			foreach (VehicleStatDrawEntry statDrawEntry in cachedDrawEntries)
			{
				if (statDrawEntry.stat == statDef)
				{
					SelectEntry(statDrawEntry, playSound);
					return;
				}
			}
			Messages.Message("MessageCannotSelectInvisibleStat".Translate(statDef), MessageTypeDefOf.RejectInput, false);
		}

		private static void SelectEntry(VehicleStatDrawEntry rec, bool playSound = true)
		{
			selectedEntry = rec;
			scrollPositioner.Arm(true);
			if (playSound)
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
			}
		}

		private static void DrawStatsWorker(Rect rect)
		{
			Rect outRect = new Rect(rect);
			outRect.width *= 0.5f;

			Rect panelRect = new Rect(rect);
			panelRect.x = outRect.xMax;
			panelRect.width = rect.xMax - panelRect.x;

			scrollPositioner.ClearInterestRects();

			// Begin ScrollView
			using TextBlock textFont = new(GameFont.Small);
			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, listHeight);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
			float curY = 0f;
			string categoryLabel = null;
			mousedOverEntry = null;
			foreach (VehicleStatDrawEntry drawEntry in cachedDrawEntries)
			{
				if (drawEntry.CategoryLabel != categoryLabel)
				{
					Widgets.ListSeparator(ref curY, viewRect.width, drawEntry.CategoryLabel);
					categoryLabel = drawEntry.CategoryLabel;
				}
				bool highlightLabel = false;
				bool lowlightLabel = false;
				bool selected = selectedEntry == drawEntry;
				bool matched = false;

				if (infoCard.CommonSearchWidget.filter.Active)
				{
					if (Matching(drawEntry))
					{
						highlightLabel = true;
						matched = true;
					}
					else
					{
						lowlightLabel = true;
					}
				}
				Rect drawRect = new Rect(8f, curY, viewRect.width - 8, 30f);
				curY += drawEntry.Draw(drawRect.x, drawRect.y, drawRect.width, selected, highlightLabel, lowlightLabel, delegate
				{
					SelectEntry(drawEntry, true);
				}, delegate
				{
					mousedOverEntry = drawEntry;
				}, scrollPosition, outRect);
				drawRect.yMax = curY;
				if (selected || matched)
				{
					scrollPositioner.RegisterInterestRect(drawRect);
				}
			}
			listHeight = curY + 100f;
			Widgets.EndScrollView();
			// End ScrollView

			scrollPositioner.ScrollVertically(ref scrollPosition, outRect.size);
			outRect = panelRect.ContractedBy(10f);
			VehicleStatDrawEntry descriptionStat = selectedEntry ?? mousedOverEntry ?? cachedDrawEntries.FirstOrDefault();
			if (descriptionStat != null)
			{
				Rect rightPanelRect = new Rect(0f, 0f, outRect.width - 16f, rightPanelHeight);
				string explanationText = descriptionStat.GetExplanationText(vehicleDef, vehicle);
				float panelHeight = 0f;
				// Begin ScrollView
				Widgets.BeginScrollView(outRect, ref scrollPositionRightPanel, rightPanelRect);
				Rect descriptionRect = rightPanelRect;
				descriptionRect.width -= 4f;
				Widgets.Label(descriptionRect, explanationText);
				float textHeight = Text.CalcHeight(explanationText, descriptionRect.width) + 10f;
				panelHeight += textHeight;
				panelHeight += DrawHyperlinks(descriptionRect, descriptionStat, textHeight);
				Widgets.EndScrollView();
				// End ScrollView

				rightPanelHeight = panelHeight;
			}
		}

		private static float DrawHyperlinks(Rect rect, VehicleStatDrawEntry statDrawEntry, float textHeight)
		{
			float heightTaken = 0;
			Rect hyperlinkRect = new Rect(rect.x, rect.y + textHeight, rect.width, textHeight);
			Color color = GUI.color;
			GUI.color = Widgets.NormalOptionColor;
			foreach (Dialog_InfoCard.Hyperlink hyperlink in statDrawEntry.GetHyperlinks(vehicle))
			{
				float hyperlinkHeight = Text.CalcHeight(hyperlink.Label, hyperlinkRect.width);
				Widgets.HyperlinkWithIcon(hyperlinkRect, hyperlink, "ViewHyperlink".Translate(hyperlink.Label), 2f, 6f, null, false, null);
				hyperlinkRect.y += hyperlinkHeight;
				heightTaken += hyperlinkHeight;
			}
			GUI.color = color;

			return heightTaken;
		}
	}
}
