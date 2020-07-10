using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.Sound;
using Vehicles.Defs;

namespace Vehicles
{
    public class Dialog_DockBoat : Window
    {
        public Dialog_DockBoat(Caravan caravan)
        {
            this.caravan = caravan;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(1024f, (float)Verse.UI.screenHeight);

        protected override float Margin => 0f;

        private BiomeDef Biome => this.caravan.Biome;

        private IEnumerable<Pawn> PawnsEmbarking
        {
            get
            {
                foreach (Pawn p in this.caravan.PawnsListForReading)
                {
                    if (!HelperMethods.IsBoat(p))
                        yield return p;
                }
            }
        }

        private IEnumerable<ThingCount> ItemsTaken
        {
            get
            {
                foreach(TransferableOneWay t in this.transferables)
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
                if(this.sourceMassUsageDirty)
                {
                    this.sourceMassUsageDirty = false;
                    this.cachedSourceMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.Ignore, false, false);
                }
                return this.cachedSourceMassUsage;
            }
        }

        private float SourceMassCapacity
        {
            get
            {
                if(this.sourceMassCapacityDirty)
                {
                    this.sourceMassCapacityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    float num = 0;
                    foreach(Pawn p in this.PawnsEmbarking)
                    {
                        if(MassUtility.CanEverCarryAnything(p))
                            num += MassUtility.Capacity(p, stringBuilder);
                    }
                    this.cachedSourceMassCapacity = num; //CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
                    this.cachedSourceMassCapacityExplanation = stringBuilder.ToString();
                }
                return this.cachedSourceMassCapacity;
            }
        }

        private float SourceTilesPerDay
        {
            get
            {
                if (this.sourceTilesPerDayDirty)
                {
                    this.sourceTilesPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedSourceTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(CaravanTicksPerMoveUtility.GetTicksPerMove(PawnsEmbarking.ToList(), this.SourceMassUsage, this.SourceMassCapacity, stringBuilder), this.caravan.Tile, -1, stringBuilder);
                    //this.cachedSourceTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.SourceMassUsage, this.SourceMassCapacity, this.caravan.Tile, (!this.caravan.pather.Moving) ? -1 : this.caravan.pather.nextTile, stringBuilder);
                    this.cachedSourceTilesPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedSourceTilesPerDay;
            }
        }

        private Pair<float, float> SourceDaysWorthOfFood
        {
            get
            {
                if (this.sourceDaysWorthOfFoodDirty)
                {
                    this.sourceDaysWorthOfFoodDirty = false;
                    float first;
                    float second;
                    first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.PawnsEmbarking.ToList(), ItemsTaken.ToList(), this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, Faction.OfPlayer);
                    second = DaysUntilRotCalculator.ApproxDaysUntilRotLeftAfterTransfer(this.transferables, this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, null, 0f, 3300);

                    this.cachedSourceDaysWorthOfFood = new Pair<float, float>(first, second);
                }
                return this.cachedSourceDaysWorthOfFood;
            }
        }

        private Pair<ThingDef, float> SourceForagedFoodPerDay
        {
            get
            {
                if (this.sourceForagedFoodPerDayDirty)
                {
                    this.sourceForagedFoodPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedSourceForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(this.PawnsEmbarking.ToList(), this.Biome, Faction.OfPlayer, true, false, stringBuilder);
                    this.cachedSourceForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedSourceForagedFoodPerDay;
            }
        }

        private float SourceVisibility
        {
            get
            {
                if (this.sourceVisibilityDirty)
                {
                    this.sourceVisibilityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedSourceVisibility = CaravanVisibilityCalculator.Visibility(this.PawnsEmbarking, true, stringBuilder);
                    this.cachedSourceVisibilityExplanation = stringBuilder.ToString();
                }
                return this.cachedSourceVisibility;
            }
        }

        private float DestMassUsage
        {
            get
            {
                if (this.destMassUsageDirty)
                {
                    this.destMassUsageDirty = false;
                    this.cachedDestMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.Ignore, false, false);
                }
                return this.cachedDestMassUsage;
            }
        }

        private float DestMassCapacity
        {
            get
            {
                if (this.destMassCapacityDirty)
                {
                    this.destMassCapacityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedDestMassCapacity = CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
                    this.cachedDestMassCapacityExplanation = stringBuilder.ToString();
                }
                return this.cachedDestMassCapacity;
            }
        }

        private float DestTilesPerDay
        {
            get
            {
                if (this.destTilesPerDayDirty)
                {
                    this.destTilesPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedDestTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.DestMassUsage, this.DestMassCapacity, this.caravan.Tile, (!this.caravan.pather.Moving) ? -1 : this.caravan.pather.nextTile, stringBuilder);
                    this.cachedDestTilesPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedDestTilesPerDay;
            }
        }

