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
	[StaticConstructorOnStartup]
    public class TransferableVehicleWidget
    {
		public float TotalNumbersColumnsWidths
		{
			get
			{
				float num = 315f;
				if (drawMass)
				{
					num += 100f;
				}
				if (drawMarketValue)
				{
					num += 100f;
				}
				if (drawFishPerDay)
				{
					num += 75f;
				}
				return num;
			}
		}

		private bool AnyTransferable
		{
			get
			{
				if (!transferablesCached)
				{
					CacheTransferables();
				}
				for (int i = 0; i < sections.Count; i++)
				{
					if (sections[i].cachedTransferables.Any<TransferableOneWay>())
					{
						return true;
					}
				}
				return false;
			}
		}

		public TransferableVehicleWidget(IEnumerable<TransferableOneWay> transferables, string sourceLabel, string destinationLabel, string sourceCountDesc, bool drawMass = false, 
			IgnorePawnsInventoryMode ignorePawnInventoryMass = IgnorePawnsInventoryMode.DontIgnore, bool includePawnsMassInMassUsage = false, Func<float> availableMassGetter = null, 
			float extraHeaderSpace = 0f, bool ignoreSpawnedCorpseGearAndInventoryMass = false, int tile = -1, bool drawMarketValue = false, bool drawNutritionEatenPerDay = false, 
			bool drawFishPerDay = false, bool playerPawnsReadOnly = false)
		{
			if (transferables != null)
			{
				AddSection(null, transferables);
			}
			this.sourceLabel = sourceLabel;
			this.destinationLabel = destinationLabel;
			this.sourceCountDesc = sourceCountDesc;
			this.drawMass = drawMass;
			this.ignorePawnInventoryMass = ignorePawnInventoryMass;
			this.includePawnsMassInMassUsage = includePawnsMassInMassUsage;
			this.availableMassGetter = availableMassGetter;
			this.extraHeaderSpace = extraHeaderSpace;
			this.ignoreSpawnedCorpseGearAndInventoryMass = ignoreSpawnedCorpseGearAndInventoryMass;
			this.tile = tile;
			this.drawMarketValue = drawMarketValue;
			this.drawNutritionEatenPerDay = drawNutritionEatenPerDay;
			this.drawFishPerDay = drawFishPerDay;
			this.playerPawnsReadOnly = playerPawnsReadOnly;
			sorter1 = TransferableSorterDefOf.Category;
			sorter2 = TransferableSorterDefOf.MarketValue;
		}

		public void AddSection(string title, IEnumerable<TransferableOneWay> transferables)
		{
			Section item = default;
			item.title = title;
			item.transferables = transferables;
			item.cachedTransferables = new List<TransferableOneWay>();
			sections.Add(item);
			transferablesCached = false;
		}

		private void CacheTransferables()
		{
			transferablesCached = true;
			for (int i = 0; i < this.sections.Count; i++)
			{
				List<TransferableOneWay> cachedTransferables = sections[i].cachedTransferables;
				cachedTransferables.Clear();
				cachedTransferables.AddRange(sections[i].transferables.OrderBy((TransferableOneWay tr) => tr, sorter1.Comparer).ThenBy((TransferableOneWay tr) => tr, sorter2.Comparer).ThenBy((TransferableOneWay tr) => TransferableUIUtility.DefaultListOrderPriority(tr)).ToList<TransferableOneWay>());
			}
		}

		public void OnGUI(Rect inRect, out bool anythingChanged)
		{
			if (!transferablesCached)
			{
				CacheTransferables();
			}
			TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, delegate(TransferableSorterDef x)
			{
				sorter1 = x;
				CacheTransferables();
			}, delegate(TransferableSorterDef x)
			{
				sorter2 = x;
				CacheTransferables();
			});
			if (!sourceLabel.NullOrEmpty() || !destinationLabel.NullOrEmpty())
			{
				float num = inRect.width - 515f;
				Rect position = new Rect(inRect.x + num, inRect.y, inRect.width - num, 37f);
				GUI.BeginGroup(position);
				Text.Font = GameFont.Medium;
				if (!sourceLabel.NullOrEmpty())
				{
					Rect rect = new Rect(0f, 0f, position.width / 2f, position.height);
					Text.Anchor = TextAnchor.UpperLeft;
					Widgets.Label(rect, sourceLabel);
				}
				if (!destinationLabel.NullOrEmpty())
				{
					Rect rect2 = new Rect(position.width / 2f, 0f, position.width / 2f, position.height);
					Text.Anchor = TextAnchor.UpperRight;
					Widgets.Label(rect2, destinationLabel);
				}
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.EndGroup();
			}
			Rect mainRect = new Rect(inRect.x, inRect.y + 37f + extraHeaderSpace, inRect.width, inRect.height - 37f - extraHeaderSpace);
			FillMainRect(mainRect, out anythingChanged);
		}

		private void FillMainRect(Rect mainRect, out bool anythingChanged)
		{
			anythingChanged = false;
			Text.Font = GameFont.Small;
			if (AnyTransferable)
			{
				float num = 6f;
				for (int i = 0; i < sections.Count; i++)
				{
					num += sections[i].cachedTransferables.Count * 30f;
					if (sections[i].title != null)
					{
						num += 30f;
					}
				}
				float num2 = 6f;
				float availableMass = (availableMassGetter != null) ? availableMassGetter() : float.MaxValue;
				Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, num);
				Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect, true);
				float num3 = scrollPosition.y - 30f;
				float num4 = scrollPosition.y + mainRect.height;
				for (int j = 0; j < sections.Count; j++)
				{
					List<TransferableOneWay> cachedTransferables = sections[j].cachedTransferables;
					if (cachedTransferables.Any<TransferableOneWay>())
					{
						if (sections[j].title != null)
						{
							Widgets.ListSeparator(ref num2, viewRect.width, sections[j].title);
							num2 += 5f;
						}
						for (int k = 0; k < cachedTransferables.Count; k++)
						{
							if (num2 > num3 && num2 < num4)
							{
								Rect rect = new Rect(0f, num2, viewRect.width, 30f);
								int countToTransfer = cachedTransferables[k].CountToTransfer;
								DoRow(rect, cachedTransferables[k], k, availableMass);
								if (countToTransfer != cachedTransferables[k].CountToTransfer)
								{
									anythingChanged = true;
								}
							}
							num2 += 30f;
						}
					}
				}
				Widgets.EndScrollView();
				return;
			}
			GUI.color = Color.gray;
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(mainRect, "NoneBrackets".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}

		private void DoRow(Rect rect, TransferableOneWay trad, int index, float availableMass)
		{
			if (index % 2 == 1)
			{
				Widgets.DrawLightHighlight(rect);
			}
			Text.Font = GameFont.Small;
			GUI.BeginGroup(rect);
			float num = rect.width;
			int maxCount = trad.MaxCount;
			Rect rect2 = new Rect(num - 240f, 0f, 240f, rect.height);
			stoppingPoints.Clear();
			if (availableMassGetter != null && (!(trad.AnyThing is Pawn) || includePawnsMassInMassUsage))
			{
				float num2 = availableMass + GetMass(trad.AnyThing) * (float)trad.CountToTransfer;
				int threshold = (num2 <= 0f) ? 0 : Mathf.FloorToInt(num2 / GetMass(trad.AnyThing));
				stoppingPoints.Add(new TransferableCountToTransferStoppingPoint(threshold, "M<", ">M"));
			}
			VehiclePawn pawn = trad.AnyThing as VehiclePawn;
			bool flag = pawn != null && (pawn.IsColonist || pawn.IsPrisonerOfColony);
			HelperMethods.DoCountAdjustInterface(rect2, trad, AvailablePawns, index, 0, maxCount, false, stoppingPoints, playerPawnsReadOnly && flag);
			num -= 240f;
			if (drawMarketValue)
			{
				Rect rect3 = new Rect(num - 100f, 0f, 100f, rect.height);
				Text.Anchor = TextAnchor.MiddleLeft;
				DrawMarketValue(rect3, trad);
				num -= 100f;
			}
			if (drawMass)
			{
				Rect rect4 = new Rect(num - 100f, 0f, 100f, rect.height);
				Text.Anchor = TextAnchor.MiddleLeft;
				DrawMass(rect4, trad, availableMass);
				num -= 100f;
			}

			if (drawFishPerDay)
			{
				Rect rect7 = new Rect(num - 75f, 0f, 75f, rect.height);
				Text.Anchor = TextAnchor.MiddleLeft;
				if (!DrawGrazeability(rect7, trad))
				{
					DrawForagedFoodPerDay(rect7, trad);
				}
				num -= 75f;
			}
			if (drawNutritionEatenPerDay)
			{
				Rect rect8 = new Rect(num - 75f, 0f, 75f, rect.height);
				Text.Anchor = TextAnchor.MiddleLeft;
				DrawNutritionEatenPerDay(rect8, trad);
				num -= 75f;
			}
			num -= 150f;
			//...
			num -= 75f;

			Rect idRect = new Rect(0f, 0f, num, rect.height);
			HelperMethods.DrawVehicleTransferableInfo(trad, idRect, Color.white);
			GenUI.ResetLabelAlign();
			GUI.EndGroup();
		}

		private bool ShouldShowCount(TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return true;
			}
			Pawn pawn = trad.AnyThing as Pawn;
			return pawn == null || !pawn.RaceProps.Humanlike || trad.MaxCount != 1;
		}

		private void DrawDaysUntilRot(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			if (!trad.ThingDef.IsNutritionGivingIngestible)
			{
				return;
			}
			int num;
			if (!this.cachedTicksUntilRot.TryGetValue(trad, out num))
			{
				num = int.MaxValue;
				for (int i = 0; i < trad.things.Count; i++)
				{
					CompRottable compRottable = trad.things[i].TryGetComp<CompRottable>();
					if (compRottable != null)
					{
						num = Mathf.Min(num, DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(compRottable, this.tile, null));
					}
				}
				this.cachedTicksUntilRot.Add(trad, num);
			}
			if (num >= 36000000 || (float)num >= 36000000f)
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			float num2 = (float)num / 60000f;
			GUI.color = Color.yellow;
			Widgets.Label(rect, num2.ToString("0.#"));
			GUI.color = Color.white;
			TooltipHandler.TipRegionByKey(rect, "DaysUntilRotTip");
		}

		private void DrawItemNutrition(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			if (!trad.ThingDef.IsNutritionGivingIngestible)
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = Color.green;
			Widgets.Label(rect, trad.ThingDef.GetStatValueAbstract(StatDefOf.Nutrition, null).ToString("0.##"));
			GUI.color = Color.white;
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, "ItemNutritionTip".Translate((1.6f * ThingDefOf.Human.race.baseHungerRate).ToString("0.##")));
			}
		}

		private bool DrawGrazeability(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return false;
			}
			Pawn pawn = trad.AnyThing as Pawn;
			if (pawn == null || !VirtualPlantsUtility.CanEverEatVirtualPlants(pawn))
			{
				return false;
			}
			rect.width = 40f;
			Rect position = new Rect(rect.x + (float)((int)((rect.width - 28f) / 2f)), rect.y + (float)((int)((rect.height - 28f) / 2f)), 28f, 28f);
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.DrawTexture(position, CanGrazeIcon);
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, delegate()
				{
					TaggedString taggedString = "AnimalCanGrazeTip".Translate();
					if (this.tile != -1)
					{
						taggedString += "\n\n" + VirtualPlantsUtility.GetVirtualPlantsStatusExplanationAt(this.tile, Find.TickManager.TicksAbs);
					}
					return taggedString;
				}, trad.GetHashCode() ^ 1948571634);
			}
			return true;
		}

		private void DrawForagedFoodPerDay(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			Pawn p = trad.AnyThing as Pawn;
			if (p == null)
			{
				return;
			}
			bool flag;
			float foragedNutritionPerDay = ForagedFoodPerDayCalculator.GetBaseForagedNutritionPerDay(p, out flag);
			if (flag)
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = ((foragedNutritionPerDay == 0f) ? Color.gray : Color.green);
			Widgets.Label(rect, "+" + foragedNutritionPerDay.ToString("0.##"));
			GUI.color = Color.white;
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => "NutritionForagedPerDayTip".Translate(StatDefOf.ForagedNutritionPerDay.Worker.GetExplanationFull(StatRequest.For(p), StatDefOf.ForagedNutritionPerDay.toStringNumberSense, foragedNutritionPerDay)), trad.GetHashCode() ^ 1958671422);
			}
		}

		private void DrawNutritionEatenPerDay(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			Pawn p = trad.AnyThing as Pawn;
			if (p == null || !p.RaceProps.EatsFood || p.Dead || p.needs.food == null)
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			string text = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed, true) * 60000f).ToString("0.##");
			DietCategory resolvedDietCategory = p.RaceProps.ResolvedDietCategory;
			if (resolvedDietCategory != DietCategory.Omnivorous)
			{
				text = text + " (" + resolvedDietCategory.ToStringHumanShort() + ")";
			}
			GUI.color = new Color(1f, 0.5f, 0f);
			Widgets.Label(rect, text);
			GUI.color = Color.white;
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, () => RaceProperties.NutritionEatenPerDayExplanation_NewTemp(p, true, true, false), trad.GetHashCode() ^ 385968958);
			}
		}

		private void DrawMarketValue(Rect rect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			Widgets.Label(rect, trad.AnyThing.MarketValue.ToStringMoney(null));
			TooltipHandler.TipRegionByKey(rect, "MarketValueTip");
		}

		private void DrawMass(Rect rect, TransferableOneWay trad, float availableMass)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			Thing anyThing = trad.AnyThing;
			Pawn pawn = anyThing as Pawn;
			if (pawn != null && !this.includePawnsMassInMassUsage && !MassUtility.CanEverCarryAnything(pawn))
			{
				return;
			}
			Widgets.DrawHighlightIfMouseover(rect);
			if (pawn == null || this.includePawnsMassInMassUsage)
			{
				float mass = this.GetMass(anyThing);
				if (Mouse.IsOver(rect))
				{
					if (pawn != null)
					{
						float gearMass = 0f;
						float invMass = 0f;
						gearMass = MassUtility.GearMass(pawn);
						if (!InventoryCalculatorsUtility.ShouldIgnoreInventoryOf(pawn, this.ignorePawnInventoryMass))
						{
							invMass = MassUtility.InventoryMass(pawn);
						}
						TooltipHandler.TipRegion(rect, () => this.GetPawnMassTip(trad, 0f, mass - gearMass - invMass, gearMass, invMass), trad.GetHashCode() * 59);
					}
					else
					{
						TooltipHandler.TipRegion(rect, "ItemWeightTip".Translate());
					}
				}
				if (mass > availableMass)
				{
					GUI.color = ColoredText.RedReadable;
				}
				else
				{
					GUI.color = TransferableOneWayWidget.ItemMassColor;
				}
				Widgets.Label(rect, mass.ToStringMass());
			}
			else
			{
				float cap = MassUtility.Capacity(pawn, null);
				float gearMass = MassUtility.GearMass(pawn);
				float invMass = InventoryCalculatorsUtility.ShouldIgnoreInventoryOf(pawn, this.ignorePawnInventoryMass) ? 0f : MassUtility.InventoryMass(pawn);
				float num = cap - gearMass - invMass;
				if (num > 0f)
				{
					GUI.color = Color.green;
				}
				else if (num < 0f)
				{
					GUI.color = ColoredText.RedReadable;
				}
				else
				{
					GUI.color = Color.gray;
				}
				Widgets.Label(rect, num.ToStringMassOffset());
				if (Mouse.IsOver(rect))
				{
					TooltipHandler.TipRegion(rect, () => this.GetPawnMassTip(trad, cap, 0f, gearMass, invMass), trad.GetHashCode() * 59);
				}
			}
			GUI.color = Color.white;
		}

		private void DrawEquippedWeapon(Rect rect, Rect iconRect, TransferableOneWay trad)
		{
			if (!trad.HasAnyThing)
			{
				return;
			}
			Pawn pawn = trad.AnyThing as Pawn;
			if (pawn == null || pawn.equipment == null || pawn.equipment.Primary == null)
			{
				return;
			}
			ThingWithComps primary = pawn.equipment.Primary;
			Widgets.DrawHighlightIfMouseover(rect);
			Widgets.ThingIcon(iconRect, primary, 1f);
			if (Mouse.IsOver(rect))
			{
				TooltipHandler.TipRegion(rect, primary.LabelCap);
			}
		}

		private string GetPawnMassTip(TransferableOneWay trad, float capacity, float pawnMass, float gearMass, float invMass)
		{
			if (!trad.HasAnyThing)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			if (capacity != 0f)
			{
				stringBuilder.Append("MassCapacity".Translate() + ": " + capacity.ToStringMass());
			}
			else
			{
				stringBuilder.Append("Mass".Translate() + ": " + pawnMass.ToStringMass());
			}
			if (gearMass != 0f)
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("EquipmentAndApparelMass".Translate() + ": " + gearMass.ToStringMass());
			}
			if (invMass != 0f)
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("InventoryMass".Translate() + ": " + invMass.ToStringMass());
			}
			return stringBuilder.ToString();
		}

		private float GetMass(Thing thing)
		{
			if (thing == null)
			{
				return 0f;
			}
			float num = thing.GetStatValue(StatDefOf.Mass, true);
			Pawn pawn = thing as Pawn;
			if (pawn != null)
			{
				if (InventoryCalculatorsUtility.ShouldIgnoreInventoryOf(pawn, this.ignorePawnInventoryMass))
				{
					num -= MassUtility.InventoryMass(pawn);
				}
			}
			else if (this.ignoreSpawnedCorpseGearAndInventoryMass)
			{
				Corpse corpse = thing as Corpse;
				if (corpse != null && corpse.Spawned)
				{
					num -= MassUtility.GearAndInventoryMass(corpse.InnerPawn);
				}
			}
			return num;
		}

		private List<Section> sections = new List<Section>();

		public List<TransferableOneWay> AvailablePawns { get; set; }

		private string sourceLabel;
		private string destinationLabel;
		private string sourceCountDesc;
		private bool drawMass;
		private IgnorePawnsInventoryMode ignorePawnInventoryMass = IgnorePawnsInventoryMode.DontIgnore;
		private bool includePawnsMassInMassUsage;
		private Func<float> availableMassGetter;
		private float extraHeaderSpace;
		private bool ignoreSpawnedCorpseGearAndInventoryMass;
		private int tile;
		private bool drawMarketValue;
		private bool drawNutritionEatenPerDay;
		private bool drawFishPerDay;
		private bool playerPawnsReadOnly;
		private bool transferablesCached;
		private Vector2 scrollPosition;
		private TransferableSorterDef sorter1;
		private TransferableSorterDef sorter2;
		private Dictionary<TransferableOneWay, int> cachedTicksUntilRot = new Dictionary<TransferableOneWay, int>();
		private static List<TransferableCountToTransferStoppingPoint> stoppingPoints = new List<TransferableCountToTransferStoppingPoint>();
		private const float TopAreaHeight = 37f;
		protected readonly Vector2 AcceptButtonSize = new Vector2(160f, 40f);
		protected readonly Vector2 OtherBottomButtonSize = new Vector2(160f, 40f);
		private const float ColumnWidth = 120f;
		private const float FirstTransferableY = 6f;
		private const float RowInterval = 30f;
		public const float CountColumnWidth = 75f;
		public const float AdjustColumnWidth = 240f;
		public const float MassColumnWidth = 100f;
		public static readonly Color ItemMassColor = new Color(0.7f, 0.7f, 0.7f);
		private const float MarketValueColumnWidth = 100f;
		private const float ExtraSpaceAfterSectionTitle = 5f;
		private const float DaysUntilRotColumnWidth = 75f;
		private const float NutritionEatenPerDayColumnWidth = 75f;
		private const float ItemNutritionColumnWidth = 75f;
		private const float ForagedFoodPerDayColumnWidth = 75f;
		private const float GrazeabilityInnerColumnWidth = 40f;
		private const float EquippedWeaponIconSize = 30f;
		public const float TopAreaWidth = 515f;
		private static readonly Texture2D CanGrazeIcon = ContentFinder<Texture2D>.Get("UI/Icons/CanGraze", true);

		private struct Section
		{
			public string title;
			public IEnumerable<TransferableOneWay> transferables;
			public List<TransferableOneWay> cachedTransferables;
		}
    }
}
