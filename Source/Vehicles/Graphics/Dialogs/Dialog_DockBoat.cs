using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class Dialog_DockBoat : Window
	{
		private const float TitleRectHeight = 35f;
		private const float BottomAreaHeight = 55f;

		private readonly Vector2 BottomButtonSize = new Vector2(160f, 60f);

		private Caravan caravan;
		private List<TransferableOneWay> transferables = new List<TransferableOneWay>();
		private TransferableOneWayWidget itemsTransfer;

		private bool sourceMassUsageDirty = true;
		private float cachedSourceMassUsage;
		private bool sourceMassCapacityDirty = true;
		private float cachedSourceMassCapacity;
		private string cachedSourceMassCapacityExplanation;
		private bool sourceTilesPerDayDirty = true;
		private float cachedSourceTilesPerDay;
		private string cachedSourceTilesPerDayExplanation;
		private bool sourceDaysWorthOfFoodDirty = true;
		private Pair<float, float> cachedSourceDaysWorthOfFood;
		private bool sourceForagedFoodPerDayDirty = true;
		private Pair<ThingDef, float> cachedSourceForagedFoodPerDay;
		private string cachedSourceForagedFoodPerDayExplanation;
		private bool sourceVisibilityDirty = true;
		private float cachedSourceVisibility;
		private string cachedSourceVisibilityExplanation;
		private bool destMassUsageDirty = true;
		private float cachedDestMassUsage;
		private bool destMassCapacityDirty = true;
		private float cachedDestMassCapacity;
		private string cachedDestMassCapacityExplanation;
		private bool destTilesPerDayDirty = true;
		private float cachedDestTilesPerDay;
		private string cachedDestTilesPerDayExplanation;
		private bool destDaysWorthOfFoodDirty = true;
		private Pair<float, float> cachedDestDaysWorthOfFood;
		private bool destForagedFoodPerDayDirty = true;
		private Pair<ThingDef, float> cachedDestForagedFoodPerDay;
		private string cachedDestForagedFoodPerDayExplanation;
		private bool destVisibilityDirty = true;
		private float cachedDestVisibility;
		private string cachedDestVisibilityExplanation;
		private bool ticksToArriveDirty = true;
		private int cachedTicksToArrive;

		public Dialog_DockBoat(Caravan caravan)
		{
			this.caravan = caravan;
			forcePause = true;
			absorbInputAroundWindow = true;
		}

		public override Vector2 InitialSize => new Vector2(1024f, Verse.UI.screenHeight);

		protected override float Margin => 0f;

		private BiomeDef Biome => caravan.Biome;

		private IEnumerable<Pawn> PawnsEmbarking
		{
			get
			{
				foreach (Pawn p in caravan.PawnsListForReading)
				{
					if (!p.IsBoat())
					{
						yield return p;
					}
				}
			}
		}

		private IEnumerable<ThingCount> ItemsTaken
		{
			get
			{
				foreach(TransferableOneWay t in transferables)
				{
					if(t.HasAnyThing && t.CountToTransfer > 0)
					{
						yield return new ThingCount(t.things.First(), t.CountToTransfer);
					}
				}
			}
		}

		private float SourceMassUsage
		{
			get
			{
				if (sourceMassUsageDirty)
				{
					sourceMassUsageDirty = false;
					cachedSourceMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.Ignore, false, false);
				}
				return cachedSourceMassUsage;
			}
		}

		private float SourceMassCapacity
		{
			get
			{
				if (sourceMassCapacityDirty)
				{
					sourceMassCapacityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					float num = 0;
					foreach(Pawn p in PawnsEmbarking)
					{
						if(MassUtility.CanEverCarryAnything(p))
							num += MassUtility.Capacity(p, stringBuilder);
					}
					cachedSourceMassCapacity = num; //CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
					cachedSourceMassCapacityExplanation = stringBuilder.ToString();
				}
				return this.cachedSourceMassCapacity;
			}
		}

		private float SourceTilesPerDay
		{
			get
			{
				if (sourceTilesPerDayDirty)
				{
					sourceTilesPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedSourceTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(CaravanTicksPerMoveUtility.GetTicksPerMove(PawnsEmbarking.ToList(), SourceMassUsage, SourceMassCapacity, stringBuilder), caravan.Tile, -1, stringBuilder);
					//this.cachedSourceTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.SourceMassUsage, this.SourceMassCapacity, this.caravan.Tile, (!this.caravan.pather.Moving) ? -1 : this.caravan.pather.nextTile, stringBuilder);
					cachedSourceTilesPerDayExplanation = stringBuilder.ToString();
				}
				return cachedSourceTilesPerDay;
			}
		}

		private Pair<float, float> SourceDaysWorthOfFood
		{
			get
			{
				if (sourceDaysWorthOfFoodDirty)
				{
					sourceDaysWorthOfFoodDirty = false;
					float first;
					float second;
					first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(PawnsEmbarking.ToList(), ItemsTaken.ToList(), caravan.Tile, IgnorePawnsInventoryMode.Ignore, Faction.OfPlayer);
					second = DaysUntilRotCalculator.ApproxDaysUntilRotLeftAfterTransfer(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, null, 0f, 3300);

					cachedSourceDaysWorthOfFood = new Pair<float, float>(first, second);
				}
				return cachedSourceDaysWorthOfFood;
			}
		}

		private Pair<ThingDef, float> SourceForagedFoodPerDay
		{
			get
			{
				if (sourceForagedFoodPerDayDirty)
				{
					sourceForagedFoodPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedSourceForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(PawnsEmbarking.ToList(), Biome, Faction.OfPlayer, true, false, stringBuilder);
					cachedSourceForagedFoodPerDayExplanation = stringBuilder.ToString();
				}
				return cachedSourceForagedFoodPerDay;
			}
		}

		private float SourceVisibility
		{
			get
			{
				if (sourceVisibilityDirty)
				{
					sourceVisibilityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedSourceVisibility = CaravanVisibilityCalculator.Visibility(PawnsEmbarking, true, stringBuilder);
					cachedSourceVisibilityExplanation = stringBuilder.ToString();
				}
				return cachedSourceVisibility;
			}
		}

		private float DestMassUsage
		{
			get
			{
				if (destMassUsageDirty)
				{
					destMassUsageDirty = false;
					cachedDestMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.Ignore, false, false);
				}
				return cachedDestMassUsage;
			}
		}

		private float DestMassCapacity
		{
			get
			{
				if (destMassCapacityDirty)
				{
					destMassCapacityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedDestMassCapacity = CollectionsMassCalculator.CapacityTransferables(transferables, stringBuilder);
					cachedDestMassCapacityExplanation = stringBuilder.ToString();
				}
				return cachedDestMassCapacity;
			}
		}

		private float DestTilesPerDay
		{
			get
			{
				if (destTilesPerDayDirty)
				{
					destTilesPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedDestTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, DestMassUsage, DestMassCapacity, caravan.Tile, (!caravan.pather.Moving) ? -1 : caravan.pather.nextTile, stringBuilder);
					cachedDestTilesPerDayExplanation = stringBuilder.ToString();
				}
				return cachedDestTilesPerDay;
			}
		}

		private Pair<float, float> DestDaysWorthOfFood
		{
			get
			{
				if (destDaysWorthOfFoodDirty)
				{
					destDaysWorthOfFoodDirty = false;
					float first;
					float second;
					if (caravan.pather.Moving)
					{
						first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, caravan.Faction, caravan.pather.curPath, caravan.pather.nextTileCostLeft, caravan.TicksPerMove);
						second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, caravan.pather.curPath, caravan.pather.nextTileCostLeft, caravan.TicksPerMove);
					}
					else
					{
						first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, caravan.Faction, null, 0f, 3300);
						second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, null, 0f, 3300);
					}
					cachedDestDaysWorthOfFood = new Pair<float, float>(first, second);
				}
				return cachedDestDaysWorthOfFood;
			}
		}

		private Pair<ThingDef, float> DestForagedFoodPerDay
		{
			get
			{
				if (destForagedFoodPerDayDirty)
				{
					destForagedFoodPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedDestForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables, Biome, Faction.OfPlayer, stringBuilder);
					cachedDestForagedFoodPerDayExplanation = stringBuilder.ToString();
				}
				return cachedDestForagedFoodPerDay;
			}
		}

		private float DestVisibility
		{
			get
			{
				if (destVisibilityDirty)
				{
					destVisibilityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedDestVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
					cachedDestVisibilityExplanation = stringBuilder.ToString();
				}
				return cachedDestVisibility;
			}
		}

		private int TicksToArrive
		{
			get
			{
				if (!caravan.pather.Moving)
				{
					return 0;
				}
				if (ticksToArriveDirty)
				{
					ticksToArriveDirty = false;
					cachedTicksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(caravan, false);
				}
				return cachedTicksToArrive;
			}
		}

		public override void PostOpen()
		{
			base.PostOpen();
			CalculateAndRecacheTransferables();
		}

		public override void PostClose()
		{
			base.PostClose();
			if (caravan.PawnsListForReading.NotNullAndAny(p => p.IsBoat()))
			{
				CaravanHelper.ToggleDocking(caravan, false);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect rect = new Rect(0f, 0f, inRect.width, TitleRectHeight);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, "DockCaravan".Translate());
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(SourceMassUsage, SourceMassCapacity, cachedSourceMassCapacityExplanation, SourceTilesPerDay, 
				cachedSourceTilesPerDayExplanation, SourceDaysWorthOfFood, SourceForagedFoodPerDay, cachedSourceForagedFoodPerDayExplanation, SourceVisibility, 
				cachedSourceVisibilityExplanation, -1f, -1f, null), null, caravan.Tile, (!caravan.pather.Moving) ? null : new int?(TicksToArrive), -9999f, 
				new Rect(12f, TitleRectHeight, inRect.width - 24f, 40f), true, null, false);
			inRect.yMin += 119f;
			Widgets.DrawMenuSection(inRect);
			TabDrawer.DrawTabs(inRect, new List<TabRecord>() { new TabRecord("ItemsTab".Translate(), null, true) }, 200f);
			inRect = inRect.ContractedBy(17f);
			Widgets.BeginGroup(inRect);
			Rect rect2 = inRect.AtZero();
			DoBottomButtons(rect2);
			Rect inRect2 = rect2;
			inRect2.yMax -= 59f;
			itemsTransfer.OnGUI(inRect2, out bool flag);
			if (flag)
			{
				CountToTransferChanged();
			}
			Widgets.EndGroup();
		}

		private void AddToTransferables(Thing t)
		{
			TransferableOneWay transferable = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
			if(transferable is null)
			{
				transferable = new TransferableOneWay();
				transferables.Add(transferable);
			}
			transferable.things.Add(t);
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - BottomAreaHeight, BottomButtonSize.x, BottomButtonSize.y);
			if(Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true) && DockBoatTransferPawns())
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				Close(false);
			}
			Rect rect3 = new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
			if(Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, false))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				CalculateAndRecacheTransferables();
			}
			Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
			if(Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
			{
				Close(true);
			}
		}

		private void CalculateAndRecacheTransferables()
		{
			transferables = new List<TransferableOneWay>();
			//this.AddPawnsToTransferables();
			AddItemsToTransferables();
			CreateCaravanItemsWidget(transferables, out itemsTransfer, "SplitCaravanThingCountTip".Translate(), IgnorePawnsInventoryMode.Ignore, () => DestMassCapacity - DestMassUsage,
				false, caravan.Tile, false);
			CountToTransferChanged();
		}

		private bool DockBoatTransferPawns()
		{
			DockedBoat dockedBoat = (DockedBoat)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DockedBoat);
			dockedBoat.Tile = caravan.Tile;
			float randomInRange = Rand.Range(2f, 4f) + (50 * (1 - caravan.PawnsListForReading.Where(x => x.IsBoat()).Max(x => (x as VehiclePawn).VehicleDef.properties.visibility)));
			dockedBoat.GetComponent<TimeoutComp>().StartTimeout(Mathf.CeilToInt(randomInRange * 60000));
			List<Pawn> boats = caravan.PawnsListForReading.Where(p => p.IsBoat()).ToList();
			List<Pawn> pawns = caravan.PawnsListForReading.Where(p => !p.IsBoat()).ToList();
			if(caravan.PawnsListForReading.Where(p => !p.IsBoat()).Count() <= 0) return false;

			foreach(TransferableOneWay t in transferables)
			{
				TransferableUtility.TransferNoSplit(t.things, t.CountToTransfer, delegate(Thing thing, int numToTake)
				{
					Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(caravan, thing);
					if(ownerOf is null) return;
					CaravanInventoryUtility.MoveInventoryToSomeoneElse(ownerOf, thing, pawns, boats, numToTake);
				}, true, true);
			}

			for(int i = caravan.pawns.Count - 1; i >= 0; i--)
			{
				Pawn p = caravan.PawnsListForReading[i];
				if(p.IsBoat())
				{
					dockedBoat.dockedBoats.TryAddOrTransfer(p, false);
				}
			}
			Find.WorldObjects.Add(dockedBoat);
			return true;
		}

		private void CreateCaravanItemsWidget(List<TransferableOneWay> transferables, out TransferableOneWayWidget itemsTransfer, string thingCountTip, IgnorePawnsInventoryMode ignorePawnInventoryMass,
			Func<float> availableMassGetter, bool ignoreSpawnedCorpsesGearAndInventoryMass, int tile, bool playerPawnsReadOnly = false)
		{
			itemsTransfer = new TransferableOneWayWidget(transferables, null, null, thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f, ignoreSpawnedCorpsesGearAndInventoryMass, tile,
				true, false, false, true, false, true, false);
		}

		private bool CheckForErrors(List<Pawn> pawns)
		{
			if(!pawns.NotNullAndAny( (Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
			{
				Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), caravan, MessageTypeDefOf.RejectInput, false);
				return false;
			}
			return true;
		}

		private void AddItemsToTransferables()
		{
			List<Thing> list = CaravanInventoryUtility.AllInventoryItems(caravan);
			foreach (Thing t in list)
			{
				AddToTransferables(t);
			}
		}

		private void CountToTransferChanged()
		{
			sourceMassUsageDirty = true;
			sourceMassCapacityDirty = true;
			sourceTilesPerDayDirty = true;
			sourceDaysWorthOfFoodDirty = true;
			sourceForagedFoodPerDayDirty = true;
			sourceVisibilityDirty = true;
			destMassUsageDirty = true;
			destMassCapacityDirty = true;
			destTilesPerDayDirty = true;
			destDaysWorthOfFoodDirty = true;
			destForagedFoodPerDayDirty = true;
			destVisibilityDirty = true;
			ticksToArriveDirty = true;
		}
	}
}