        private Pair<float, float> DestDaysWorthOfFood
        {
            get
            {
                if (this.destDaysWorthOfFoodDirty)
                {
                    this.destDaysWorthOfFoodDirty = false;
                    float first;
                    float second;
                    if (this.caravan.pather.Moving)
                    {
                        first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, this.caravan.Faction, this.caravan.pather.curPath, this.caravan.pather.nextTileCostLeft, this.caravan.TicksPerMove);
                        second = DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, this.caravan.pather.curPath, this.caravan.pather.nextTileCostLeft, this.caravan.TicksPerMove);
                    }
                    else
                    {
                        first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, this.caravan.Faction, null, 0f, 3300);
                        second = DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.caravan.Tile, IgnorePawnsInventoryMode.Ignore, null, 0f, 3300);
                    }
                    this.cachedDestDaysWorthOfFood = new Pair<float, float>(first, second);
                }
                return this.cachedDestDaysWorthOfFood;
            }
        }

        private Pair<ThingDef, float> DestForagedFoodPerDay
        {
            get
            {
                if (this.destForagedFoodPerDayDirty)
                {
                    this.destForagedFoodPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedDestForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(this.transferables, this.Biome, Faction.OfPlayer, stringBuilder);
                    this.cachedDestForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedDestForagedFoodPerDay;
            }
        }

        private float DestVisibility
        {
            get
            {
                if (this.destVisibilityDirty)
                {
                    this.destVisibilityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedDestVisibility = CaravanVisibilityCalculator.Visibility(this.transferables, stringBuilder);
                    this.cachedDestVisibilityExplanation = stringBuilder.ToString();
                }
                return this.cachedDestVisibility;
            }
        }

        private int TicksToArrive
        {
            get
            {
                if (!this.caravan.pather.Moving)
                {
                    return 0;
                }
                if (this.ticksToArriveDirty)
                {
                    this.ticksToArriveDirty = false;
                    this.cachedTicksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this.caravan, false);
                }
                return this.cachedTicksToArrive;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.CalculateAndRecacheTransferables();
        }

        public override void PostClose()
        {
            base.PostClose();
            if(this.caravan.PawnsListForReading.Any(x => HelperMethods.IsBoat(x)))
                HelperMethods.ToggleDocking(caravan, false);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "DockCaravan".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(this.SourceMassUsage, this.SourceMassCapacity, this.cachedSourceMassCapacityExplanation, this.SourceTilesPerDay, 
                this.cachedSourceTilesPerDayExplanation, this.SourceDaysWorthOfFood, this.SourceForagedFoodPerDay, this.cachedSourceForagedFoodPerDayExplanation, this.SourceVisibility, 
                this.cachedSourceVisibilityExplanation, -1f, -1f, null), null, this.caravan.Tile, (!this.caravan.pather.Moving) ? null : new int?(this.TicksToArrive), -9999f, 
                new Rect(12f, 35f, inRect.width - 24f, 40f), true, null, false);
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, new List<TabRecord>() { new TabRecord("ItemsTab".Translate(), null, true) }, 200f);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            this.DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            this.itemsTransfer.OnGUI(inRect2, out bool flag);
            if (flag)
                this.CountToTransferChanged();
            GUI.EndGroup();
        }

        private void AddToTransferables(Thing t)
        {
            TransferableOneWay transferable = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if(transferable is null)
            {
                transferable = new TransferableOneWay();
                this.transferables.Add(transferable);
            }
            transferable.things.Add(t);
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - this.BottomButtonSize.x / 2f, rect.height - 55f, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if(Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true) && this.DockBoatTransferPawns())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                this.Close(false);
            }
            Rect rect3 = new Rect(rect2.x - 10f - this.BottomButtonSize.x, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if(Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, false))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                this.CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if(Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                this.Close(true);
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = new List<TransferableOneWay>();
            //this.AddPawnsToTransferables();
            this.AddItemsToTransferables();
            this.CreateCaravanItemsWidget(this.transferables, out this.itemsTransfer, "SplitCaravanThingCountTip".Translate(), IgnorePawnsInventoryMode.Ignore, () => this.DestMassCapacity - this.DestMassUsage,
                false, this.caravan.Tile, false);
            this.CountToTransferChanged();
        }

        private bool DockBoatTransferPawns()
        {
            DockedBoat dockedBoat = (DockedBoat)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DockedBoat);
            dockedBoat.Tile = caravan.Tile;
            float randomInRange = Rand.Range(2f, 4f) + (50 * (1 - caravan.PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).Max(x => x.GetComp<CompVehicle>().Props.visibility)));
            dockedBoat.GetComponent<TimeoutComp>().StartTimeout(Mathf.CeilToInt(randomInRange * 60000));
            List<Pawn> boats = caravan.PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList();
            List<Pawn> pawns = caravan.PawnsListForReading.Where(x => !HelperMethods.IsBoat(x)).ToList();
            if(caravan.PawnsListForReading.Where(x => !HelperMethods.IsBoat(x)).Count() <= 0)
                return false;

            foreach(TransferableOneWay t in this.transferables)
            {
                TransferableUtility.TransferNoSplit(t.things, t.CountToTransfer, delegate(Thing thing, int numToTake)
                {
                    Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(this.caravan, thing);
                    if(ownerOf is null)
                        return;
                    CaravanInventoryUtility.MoveInventoryToSomeoneElse(ownerOf, thing, pawns, boats, numToTake);
                }, true, true);
            }

            for(int i = caravan.pawns.Count - 1; i >= 0; i--)
            {
                Pawn p = caravan.PawnsListForReading[i];
                if(HelperMethods.IsBoat(p))
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
            if(!pawns.Any( (Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
            {
                Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), this.caravan, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        private void AddItemsToTransferables()
        {
            List<Thing> list = CaravanInventoryUtility.AllInventoryItems(this.caravan);
            foreach(Thing t in list)
                this.AddToTransferables(t);
        }

        private void CountToTransferChanged()
        {
            this.sourceMassUsageDirty = true;
            this.sourceMassCapacityDirty = true;
            this.sourceTilesPerDayDirty = true;
            this.sourceDaysWorthOfFoodDirty = true;
            this.sourceForagedFoodPerDayDirty = true;
            this.sourceVisibilityDirty = true;
            this.destMassUsageDirty = true;
            this.destMassCapacityDirty = true;
            this.destTilesPerDayDirty = true;
            this.destDaysWorthOfFoodDirty = true;
            this.destForagedFoodPerDayDirty = true;
            this.destVisibilityDirty = true;
            this.ticksToArriveDirty = true;
        }

        private Caravan caravan;

        private List<TransferableOneWay> transferables = new List<TransferableOneWay>();

        private TransferableOneWayWidget itemsTransfer;

        private const float TitleRectHeight = 35f;

        private const float BottomAreaHeight = 55f;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 60f);

        private bool sourceMassUsageDirty = true;

        // Token: 0x04001CF5 RID: 7413
        private float cachedSourceMassUsage;

        // Token: 0x04001CF6 RID: 7414
        private bool sourceMassCapacityDirty = true;

        // Token: 0x04001CF7 RID: 7415
        private float cachedSourceMassCapacity;

        // Token: 0x04001CF8 RID: 7416
        private string cachedSourceMassCapacityExplanation;

        // Token: 0x04001CF9 RID: 7417
        private bool sourceTilesPerDayDirty = true;

        // Token: 0x04001CFA RID: 7418
        private float cachedSourceTilesPerDay;

        // Token: 0x04001CFB RID: 7419
        private string cachedSourceTilesPerDayExplanation;

        // Token: 0x04001CFC RID: 7420
        private bool sourceDaysWorthOfFoodDirty = true;

        // Token: 0x04001CFD RID: 7421
        private Pair<float, float> cachedSourceDaysWorthOfFood;

        // Token: 0x04001CFE RID: 7422
        private bool sourceForagedFoodPerDayDirty = true;

        // Token: 0x04001CFF RID: 7423
        private Pair<ThingDef, float> cachedSourceForagedFoodPerDay;

        // Token: 0x04001D00 RID: 7424
        private string cachedSourceForagedFoodPerDayExplanation;

        // Token: 0x04001D01 RID: 7425
        private bool sourceVisibilityDirty = true;

        // Token: 0x04001D02 RID: 7426
        private float cachedSourceVisibility;

        // Token: 0x04001D03 RID: 7427
        private string cachedSourceVisibilityExplanation;

        // Token: 0x04001D04 RID: 7428
        private bool destMassUsageDirty = true;

        // Token: 0x04001D05 RID: 7429
        private float cachedDestMassUsage;

        // Token: 0x04001D06 RID: 7430
        private bool destMassCapacityDirty = true;

        // Token: 0x04001D07 RID: 7431
        private float cachedDestMassCapacity;

        // Token: 0x04001D08 RID: 7432
        private string cachedDestMassCapacityExplanation;

        // Token: 0x04001D09 RID: 7433
        private bool destTilesPerDayDirty = true;

        // Token: 0x04001D0A RID: 7434
        private float cachedDestTilesPerDay;

        // Token: 0x04001D0B RID: 7435
        private string cachedDestTilesPerDayExplanation;

        // Token: 0x04001D0C RID: 7436
        private bool destDaysWorthOfFoodDirty = true;

        // Token: 0x04001D0D RID: 7437
        private Pair<float, float> cachedDestDaysWorthOfFood;

        // Token: 0x04001D0E RID: 7438
        private bool destForagedFoodPerDayDirty = true;

        // Token: 0x04001D0F RID: 7439
        private Pair<ThingDef, float> cachedDestForagedFoodPerDay;

        // Token: 0x04001D10 RID: 7440
        private string cachedDestForagedFoodPerDayExplanation;

        // Token: 0x04001D11 RID: 7441
        private bool destVisibilityDirty = true;

        // Token: 0x04001D12 RID: 7442
        private float cachedDestVisibility;

        // Token: 0x04001D13 RID: 7443
        private string cachedDestVisibilityExplanation;

        // Token: 0x04001D14 RID: 7444
        private bool ticksToArriveDirty = true;

        // Token: 0x04001D15 RID: 7445
        private int cachedTicksToArrive;
    }
}
