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

namespace Vehicles
{
	public static class VehicleInfoCard
	{
		private static VehiclePawn vehicle;
		private static VehicleDef vehicleDef;
		private static InfoCardTab tab;

		private static float listHeight;
		private static float rightPanelHeight;

		private static Vector2 scrollPosition = Vector2.zero;
		private static Vector2 scrollPositionRightPanel = Vector2.zero;
		private static ScrollPositioner scrollPositioner = new ScrollPositioner();
		private static QuickSearchWidget quickSearchWidget = new QuickSearchWidget();

		private static VehicleStatDrawEntry selectedEntry;
		private static VehicleStatDrawEntry mousedOverEntry;
		private static List<VehicleStatDrawEntry> cachedDrawEntries = new List<VehicleStatDrawEntry>();
		internal static List<StatDef> displayedStatDefs = new List<StatDef>();

		public static void RegisterStatDef(StatDef statDef)
		{
			displayedStatDefs.Add(statDef);
		}

		public static void Init(VehiclePawn vehicle)
		{
			VehicleInfoCard.vehicle = vehicle;
			VehicleInfoCard.vehicleDef = vehicle.VehicleDef;
			Reset();
		}

		public static void Init(VehicleDef vehicleDef)
		{
			VehicleInfoCard.vehicleDef = vehicleDef;
			Reset();
		}

		public static void Reset()
		{
			tab = InfoCardTab.Stats;
			scrollPosition = Vector2.zero;
			cachedDrawEntries.Clear();
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InfoCard, KnowledgeAmount.Total);
		}

		private static bool Matching(VehicleStatDrawEntry drawEntry)
		{
			return quickSearchWidget.filter.Matches(drawEntry.LabelCap);
		}

		public static void Clear()
		{
			vehicle = null;
			vehicleDef = null;
			mousedOverEntry = null;
			selectedEntry = null;
			quickSearchWidget.Reset();
		}

		//TEMP (Needs proper hook for info card patch in Gizmos.VehicleInfoCardOverride
		public static void DrawFor(Rect rect, VehicleDef vehicleDef)
		{
			if (VehicleInfoCard.vehicleDef != vehicleDef)
			{
				Init(vehicleDef);
			}
			Draw(rect.ContractedBy(18));
		}

		//TEMP
		public static void DrawFor(Rect rect, VehiclePawn vehicle)
		{
			if (VehicleInfoCard.vehicle != vehicle)
			{
				Init(vehicle);
			}
			Draw(rect.ContractedBy(18));
		}

		public static void Draw(Rect rect)
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
				TabRecord item = new TabRecord("TabStats".Translate(), delegate ()
				{
					tab = InfoCardTab.Stats;
				}, tab == InfoCardTab.Stats);
				list.Add(item);

				TabRecord item2 = new TabRecord("TabHealth".Translate(), delegate ()
				{
					tab = InfoCardTab.Health;
				}, tab == InfoCardTab.Health);
				list.Add(item2);

				TabRecord item3 = new TabRecord("TabRecords".Translate(), delegate ()
				{
					tab = InfoCardTab.Records;
				}, tab == InfoCardTab.Records);
				list.Add(item3);
				TabDrawer.DrawTabs(tabRect, list, 200f);
				FillCard(tabRect.ContractedBy(18f));

