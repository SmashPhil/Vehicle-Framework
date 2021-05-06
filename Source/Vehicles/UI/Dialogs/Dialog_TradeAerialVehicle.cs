using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class Dialog_TradeAerialVehicle : Window
	{
		private const float TopAreaHeight = 58f;
		private const float FirstCommodityY = 6f;
		private const float RowInterval = 30f;
		private const float SpaceBetweenTraderNameAndTraderKind = 27f;
		private const float ShowSellableItemsIconSize = 32f;
		private const float GiftModeIconSize = 32f;
		private const float TradeModeIconSize = 32f;

		private AerialVehicleInFlight aerialVehicle;

		private bool giftsOnly;
		private Vector2 scrollPosition = Vector2.zero;
		public static float lastCurrencyFlashTime = -100f;

		private List<Tradeable> cachedTradeables;
		private Tradeable cachedCurrencyTradeable;
		private TransferableSorterDef sorter1;
		private TransferableSorterDef sorter2;
		private List<Thing> playerVehicleAllPawnsAndItems;

		private bool massUsageDirty = true;
		private float cachedMassUsage;
		private bool massCapacityDirty = true;
		private float cachedMassCapacity;
		private string cachedMassCapacityExplanation;

		protected static readonly Vector2 AcceptButtonSize = new Vector2(160f, 40f);
		protected static readonly Vector2 OtherBottomButtonSize = new Vector2(160f, 40f);
		private static readonly Texture2D ShowSellableItemsIcon = ContentFinder<Texture2D>.Get("UI/Commands/SellableItems", true);
		private static readonly Texture2D GiftModeIcon = ContentFinder<Texture2D>.Get("UI/Buttons/GiftMode", true);
		private static readonly Texture2D TradeModeIcon = ContentFinder<Texture2D>.Get("UI/Buttons/TradeMode", true);

		public Dialog_TradeAerialVehicle(AerialVehicleInFlight aerialVehicle, Pawn playerNegotiator, ITrader trader, bool giftsOnly = false)
		{
			this.aerialVehicle = aerialVehicle;
			this.giftsOnly = giftsOnly;
			TradeSession.SetupWith(trader, playerNegotiator, giftsOnly);
			SetupPlayerCaravanVariables();
			forcePause = true;
			absorbInputAroundWindow = true;
			soundAppear = SoundDefOf.CommsWindow_Open;
			soundClose = SoundDefOf.CommsWindow_Close;
			if (trader is PassingShip)
			{
				soundAmbient = SoundDefOf.RadioComms_Ambience;
			}
			sorter1 = TransferableSorterDefOf.Category;
			sorter2 = TransferableSorterDefOf.MarketValue;
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(1024f, Verse.UI.screenHeight);
			}
		}

		private float MassUsage
		{
			get
			{
				if (massUsageDirty)
				{
					massUsageDirty = false;
					TradeSession.deal.UpdateCurrencyCount();
					if (cachedCurrencyTradeable != null)
					{
						cachedTradeables.Add(cachedCurrencyTradeable);
					}
					cachedMassUsage = CollectionsMassCalculator.MassUsageLeftAfterTradeableTransfer(playerVehicleAllPawnsAndItems, cachedTradeables, IgnorePawnsInventoryMode.Ignore, false, false);
					if (cachedCurrencyTradeable != null)
					{
						cachedTradeables.RemoveLast();
					}
				}
				return cachedMassUsage;
			}
		}

		private float MassCapacity
		{
			get
			{
				if (massCapacityDirty)
				{
					massCapacityDirty = false;
					TradeSession.deal.UpdateCurrencyCount();
					if (cachedCurrencyTradeable != null)
					{
						cachedTradeables.Add(cachedCurrencyTradeable);
					}
					StringBuilder stringBuilder = new StringBuilder();
					cachedMassCapacity = CollectionsMassCalculator.CapacityLeftAfterTradeableTransfer(playerVehicleAllPawnsAndItems, cachedTradeables, stringBuilder);
					cachedMassCapacityExplanation = stringBuilder.ToString();
					if (cachedCurrencyTradeable != null)
					{
						cachedTradeables.RemoveLast();
					}
				}
				return cachedMassCapacity;
			}
		}

		public override void PostOpen()
		{
			base.PostOpen();
			if (!giftsOnly)
			{
				Pawn playerNegotiator = TradeSession.playerNegotiator;
				float level = playerNegotiator.health.capacities.GetLevel(PawnCapacityDefOf.Talking);
				float level2 = playerNegotiator.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
				if (level < 0.95f || level2 < 0.95f)
				{
					TaggedString taggedString;
					if (level < 0.95f)
					{
						taggedString = "NegotiatorTalkingImpaired".Translate(playerNegotiator.LabelShort, playerNegotiator);
					}
					else
					{
						taggedString = "NegotiatorHearingImpaired".Translate(playerNegotiator.LabelShort, playerNegotiator);
					}
					taggedString += "\n\n" + "NegotiatorCapacityImpaired".Translate();
					Find.WindowStack.Add(new Dialog_MessageBox(taggedString, null, null, null, null, null, false, null, null));
				}
			}
			CacheTradeables();
		}

		private void CacheTradeables()
		{
			cachedCurrencyTradeable = TradeSession.deal.AllTradeables.FirstOrDefault((Tradeable x) => x.IsCurrency && (TradeSession.TradeCurrency != TradeCurrency.Favor || x.IsFavor));
			cachedTradeables = (from tr in TradeSession.deal.AllTradeables
			where !tr.IsCurrency && (tr.TraderWillTrade || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade)
			select tr).OrderByDescending(delegate(Tradeable tr)
			{
				if (!tr.TraderWillTrade)
				{
					return -1;
				}
				return 0;
			}).ThenBy((Tradeable tr) => tr, sorter1.Comparer).ThenBy((Tradeable tr) => tr, sorter2.Comparer).ThenBy((Tradeable tr) => TransferableUIUtility.DefaultListOrderPriority(tr)).ThenBy((Tradeable tr) => tr.ThingDef.label).ThenBy(delegate(Tradeable tr)
			{
				QualityCategory result;
				if (tr.AnyThing.TryGetQuality(out result))
				{
					return (int)result;
				}
				return -1;
			}).ThenBy((Tradeable tr) => tr.AnyThing.HitPoints).ToList<Tradeable>();
		}

		public override void DoWindowContents(Rect inRect)
		{
			TradeSession.deal.UpdateCurrencyCount();
			GUI.BeginGroup(inRect);
			inRect = inRect.AtZero();
			TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, delegate(TransferableSorterDef x)
			{
				sorter1 = x;
				CacheTradeables();
			}, delegate(TransferableSorterDef x)
			{
				sorter2 = x;
				CacheTradeables();
			});
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(0f, SpaceBetweenTraderNameAndTraderKind, inRect.width / 2f, inRect.height / 2f), "NegotiatorTradeDialogInfo".Translate(TradeSession.playerNegotiator.Name.ToStringFull, TradeSession.playerNegotiator.GetStatValue(StatDefOf.TradePriceImprovement, true).ToStringPercent()));
			float num = inRect.width - 590f;
			Rect position = new Rect(num, 0f, inRect.width - num, TopAreaHeight);
			GUI.BeginGroup(position);
			Text.Font = GameFont.Medium;
			Rect rect = new Rect(0f, 0f, position.width / 2f, position.height);
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(rect, Faction.OfPlayer.Name.Truncate(rect.width, null));
			Rect rect2 = new Rect(position.width / 2f, 0f, position.width / 2f, position.height);
			Text.Anchor = TextAnchor.UpperRight;
			string text = TradeSession.trader.TraderName;
			if (Text.CalcSize(text).x > rect2.width)
			{
				Text.Font = GameFont.Small;
				text = text.Truncate(rect2.width, null);
			}
			Widgets.Label(rect2, text);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperRight;
			Widgets.Label(new Rect(position.width / 2f, SpaceBetweenTraderNameAndTraderKind, position.width / 2f, position.height / 2f), TradeSession.trader.TraderKind.LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;
			if (!TradeSession.giftMode)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.6f);
				Text.Font = GameFont.Tiny;
				Rect rect3 = new Rect(position.width / 2f - 100f - RowInterval, 0f, 200f, position.height);
				Text.Anchor = TextAnchor.LowerCenter;
				Widgets.Label(rect3, "PositiveBuysNegativeSells".Translate());
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
			GUI.EndGroup();
			float num2 = 0f;
			if (cachedCurrencyTradeable != null)
			{
				float num3 = inRect.width - 16f;
				TradeUI.DrawTradeableRow(new Rect(0f, TopAreaHeight, num3, RowInterval), cachedCurrencyTradeable, 1);
				GUI.color = Color.gray;
				Widgets.DrawLineHorizontal(0f, 87f, num3);
				GUI.color = Color.white;
				num2 = RowInterval;
			}
			Rect mainRect = new Rect(0f, TopAreaHeight + num2, inRect.width, inRect.height - TopAreaHeight - 38f - num2 - 20f);
			FillMainRect(mainRect);
			Rect rect4 = new Rect(inRect.width / 2f - AcceptButtonSize.x / 2f, inRect.height - 55f, AcceptButtonSize.x, AcceptButtonSize.y);
			if (Widgets.ButtonText(rect4, TradeSession.giftMode ? ("OfferGifts".Translate() + " (" + FactionGiftUtility.GetGoodwillChange(TradeSession.deal.AllTradeables, TradeSession.trader.Faction).ToStringWithSign() + ")") : "AcceptButton".Translate(), true, true, true))
			{
				void action()
				{
					if (TradeSession.deal.TryExecute(out bool flag))
					{
						if (flag)
						{
							SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
							Close(false);
							return;
						}
						Close(true);
					}
				}
				if (TradeSession.deal.DoesTraderHaveEnoughSilver())
				{
					action();
				}
				else
				{
					FlashSilver();
					SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
					Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmTraderShortFunds".Translate(), action, false, null));
				}
				Event.current.Use();
			}
			if (Widgets.ButtonText(new Rect(rect4.x - 10f - OtherBottomButtonSize.x, rect4.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y), "ResetButton".Translate(), true, true, true))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				TradeSession.deal.Reset();
				CacheTradeables();
				CountToTransferChanged();
			}
			if (Widgets.ButtonText(new Rect(rect4.xMax + 10f, rect4.y, OtherBottomButtonSize.x, OtherBottomButtonSize.y), "CancelButton".Translate(), true, true, true))
			{
				this.Close(true);
				Event.current.Use();
			}
			float y = OtherBottomButtonSize.y;
			Rect rect5 = new Rect(inRect.width - y, rect4.y, y, y);
			if (Widgets.ButtonImageWithBG(rect5, ShowSellableItemsIcon, new Vector2?(new Vector2(ShowSellableItemsIconSize, ShowSellableItemsIconSize))))
			{
				Find.WindowStack.Add(new Dialog_SellableItems(TradeSession.trader));
			}
			TooltipHandler.TipRegionByKey(rect5, "CommandShowSellableItemsDesc");
			Faction faction = TradeSession.trader.Faction;
			if (faction != null && !giftsOnly && !faction.def.permanentEnemy)
			{
				Rect rect6 = new Rect(rect5.x - y - 4f, rect4.y, y, y);
				if (TradeSession.giftMode)
				{
					if (Widgets.ButtonImageWithBG(rect6, TradeModeIcon, new Vector2?(new Vector2(TradeModeIconSize, TradeModeIconSize))))
					{
						TradeSession.giftMode = false;
						TradeSession.deal.Reset();
						CacheTradeables();
						CountToTransferChanged();
						SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					}
					TooltipHandler.TipRegionByKey(rect6, "TradeModeTip");
				}
				else
				{
					if (Widgets.ButtonImageWithBG(rect6, GiftModeIcon, new Vector2?(new Vector2(GiftModeIconSize, GiftModeIconSize))))
					{
						TradeSession.giftMode = true;
						TradeSession.deal.Reset();
						CacheTradeables();
						CountToTransferChanged();
						SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					}
					TooltipHandler.TipRegionByKey(rect6, "GiftModeTip", faction.Name);
				}
			}
			GUI.EndGroup();
		}

		public override void Close(bool doCloseSound = true)
		{
			DragSliderManager.ForceStop();
			base.Close(doCloseSound);
		}

		private void FillMainRect(Rect mainRect)
		{
			Text.Font = GameFont.Small;
			float height = FirstCommodityY + cachedTradeables.Count * RowInterval;
			Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
			Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect, true);
			float num = FirstCommodityY;
			float num2 = scrollPosition.y - RowInterval;
			float num3 = scrollPosition.y + mainRect.height;
			int num4 = 0;
			for (int i = 0; i < cachedTradeables.Count; i++)
			{
				if (num > num2 && num < num3)
				{
					Rect rect = new Rect(0f, num, viewRect.width, RowInterval);
					int countToTransfer = cachedTradeables[i].CountToTransfer;
					TradeUI.DrawTradeableRow(rect, cachedTradeables[i], num4);
					if (countToTransfer != cachedTradeables[i].CountToTransfer)
					{
						CountToTransferChanged();
					}
				}
				num += RowInterval;
				num4++;
			}
			Widgets.EndScrollView();
		}

		public void FlashSilver()
		{
			lastCurrencyFlashTime = Time.time;
		}

		public override bool CausesMessageBackground()
		{
			return true;
		}

		private void SetupPlayerCaravanVariables()
		{
			playerVehicleAllPawnsAndItems = new List<Thing>(aerialVehicle.vehicle.AllPawnsAboard);
			foreach (Pawn pawn in aerialVehicle.vehicle.AllPawnsAboard)
			{
				playerVehicleAllPawnsAndItems.AddRange(pawn.inventory.innerContainer);
			}
			playerVehicleAllPawnsAndItems.AddRange(aerialVehicle.vehicle.inventory.innerContainer);
		}

		private void CountToTransferChanged()
		{
			massUsageDirty = true;
			massCapacityDirty = true;
		}
	}
}
