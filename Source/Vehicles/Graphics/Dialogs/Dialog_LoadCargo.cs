using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class Dialog_LoadCargo : Window
	{
		private VehiclePawn vehicle;

		private List<TransferableOneWay> transferables = new List<TransferableOneWay>();
		private TransferableOneWayWidget itemsTransfer;
		private bool massUsageDirty;

		private float cachedMassUsage;
		private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

		private static readonly List<Pair<float, Color>> MassColor = new List<Pair<float, Color>>
		{
			new Pair<float, Color>(0.1f, Color.green),
			new Pair<float, Color>(0.75f, Color.yellow),
			new Pair<float, Color>(1f, new Color(1f, 0.6f, 0f))
		};

		public Dialog_LoadCargo(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			closeOnAccept = true;
			closeOnCancel = true;
			forcePause = false;
			absorbInputAroundWindow = true;
		}

		public override Vector2 InitialSize => new Vector2(1024, Verse.UI.screenHeight);

		public float MassUsage
		{
			get
			{
				if (massUsageDirty)
				{
					massUsageDirty = false;
					cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn, true);
					cachedMassUsage += MassUtility.GearAndInventoryMass(vehicle);
				}
				return cachedMassUsage;
			}
		}

		public float MassCapacity
		{
			get
			{
				return vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			}
		}

		public override void PostOpen()
		{
			base.PostOpen();
			massUsageDirty = true;
			CalculateAndRecacheTransferables();
		}

		public override void PostClose()
		{
			base.PostClose();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect rect = new Rect(0f, 0f, inRect.width, 35f);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, vehicle.LabelShortCap);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			DrawCargoNumbers(new Rect(12f, 35f, inRect.width - 24f, 40f));

			Rect checkRect = new Rect(inRect.width - 225f, 35f, 225f, 40f);
			bool checkBox = VehicleMod.settings.showAllCargoItems;
			Widgets.Label(checkRect, "ShowAllItemsOnMap".Translate());
			checkRect.x += Text.CalcSize("ShowAllItemsOnMap".Translate()).x + 20f;
			Widgets.Checkbox(new Vector2(checkRect.x, checkRect.y), ref VehicleMod.settings.showAllCargoItems);
			if (checkBox != VehicleMod.settings.showAllCargoItems)
			{
				CalculateAndRecacheTransferables();
			}
			inRect.yMin += 60;
			Widgets.DrawMenuSection(inRect);
			inRect = inRect.ContractedBy(17f);
			Widgets.BeginGroup(inRect);
			Rect bottomRect = inRect.AtZero();
			BottomButtons(bottomRect);
			Rect inRect2 = bottomRect;
			inRect2.yMax -= 76f;
			itemsTransfer.OnGUI(inRect2, out bool flag);
			if (flag)
				CountToTransferChanged();
			Widgets.EndGroup();
		}

		public void BottomButtons(Rect rect)
		{
			Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y);
			if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, true, true))
			{
				List<TransferableOneWay> cargoToTransfer = transferables.Where(t => t.CountToTransfer > 0).ToList();
				vehicle.cargoToLoad = cargoToTransfer;
				vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(vehicle, ReservationType.LoadVehicle);
				Close(true);
			}
			if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate(), true, true, true))
			{
				SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				CalculateAndRecacheTransferables();
			}
			if (Widgets.ButtonText(new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate(), true, true, true))
			{
				Close(true);
			}
			if (Prefs.DevMode)
			{
				float width = 200f;
				float num = BottomButtonSize.y / 2f;
				if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, width, num), "Dev: Pack Instantly", true, true, true))
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					for(int i = 0; i < transferables.Count; i++)
					{
						List<Thing> things = transferables[i].things;
						int countToTransfer = transferables[i].CountToTransfer;
						Action<Thing, IThingHolder> transferred = null;
						if(transferred is null)
						{
							transferred = delegate(Thing thing, IThingHolder originalHolder)
							{
								vehicle.inventory.innerContainer.TryAdd(thing, true);
							};
						}
						TransferableUtility.Transfer(things, countToTransfer, transferred);
					}
					Close(false);
				}
				if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f + num, width, num), "Dev: Select everything", true, true, true))
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
					SetToSendEverything();
				}
			}
		}

		public void DrawCargoNumbers(Rect rect)
		{
			Color textColor;
			if (MassUsage > MassCapacity)
			{
				textColor = Color.red;
			}
			else if (MassCapacity == 0)
			{
				textColor = Color.grey;
			}
			else
			{
				textColor = GenUI.LerpColor(MassColor, MassUsage / MassCapacity);
			}
			var color = GUI.color;
			GUI.color = textColor;
			string massText = string.Format("Mass: {0}/{1}", MassUsage, MassCapacity);
			Widgets.Label(rect, massText);
			GUI.color = color;
		}

		private void AddToTransferables(Thing t, bool setToTransferMax = false)
		{
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay == null)
			{
				transferableOneWay = new TransferableOneWay();
				transferables.Add(transferableOneWay);
			}
			if (transferableOneWay.things.Contains(t))
			{
				Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
				return;
			}
			transferableOneWay.things.Add(t);
			if (setToTransferMax)
			{
				transferableOneWay.AdjustTo(transferableOneWay.CountToTransfer + t.stackCount);
			}
		}

		private void CalculateAndRecacheTransferables()
		{
			transferables = new List<TransferableOneWay>();
			AddItemsToTransferables();
			itemsTransfer = new TransferableOneWayWidget(transferables, null, null, null, true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn);
			CountToTransferChanged();
		}

		private void AddItemsToTransferables()
		{
			List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(vehicle.Map, VehicleMod.settings.showAllCargoItems, false, false);
			for (int i = 0; i < list.Count; i++)
			{
				AddToTransferables(list[i], false);
			}
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
		}
	}
}
