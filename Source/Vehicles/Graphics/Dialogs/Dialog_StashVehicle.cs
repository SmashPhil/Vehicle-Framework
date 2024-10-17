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
	public class Dialog_StashVehicle : Window
	{
		private const float TitleRectHeight = 35f;
		private const float BottomAreaHeight = 55f;

		private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private VehicleCaravan caravan;
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

		public Dialog_StashVehicle(VehicleCaravan caravan)
		{
			this.caravan = caravan;
			forcePause = true;
			absorbInputAroundWindow = true;
		}

		public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

		protected override float Margin => 0f;

		private BiomeDef Biome => caravan.Biome;

		private IEnumerable<Pawn> PawnsEmbarking
		{
			get
			{
				foreach (Pawn pawn in caravan.PawnsListForReading)
				{
					if (!pawn.IsBoat())
					{
						yield return pawn;
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
					float capacity = 0;
					foreach (Pawn pawn in PawnsEmbarking)
					{
						if (MassUtility.CanEverCarryAnything(pawn))
						{
							capacity += MassUtility.Capacity(pawn, stringBuilder);
						}
					}
					cachedSourceMassCapacity = capacity; //CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
					cachedSourceMassCapacityExplanation = stringBuilder.ToString();
				}
				return cachedSourceMassCapacity;
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
					//this.cachedSourceTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.SourceMassUsage, this.SourceMassCapacity, this.caravan.Tile, (!this.caravan.vehiclePather.Moving) ? -1 : this.caravan.vehiclePather.nextTile, stringBuilder);
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
					first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, caravan.Tile, IgnorePawnsInventoryMode.DontIgnore, caravan.Faction);
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
					cachedDestTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, DestMassUsage, DestMassCapacity, caravan.Tile, (!caravan.vehiclePather.Moving) ? -1 : caravan.vehiclePather.nextTile, stringBuilder);
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
					if (caravan.vehiclePather.Moving)
					{
						first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, caravan.Faction, caravan.vehiclePather.curPath, caravan.vehiclePather.nextTileCostLeft, caravan.TicksPerMove);
						second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, caravan.Tile, IgnorePawnsInventoryMode.Ignore, caravan.vehiclePather.curPath, caravan.vehiclePather.nextTileCostLeft, caravan.TicksPerMove);
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
				if (!caravan.vehiclePather.Moving)
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

		public override void DoWindowContents(Rect inRect)
		{
			using (new TextBlock(GameFont.Medium, TextAnchor.MiddleCenter))
			{
				Rect titleRect = new Rect(0f, 0f, inRect.width, TitleRectHeight);
				Widgets.Label(titleRect, "VF_DockToCaravan".Translate());
			}

			using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
			{
				CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(SourceMassUsage, SourceMassCapacity, cachedSourceMassCapacityExplanation, SourceTilesPerDay,
				cachedSourceTilesPerDayExplanation, SourceDaysWorthOfFood, SourceForagedFoodPerDay, cachedSourceForagedFoodPerDayExplanation, SourceVisibility,
				cachedSourceVisibilityExplanation, -1f, -1f, null), null, caravan.Tile, (!caravan.vehiclePather.Moving) ? null : new int?(TicksToArrive), -9999f,
				new Rect(12f, TitleRectHeight, inRect.width - 24f, 40f), true, null, false);

				inRect.yMin += 119f;
				Widgets.DrawMenuSection(inRect);
				TabDrawer.DrawTabs(inRect, [new TabRecord("ItemsTab".Translate(), null, true)], 200f);
				inRect = inRect.ContractedBy(17f);
				Widgets.BeginGroup(inRect);
				{
					Rect groupRect = inRect.AtZero();
					DoBottomButtons(groupRect);
					Rect itemsRect = groupRect;
					itemsRect.yMax -= 59f;
					itemsTransfer.OnGUI(itemsRect, out bool anythingChange);
					if (anythingChange)
					{
						CountToTransferChanged();
					}
				}
				Widgets.EndGroup();
			}
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
			Rect acceptButtonRect = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - BottomAreaHeight, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(acceptButtonRect, "AcceptButton".Translate(), true, false, true) && TransferPawns())
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
				Close(false);
			}
			Rect resetButtonRect = new Rect(acceptButtonRect.x - 10f - BottomButtonSize.x, acceptButtonRect.y, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(resetButtonRect, "ResetButton".Translate(), true, false, false))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				CalculateAndRecacheTransferables();
			}
			Rect cancelButtonRect = new Rect(acceptButtonRect.xMax + 10f, acceptButtonRect.y, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(cancelButtonRect, "CancelButton".Translate(), true, false, true))
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

		private bool TransferPawns()
		{
			StashedVehicle stashedVehicle = StashedVehicle.Create(caravan, out _, transferables);
			return stashedVehicle != null;
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
			foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
			{
				foreach (Thing thing in vehicle.inventory.innerContainer)
				{
					AddToTransferables(thing);
				}
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