				if (tab == InfoCardTab.Stats)
				{
					Rect searchBarRect = new Rect(0, rect.height - Window.QuickSearchSize.y, Window.QuickSearchSize.x, Window.QuickSearchSize.y);
					quickSearchWidget.OnGUI(searchBarRect, cachedDrawEntries.Clear);
				}
			}
		}

		private static void FillCard(Rect rect)
		{
			switch (tab)
			{
				case InfoCardTab.Stats:
					if (vehicle != null)
					{
						DrawStatsReport(rect);
					}
					else
					{
						DrawStatsReport(rect);
					}
					break;
				case InfoCardTab.Health:
					DrawHealthScreen();
					break;
				case InfoCardTab.Upgrades:
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
			return new VehicleStatDrawEntry(VehicleStatCategoryDefOf.VehicleBasicsImportant, "Description".Translate(), string.Empty, description, 99999, hyperlinks: Dialog_InfoCard.DefsToHyperlinks(vehicleDef.descriptionHyperlinks));
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
			cachedDrawEntries = stats.OrderBy(drawEntry => drawEntry.category.displayOrder)
									 .ThenByDescending(drawEntry => drawEntry.DisplayPriorityWithinCategory)
									 .ThenBy(drawEntry => drawEntry.LabelCap).ToList();
			quickSearchWidget.noResultsMatched = !cachedDrawEntries.Any();
			if (selectedEntry != null)
			{
				selectedEntry = cachedDrawEntries.FirstOrDefault((VehicleStatDrawEntry drawEntry) => drawEntry.Matching(selectedEntry));
			}
			if (quickSearchWidget.filter.Active)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void TryRecacheEntries()
		{
			if (cachedDrawEntries.NullOrEmpty())
			{
				cachedDrawEntries.AddRange(StatsToDraw().Where(statDrawEntry => statDrawEntry.ShouldDisplay));
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

			GUIState.Push();
			{
				Text.Font = GameFont.Small;
				Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, listHeight);
				Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
				{
					float curY = 0f;
					string categoryLabel = null;
					mousedOverEntry = null;
					foreach (VehicleStatDrawEntry drawEntry in cachedDrawEntries)
					{
						if (drawEntry.category.LabelCap != categoryLabel)
						{
							Widgets.ListSeparator(ref curY, viewRect.width, drawEntry.category.LabelCap);
							categoryLabel = drawEntry.category.LabelCap;
						}
						bool highlightLabel = false;
						bool lowlightLabel = false;
						bool selected = selectedEntry == drawEntry;
						bool matched = false;

						GUI.color = Color.white;
						if (quickSearchWidget.filter.Active)
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
						Rect drawRect = new Rect(8f, curY, viewRect.width - 8f, 30f);
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
				}
				Widgets.EndScrollView();

				scrollPositioner.ScrollVertically(ref scrollPosition, outRect.size);
				outRect = panelRect.ContractedBy(10f);
				VehicleStatDrawEntry statDrawEntry = selectedEntry ?? mousedOverEntry ?? cachedDrawEntries.FirstOrDefault();
				if (statDrawEntry != null)
				{
					Rect rect5 = new Rect(0f, 0f, outRect.width - 16f, rightPanelHeight);
					string explanationText = statDrawEntry.GetExplanationText(vehicleDef, vehicle);
					float panelHeight = 0f;
					Widgets.BeginScrollView(outRect, ref scrollPositionRightPanel, rect5, true);
					{
						Rect rect6 = rect5;
						rect6.width -= 4f;
						Widgets.Label(rect6, explanationText);
						float textHeight = Text.CalcHeight(explanationText, rect6.width) + 10f;
						panelHeight += textHeight;
						DrawHyperlinks(rect6, statDrawEntry, textHeight);

						rightPanelHeight = panelHeight;
					}
					Widgets.EndScrollView();
				}
			}
			GUIState.Pop();
		}

		private static void DrawHyperlinks(Rect rect, VehicleStatDrawEntry statDrawEntry, float textHeight)
		{
			if (vehicle == null)
			{
				return;
			}
			IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = statDrawEntry.GetHyperlinks(vehicle);
			if (hyperlinks != null)
			{
				Rect rect7 = new Rect(rect.x, rect.y + textHeight, rect.width, rect.height - textHeight);
				Color color = GUI.color;
				GUI.color = Widgets.NormalOptionColor;
				foreach (Dialog_InfoCard.Hyperlink hyperlink in hyperlinks)
				{
					float num4 = Text.CalcHeight(hyperlink.Label, rect7.width);
					Widgets.HyperlinkWithIcon(new Rect(rect7.x, rect7.y, rect7.width, num4), hyperlink, "ViewHyperlink".Translate(hyperlink.Label), 2f, 6f, null, false, null);
					rect7.y += num4;
					rect7.height -= num4;
					textHeight += num4;
				}
				GUI.color = color;
			}
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
