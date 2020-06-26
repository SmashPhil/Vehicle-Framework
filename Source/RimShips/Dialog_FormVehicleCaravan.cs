using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class Dialog_FormVehicleCaravan : Window
    {
        public int CurrentTile
		{
			get
			{
				return this.map.Tile;
			}
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(1024f, (float)Verse.UI.screenHeight);
			}
		}

		protected override float Margin
		{
			get
			{
				return 0f;
			}
		}

		private bool AutoStripSpawnedCorpses
		{
			get
			{
				return reform;
			}
		}

		private bool ListPlayerPawnsInventorySeparately
		{
			get
			{
				return reform;
			}
		}

		private BiomeDef Biome
		{
			get
			{
				return map.Biome;
			}
		}

		private bool MustChooseRoute
		{
			get
			{
				return canChooseRoute && (!reform || map.Parent is Settlement);
			}
		}

		private bool ShowCancelButton
		{
			get
			{
				if (!mapAboutToBeRemoved)
				{
					return true;
				}
				bool flag = false;
				for (int i = 0; i < transferables.Count; i++)
				{
					Pawn pawn = transferables[i].AnyThing as Pawn;
					if (pawn != null && pawn.IsColonist && !pawn.Downed)
					{
						flag = true;
						break;
					}
				}
				return !flag;
			}
		}

		private IgnorePawnsInventoryMode IgnoreInventoryMode
		{
			get
			{
				if (!ListPlayerPawnsInventorySeparately)
				{
					return IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
				}
				return IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn;
			}
		}

		public float MassUsage
		{
			get
			{
				if (massUsageDirty)
				{
					massUsageDirty = false;
					cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnoreInventoryMode, false, AutoStripSpawnedCorpses);
				}
				return cachedMassUsage;
			}
		}

		public float MassCapacity
		{
			get
			{
				if (massCapacityDirty)
				{
					massCapacityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedMassCapacity = CollectionsMassCalculator.CapacityTransferables(transferables, stringBuilder);
					cachedMassCapacityExplanation = stringBuilder.ToString();
				}
				return this.cachedMassCapacity;
			}
		}

		private float TilesPerDay
		{
			get
			{
				if (tilesPerDayDirty)
				{
					tilesPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(transferables, MassUsage, MassCapacity, CurrentTile, startingTile, stringBuilder);
					cachedTilesPerDayExplanation = stringBuilder.ToString();
				}
				return this.cachedTilesPerDay;
			}
		}

		private Pair<float, float> DaysWorthOfFood
		{
			get
			{
				if (this.daysWorthOfFoodDirty)
				{
					daysWorthOfFoodDirty = false;
					float first;
					float second;
					if (destinationTile != -1)
					{
						using (WorldPath worldPath = Find.WorldPathFinder.FindPath(CurrentTile, destinationTile, null, null))
						{
							int ticksPerMove = 1;// CaravanTicksPerMoveUtility.GetTicksPerMove(new CaravanTicksPerMoveUtility.CaravanInfo(this), null); REDO
							first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, CurrentTile, IgnoreInventoryMode, Faction.OfPlayer, worldPath, 0f, ticksPerMove);
							second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, CurrentTile, IgnoreInventoryMode, worldPath, 0f, ticksPerMove);
							goto IL_DB;
						}
					}
					first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(transferables, CurrentTile, IgnoreInventoryMode, Faction.OfPlayer, null, 0f, 3300);
					second = DaysUntilRotCalculator.ApproxDaysUntilRot(transferables, CurrentTile, IgnoreInventoryMode, null, 0f, 3300);
					IL_DB:
					cachedDaysWorthOfFood = new Pair<float, float>(first, second);
				}
				return cachedDaysWorthOfFood;
			}
		}

		private Pair<ThingDef, float> ForagedFoodPerDay
		{
			get
			{
				if (foragedFoodPerDayDirty)
				{
					foragedFoodPerDayDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(transferables, Biome, Faction.OfPlayer, stringBuilder);
					cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
				}
				return cachedForagedFoodPerDay;
			}
		}

		private float Visibility
		{
			get
			{
				if (visibilityDirty)
				{
					visibilityDirty = false;
					StringBuilder stringBuilder = new StringBuilder();
					cachedVisibility = CaravanVisibilityCalculator.Visibility(transferables, stringBuilder);
					cachedVisibilityExplanation = stringBuilder.ToString();
				}
				return cachedVisibility;
			}
		}

		private int TicksToArrive
		{
			get
			{
				if (destinationTile == -1)
				{
					return 0;
				}
				if (ticksToArriveDirty)
				{
					ticksToArriveDirty = false;
					using (WorldPath worldPath = Find.WorldPathFinder.FindPath(CurrentTile, destinationTile, null, null))
					{
						cachedTicksToArrive = 1;// CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this.CurrentTile, this.destinationTile, worldPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(new CaravanTicksPerMoveUtility.CaravanInfo(this), null), Find.TickManager.TicksAbs);
					}
				}
				return cachedTicksToArrive;
			}
		}

		private bool MostFoodWillRotSoon
		{
			get
			{
				float num = 0f;
				float num2 = 0f;
				for (int i = 0; i < transferables.Count; i++)
				{
					TransferableOneWay transferableOneWay = transferables[i];
					if (transferableOneWay.HasAnyThing && transferableOneWay.CountToTransfer > 0 && transferableOneWay.ThingDef.IsNutritionGivingIngestible && !(transferableOneWay.AnyThing is Corpse))
					{
						float num3 = 600f;
						CompRottable compRottable = transferableOneWay.AnyThing.TryGetComp<CompRottable>();
						if (compRottable != null)
						{
							num3 = (float)DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(compRottable, CurrentTile, null) / 60000f;
						}
						float num4 = transferableOneWay.ThingDef.GetStatValueAbstract(StatDefOf.Nutrition, null) * (float)transferableOneWay.CountToTransfer;
						if (num3 < 5f)
						{
							num += num4;
						}
						else
						{
							num2 += num4;
						}
					}
				}
				return (num != 0f || num2 != 0f) && num / (num + num2) >= 0.75f;
			}
		}

		public Dialog_FormVehicleCaravan(Map map, bool reform = false, Action onClosed = null, bool mapAboutToBeRemoved = false)
		{
			this.map = map;
			this.reform = reform;
			this.onClosed = onClosed;
			this.mapAboutToBeRemoved = mapAboutToBeRemoved;
			canChooseRoute = (!reform || !map.retainedCaravanData.HasDestinationTile);
			closeOnAccept = !reform;
			closeOnCancel = !reform;
			forcePause = true;
			absorbInputAroundWindow = true;
		}

		public override void PostOpen()
		{
			base.PostOpen();
			choosingRoute = false;
			ShipHarmony.currentFormingCaravan = this;
			if (!thisWindowInstanceEverOpened)
			{
				thisWindowInstanceEverOpened = true;
				CalculateAndRecacheTransferables();
			}
		}

		public override void PostClose()
		{
			base.PostClose();
			if (!choosingRoute)
			{
				ShipHarmony.currentFormingCaravan = null;
				HelperMethods.assignedSeats.Clear();
			}
			if (onClosed != null && !choosingRoute)
			{
				onClosed();
			}
		}

		public void Notify_NoLongerChoosingRoute()
		{
			choosingRoute = false;
			if (!Find.WindowStack.IsOpen(this) && onClosed != null)
			{
				onClosed();
			}
		}


		public override void DoWindowContents(Rect inRect)
		{
			Rect rect = new Rect(0f, 0f, inRect.width, 35f);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, (reform ? "ReformCaravan" : "FormCaravan").Translate());
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, cachedMassCapacityExplanation, TilesPerDay, cachedTilesPerDayExplanation, DaysWorthOfFood, ForagedFoodPerDay, cachedForagedFoodPerDayExplanation, Visibility, cachedVisibilityExplanation, -1f, -1f, null), null, CurrentTile, (destinationTile == -1) ? null : new int?(TicksToArrive), lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), true, (destinationTile == -1) ? null : ("\n" + "DaysWorthOfFoodTooltip_OnlyFirstWaypoint".Translate()), false);
			tabsList.Clear();

			tabsList.Add(new TabRecord("VehiclesTab".Translate(), delegate ()
			{
				this.tab = Tab.Vehicles;
			}, this.tab == Tab.Vehicles));
			tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate()
			{
				this.tab = Tab.Pawns;
			}, this.tab == Tab.Pawns));
			tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate()
			{
				this.tab = Tab.Items;
			}, this.tab == Tab.Items));
			
			inRect.yMin += 119f;
			Widgets.DrawMenuSection(inRect);
			TabDrawer.DrawTabs(inRect, tabsList, 200f);
			tabsList.Clear();
			inRect = inRect.ContractedBy(17f);
			inRect.height += 17f;
			GUI.BeginGroup(inRect);
			Rect rect2 = inRect.AtZero();
			this.DoBottomButtons(rect2);
			Rect inRect2 = rect2;
			inRect2.yMax -= 76f;
			bool flag = false;
			Tab tab = this.tab;
			if (tab == Tab.Items)
			{
				itemsTransfer.OnGUI(inRect2, out flag);
			}
			else if(tab == Tab.Vehicles)
            {
				vehiclesTransfer.OnGUI(inRect2, out flag);
            }
			else
			{
				pawnsTransfer.OnGUI(inRect2, out flag);
			}
			if (flag)
			{
				CountToTransferChanged();
			}
			GUI.EndGroup();
		}

		public override bool CausesMessageBackground()
		{
			return true;
		}

		public void Notify_ChoseRoute(int destinationTile)
		{
			if(!reform && startingTile < 0)
            {
                List<Pawn> pawns = TransferableUtility.GetPawnsFromTransferables(transferables);
                if(HelperMethods.HasBoat(pawns))
                {
                    List<int> neighboringCells = new List<int>();
                    Find.WorldGrid.GetTileNeighbors(map.Tile, neighboringCells);
                    foreach(int neighbor in neighboringCells)
                    {
                        if(HelperMethods.WaterCovered(neighbor))
                        {
                            startingTile = neighbor;
                            return;
                        }
                    }
                }
            }
			this.destinationTile = destinationTile;
			startingTile = CaravanExitMapUtility.BestExitTileToGoTo(destinationTile, map);
			ticksToArriveDirty = true;
			daysWorthOfFoodDirty = true;
			Messages.Message("MessageChoseRoute".Translate(), MessageTypeDefOf.CautionInput, false);
			soundAppear.PlayOneShotOnCamera(null);
		}

		private void AddToTransferables(Thing t, bool setToTransferMax = false)
		{
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay == null)
			{
				transferableOneWay = new TransferableOneWay();
				transferables.Add(transferableOneWay);
			}
			if (transferableOneWay.things.Contains(t))
			{
				Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t, false);
				return;
			}
			transferableOneWay.things.Add(t);
			if (setToTransferMax)
			{
				transferableOneWay.AdjustTo(transferableOneWay.CountToTransfer + t.stackCount);
			}
		}

		private void DoBottomButtons(Rect rect)
		{
			Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, true, true))
			{
				if (reform)
				{
					if (TryReformCaravan())
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
						Close(false);
					}
				}
				else
				{
					List<string> list = new List<string>();
					Pair<float, float> daysWorthOfFood = DaysWorthOfFood;
					if (daysWorthOfFood.First < 5f)
					{
						list.Add((daysWorthOfFood.First < 0.1f) ? "DaysWorthOfFoodWarningDialog_NoFood".Translate() : "DaysWorthOfFoodWarningDialog".Translate(daysWorthOfFood.First.ToString("0.#")));
					}
					else if (MostFoodWillRotSoon)
					{
						list.Add("CaravanFoodWillRotSoonWarningDialog".Translate());
					}
					if (!TransferableUtility.GetPawnsFromTransferables(transferables).Any((Pawn pawn) => CaravanUtility.IsOwner(pawn, Faction.OfPlayer) && !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled))
					{
						list.Add("CaravanIncapableOfSocial".Translate());
					}
					if (list.Count > 0)
					{
						if (CheckForErrors(TransferableUtility.GetPawnsFromTransferables(transferables)))
						{
							string str2 = string.Concat((from str in list
							select str + "\n\n").ToArray()) + "CaravanAreYouSure".Translate();
							Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(str2, delegate
							{
								if (TryFormAndSendCaravan())
								{
									Close(false);
								}
							}, false, null));
						}
					}
					else if (TryFormAndSendCaravan())
					{
						SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
						Close(false);
					}
				}
			}
			if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate(), true, true, true))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				CalculateAndRecacheTransferables();
			}
			if (ShowCancelButton && Widgets.ButtonText(new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate(), true, true, true))
			{
				Close(true);
			}
			if (canChooseRoute)
			{
				Rect rect3 = new Rect(rect.width - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y);
				if (Widgets.ButtonText(rect3, "ChooseRouteButton".Translate(), true, true, true))
				{
					List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
					soundClose.PlayOneShotOnCamera(null);
					if (!pawnsFromTransferables.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
					{
						Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
					}
					else
					{
						//Find.WorldRoutePlanner.Start(this);
					}
				}
				if (destinationTile != -1)
				{
					Rect rect4 = rect3;
					rect4.y += rect3.height + 4f;
					rect4.height = 200f;
					rect4.xMin -= 200f;
					Text.Anchor = TextAnchor.UpperRight;
					Widgets.Label(rect4, "CaravanEstimatedDaysToDestination".Translate((TicksToArrive / 60000f).ToString("0.#")));
					Text.Anchor = TextAnchor.UpperLeft;
				}
			}
			if (Prefs.DevMode)
			{
				float width = 200f;
				float num = BottomButtonSize.y / 2f;
				if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, width, num), "Dev: Send instantly", true, true, true) && DebugTryFormCaravanInstantly())
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					Close(false);
				}
				if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f + num, width, num), "Dev: Select everything", true, true, true))
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					SetToSendEverything();
				}
			}
		}

		private void CalculateAndRecacheTransferables()
		{
			transferables = new List<TransferableOneWay>();
			AddPawnsToTransferables();
			AddItemsToTransferables();
			HelperMethods.CreateVehicleCaravanTransferableWidgets(transferables, out pawnsTransfer, out vehiclesTransfer, out itemsTransfer, "FormCaravanColonyThingCountTip".Translate(), IgnoreInventoryMode, () => MassCapacity - MassUsage, AutoStripSpawnedCorpses, CurrentTile, mapAboutToBeRemoved);
			CountToTransferChanged();
		}

		private bool DebugTryFormCaravanInstantly()
		{
			List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
			if (!pawnsFromTransferables.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer)))
			{
				Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			AddItemsFromTransferablesToRandomInventories(pawnsFromTransferables);
			int num = startingTile;
			if (num < 0)
			{
				num = CaravanExitMapUtility.RandomBestExitTileFrom(map);
			}
			if (num < 0)
			{
				num = this.CurrentTile;
			}
			CaravanFormingUtility.FormAndCreateCaravan(pawnsFromTransferables, Faction.OfPlayer, CurrentTile, num, destinationTile);
			return true;
		}

		private bool TryFormAndSendCaravan()
		{
			List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
			if (!CheckForErrors(pawnsFromTransferables))
			{
				return false;
			}
			Direction8Way direction8WayFromTo = Find.WorldGrid.GetDirection8WayFromTo(CurrentTile, startingTile);
			IntVec3 intVec;
			if (!TryFindExitSpot(pawnsFromTransferables, true, out intVec))
			{
				if (!TryFindExitSpot(pawnsFromTransferables, false, out intVec))
				{
					Messages.Message("CaravanCouldNotFindExitSpot".Translate(direction8WayFromTo.LabelShort()), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				Messages.Message("CaravanCouldNotFindReachableExitSpot".Translate(direction8WayFromTo.LabelShort()), new GlobalTargetInfo(intVec, map, false), MessageTypeDefOf.CautionInput, false);
			}
			IntVec3 meetingPoint;
			if (!TryFindRandomPackingSpot(intVec, out meetingPoint))
			{
				Messages.Message("CaravanCouldNotFindPackingSpot".Translate(direction8WayFromTo.LabelShort()), new GlobalTargetInfo(intVec, map, false), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			CaravanFormingUtility.StartFormingCaravan((from x in pawnsFromTransferables
			where !x.Downed
			select x).ToList<Pawn>(), (from x in pawnsFromTransferables
			where x.Downed
			select x).ToList<Pawn>(), Faction.OfPlayer, transferables, meetingPoint, intVec, startingTile, destinationTile);
			Messages.Message("CaravanFormationProcessStarted".Translate(), pawnsFromTransferables[0], MessageTypeDefOf.PositiveEvent, false);
			return true;
		}

		private bool TryReformCaravan()
		{
			List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
			if (!CheckForErrors(pawnsFromTransferables))
			{
				return false;
			}
			AddItemsFromTransferablesToRandomInventories(pawnsFromTransferables);
			Caravan caravan = CaravanExitMapUtility.ExitMapAndCreateCaravan(pawnsFromTransferables, Faction.OfPlayer, CurrentTile, CurrentTile, destinationTile, false);
			map.Parent.CheckRemoveMapNow();
			TaggedString taggedString = "MessageReformedCaravan".Translate();
			if (caravan.pather.Moving && caravan.pather.ArrivalAction != null)
			{
				taggedString += " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.pather.ArrivalAction.Label + ".";
			}
			Messages.Message(taggedString, caravan, MessageTypeDefOf.TaskCompletion, false);
			return true;
		}

		private void AddItemsFromTransferablesToRandomInventories(List<Pawn> pawns)
		{
			transferables.RemoveAll((TransferableOneWay x) => x.AnyThing is Pawn);
			if (ListPlayerPawnsInventorySeparately)
			{
				for (int i = 0; i < pawns.Count; i++)
				{
					if (Dialog_FormCaravan.CanListInventorySeparately(pawns[i]))
					{
						ThingOwner<Thing> innerContainer = pawns[i].inventory.innerContainer;
						for (int j = innerContainer.Count - 1; j >= 0; j--)
						{
							RemoveCarriedItemFromTransferablesOrDrop(innerContainer[j], pawns[i], transferables);
						}
					}
				}
				for (int k = 0; k < transferables.Count; k++)
				{
					if (transferables[k].things.Any((Thing x) => !x.Spawned))
					{
						transferables[k].things.SortBy((Thing x) => x.Spawned);
					}
				}
			}
		}

		private bool CheckForErrors(List<Pawn> pawns)
		{
			if (MustChooseRoute && destinationTile < 0)
			{
				Messages.Message("MessageMustChooseRouteFirst".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			if (!reform && startingTile < 0)
			{
				Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			if (!pawns.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer) && !x.Downed))
			{
				Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			if (!reform && MassUsage > MassCapacity)
			{
				FlashMass();
				Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			Pawn pawn = pawns.Find((Pawn x) => !x.IsColonist && !pawns.Any((Pawn y) => y.IsColonist && y.CanReach(x, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn)));
			if (pawn != null)
			{
				Messages.Message("CaravanPawnIsUnreachable".Translate(pawn.LabelShort, pawn), pawn, MessageTypeDefOf.RejectInput, false);
				return false;
			}
			for (int i = 0; i < transferables.Count; i++)
			{
				if (transferables[i].ThingDef.category == ThingCategory.Item)
				{
					int countToTransfer = transferables[i].CountToTransfer;
					int num = 0;
					if (countToTransfer > 0)
					{
						for (int j = 0; j < transferables[i].things.Count; j++)
						{
							Thing t = transferables[i].things[j];
							if (!t.Spawned || pawns.Any((Pawn x) => x.IsColonist && x.CanReach(t, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn)))
							{
								num += t.stackCount;
								if (num >= countToTransfer)
								{
									break;
								}
							}
						}
						if (num < countToTransfer)
						{
							if (countToTransfer == 1)
							{
								Messages.Message("CaravanItemIsUnreachableSingle".Translate(transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
							}
							else
							{
								Messages.Message("CaravanItemIsUnreachableMulti".Translate(countToTransfer, transferables[i].ThingDef.label), MessageTypeDefOf.RejectInput, false);
							}
							return false;
						}
					}
				}
			}
			return true;
		}

		private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot)
		{
			Rot4 rot;
			Rot4 rot2;
			CaravanExitMapUtility.GetExitMapEdges(Find.WorldGrid, CurrentTile, startingTile, out rot, out rot2);
			return (rot != Rot4.Invalid && TryFindExitSpot(pawns, reachableForEveryColonist, rot, out spot)) || (rot2 != Rot4.Invalid && TryFindExitSpot(pawns, reachableForEveryColonist, rot2, out spot)) || TryFindExitSpot(pawns, reachableForEveryColonist, rot.Rotated(RotationDirection.Clockwise), out spot) || TryFindExitSpot(pawns, reachableForEveryColonist, rot.Rotated(RotationDirection.Counterclockwise), out spot);
		}

		private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot)
		{
			if (startingTile < 0)
			{
				Log.Error("Can't find exit spot because startingTile is not set.", false);
				spot = IntVec3.Invalid;
				return false;
			}
			Predicate<IntVec3> validator = (IntVec3 x) => !x.Fogged(map) && x.Standable(map);
			if (reachableForEveryColonist)
			{
				return CellFinder.TryFindRandomEdgeCellWith(delegate(IntVec3 x)
				{
					if (!validator(x))
					{
						return false;
					}
					for (int j = 0; j < pawns.Count; j++)
					{
						if (pawns[j].IsColonist && !pawns[j].Downed && !pawns[j].CanReach(x, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
						{
							return false;
						}
					}
					return true;
				}, map, exitDirection, CellFinder.EdgeRoadChance_Always, out spot);
			}
			IntVec3 intVec = IntVec3.Invalid;
			int num = -1;
			foreach (IntVec3 intVec2 in CellRect.WholeMap(map).GetEdgeCells(exitDirection).InRandomOrder(null))
			{
				if (validator(intVec2))
				{
					int num2 = 0;
					for (int i = 0; i < pawns.Count; i++)
					{
						if (pawns[i].IsColonist && !pawns[i].Downed && pawns[i].CanReach(intVec2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
						{
							num2++;
						}
					}
					if (num2 > num)
					{
						num = num2;
						intVec = intVec2;
					}
				}
			}
			spot = intVec;
			return intVec.IsValid;
		}

		private bool TryFindRandomPackingSpot(IntVec3 exitSpot, out IntVec3 packingSpot)
		{
			tmpPackingSpots.Clear();
			List<Thing> list = map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
			TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
			for (int i = 0; i < list.Count; i++)
			{
				if (map.reachability.CanReach(exitSpot, list[i], PathEndMode.OnCell, traverseParams))
				{
					tmpPackingSpots.Add(list[i]);
				}
			}
			if (tmpPackingSpots.Any<Thing>())
			{
				Thing thing = tmpPackingSpots.RandomElement<Thing>();
				tmpPackingSpots.Clear();
				packingSpot = thing.Position;
				return true;
			}
			return RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, map, out packingSpot);
		}

		private void AddPawnsToTransferables()
		{
			List<Pawn> list = Dialog_FormCaravan.AllSendablePawns(map, reform);
			for (int i = 0; i < list.Count; i++)
			{
				bool setToTransferMax = (reform || mapAboutToBeRemoved) && !CaravanUtility.ShouldAutoCapture(list[i], Faction.OfPlayer);
				AddToTransferables(list[i], setToTransferMax);
			}
		}

		private void AddItemsToTransferables()
		{
			List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(map, reform, reform, reform);
			for (int i = 0; i < list.Count; i++)
			{
				AddToTransferables(list[i], false);
			}
			if (AutoStripSpawnedCorpses)
			{
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].Spawned)
					{
						TryAddCorpseInventoryAndGearToTransferables(list[j]);
					}
				}
			}
			if (ListPlayerPawnsInventorySeparately)
			{
				List<Pawn> list2 = Dialog_FormCaravan.AllSendablePawns(map, reform);
				for (int k = 0; k < list2.Count; k++)
				{
					if (Dialog_FormCaravan.CanListInventorySeparately(list2[k]))
					{
						ThingOwner<Thing> innerContainer = list2[k].inventory.innerContainer;
						for (int l = 0; l < innerContainer.Count; l++)
						{
							AddToTransferables(innerContainer[l], true);
							if (AutoStripSpawnedCorpses && innerContainer[l].Spawned)
							{
								TryAddCorpseInventoryAndGearToTransferables(innerContainer[l]);
							}
						}
					}
				}
			}
		}

		private void TryAddCorpseInventoryAndGearToTransferables(Thing potentiallyCorpse)
		{
			Corpse corpse = potentiallyCorpse as Corpse;
			if (corpse != null)
			{
				AddCorpseInventoryAndGearToTransferables(corpse);
			}
		}

		private void AddCorpseInventoryAndGearToTransferables(Corpse corpse)
		{
			Pawn_InventoryTracker inventory = corpse.InnerPawn.inventory;
			Pawn_ApparelTracker apparel = corpse.InnerPawn.apparel;
			Pawn_EquipmentTracker equipment = corpse.InnerPawn.equipment;
			for (int i = 0; i < inventory.innerContainer.Count; i++)
			{
				AddToTransferables(inventory.innerContainer[i], false);
			}
			if (apparel != null)
			{
				List<Apparel> wornApparel = apparel.WornApparel;
				for (int j = 0; j < wornApparel.Count; j++)
				{
					AddToTransferables(wornApparel[j], false);
				}
			}
			if (equipment != null)
			{
				List<ThingWithComps> allEquipmentListForReading = equipment.AllEquipmentListForReading;
				for (int k = 0; k < allEquipmentListForReading.Count; k++)
				{
					AddToTransferables(allEquipmentListForReading[k], false);
				}
			}
		}

		private void RemoveCarriedItemFromTransferablesOrDrop(Thing carried, Pawn carrier, List<TransferableOneWay> transferables)
		{
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(carried, transferables, TransferAsOneMode.PodsOrCaravanPacking);
			int num;
			if (transferableOneWay == null)
			{
				num = carried.stackCount;
			}
			else if (transferableOneWay.CountToTransfer >= carried.stackCount)
			{
				transferableOneWay.AdjustBy(-carried.stackCount);
				transferableOneWay.things.Remove(carried);
				num = 0;
			}
			else
			{
				num = carried.stackCount - transferableOneWay.CountToTransfer;
				transferableOneWay.AdjustTo(0);
			}
			if (num > 0)
			{
				Thing thing = carried.SplitOff(num);
				if (carrier.SpawnedOrAnyParentSpawned)
				{
					GenPlace.TryPlaceThing(thing, carrier.PositionHeld, carrier.MapHeld, ThingPlaceMode.Near, null, null, default(Rot4));
					return;
				}
				thing.Destroy(DestroyMode.Vanish);
			}
		}

		private void FlashMass()
		{
			lastMassFlashTime = Time.time;
		}

		public static bool CanListInventorySeparately(Pawn p)
		{
			return p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer;
		}

		private void SetToSendEverything()
		{
			for (int i = 0; i < transferables.Count; i++)
			{
				transferables[i].AdjustTo(transferables[i].GetMaximumToTransfer());
			}
			CountToTransferChanged();
		}

		private void CountToTransferChanged()
		{
			massUsageDirty = true;
			massCapacityDirty = true;
			tilesPerDayDirty = true;
			daysWorthOfFoodDirty = true;
			foragedFoodPerDayDirty = true;
			visibilityDirty = true;
			ticksToArriveDirty = true;
		}

		public static List<Pawn> AllSendablePawns(Map map, bool reform)
		{
			return CaravanFormingUtility.AllSendablePawns(map, true, reform, reform, reform);
		}


		private Map map;
		private bool reform;
		private Action onClosed;
		private bool canChooseRoute;
		private bool mapAboutToBeRemoved;
		public bool choosingRoute;
		private bool thisWindowInstanceEverOpened;
		public List<TransferableOneWay> transferables;
		private TransferableVehicleWidget vehiclesTransfer;
		private TransferableOneWayWidget pawnsTransfer;
		private TransferableOneWayWidget itemsTransfer;
		private Tab tab;
		private float lastMassFlashTime = -9999f;
		private int startingTile = -1;
		private int destinationTile = -1;
		private bool massUsageDirty = true;
		private float cachedMassUsage;
		private bool massCapacityDirty = true;
		private float cachedMassCapacity;
		private string cachedMassCapacityExplanation;
		private bool tilesPerDayDirty = true;
		private float cachedTilesPerDay;
		private string cachedTilesPerDayExplanation;
		private bool daysWorthOfFoodDirty = true;
		private Pair<float, float> cachedDaysWorthOfFood;
		private bool foragedFoodPerDayDirty = true;
		private Pair<ThingDef, float> cachedForagedFoodPerDay;
		private string cachedForagedFoodPerDayExplanation;
		private bool visibilityDirty = true;
		private float cachedVisibility;
		private string cachedVisibilityExplanation;
		private bool ticksToArriveDirty = true;
		private int cachedTicksToArrive;
		private const float TitleRectHeight = 35f;
		private const float BottomAreaHeight = 55f;
		private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);
		private const float MaxDaysWorthOfFoodToShowWarningDialog = 5f;
		private static List<TabRecord> tabsList = new List<TabRecord>();
		private static List<Thing> tmpPackingSpots = new List<Thing>();


		private enum Tab
		{
			Vehicles,
			Pawns,
			Items
		}
    }
}
