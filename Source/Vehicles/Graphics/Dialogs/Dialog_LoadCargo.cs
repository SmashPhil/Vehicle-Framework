using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;
using Color = UnityEngine.Color;
using Rect = UnityEngine.Rect;

namespace Vehicles;

public class Dialog_LoadCargo : Window
{
  private static readonly Vector2 BottomButtonSize = new(160f, 40f);

  private readonly VehiclePawn vehicle;

  private List<TransferableOneWay> transferables = [];
  private TransferableOneWayWidget itemsTransfer;
  private bool massUsageDirty;

  private static readonly List<Pair<float, Color>> MassColor =
    [
      new(0.1f, Color.green),
      new(0.75f, Color.yellow),
      new(1f, new Color(1f, 0.6f, 0f))
    ];

  public Dialog_LoadCargo(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
    closeOnAccept = true;
    closeOnCancel = true;
    forcePause = false;
    absorbInputAroundWindow = true;
  }

  public override Vector2 InitialSize => new(1024, UI.screenHeight);

  public float MassUsage
  {
    get
    {
      if (massUsageDirty)
      {
        massUsageDirty = false;
        field = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn, true);
        field += MassUtility.GearAndInventoryMass(vehicle);
      }
      return field;
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

  // TODO 1.8 - Remove unused method
  [UsedImplicitly]
  public override void PostClose()
  {
    base.PostClose();
  }

  public override void DoWindowContents(Rect inRect)
  {
    Rect rect = new(0f, 0f, inRect.width, 35f);
    Text.Font = GameFont.Medium;
    Text.Anchor = TextAnchor.MiddleCenter;
    Widgets.Label(rect, vehicle.LabelShortCap);
    Text.Font = GameFont.Small;
    Text.Anchor = TextAnchor.UpperLeft;
    DrawCargoNumbers(new Rect(12f, 35f, inRect.width - 24f, 40f));

    Rect checkRect = new(inRect.width - 225f, 35f, 225f, 40f);
    bool checkBox = VehicleMod.settings.showAllCargoItems;
    string showAllItemsLabel = "VF_ShowAllItemsOnMap".Translate();
    Widgets.Label(checkRect, showAllItemsLabel);
    checkRect.x += Text.CalcSize(showAllItemsLabel).x + 20f;
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
    itemsTransfer.OnGUI(inRect2, out bool anythingChanged);
    if (anythingChanged)
    {
      CountToTransferChanged();
    }
    Widgets.EndGroup();
  }

  public void BottomButtons(Rect rect)
  {
    Rect acceptRect = new(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f - 17f, BottomButtonSize.x, BottomButtonSize.y);
    if (Widgets.ButtonText(acceptRect, "AcceptButton".Translate()))
    {
      Accept();
    }
    Rect resetRect = new(acceptRect.x - 10f - BottomButtonSize.x, acceptRect.y, BottomButtonSize.x, BottomButtonSize.y);
    if (Widgets.ButtonText(resetRect, "ResetButton".Translate()))
    {
      Reset();
    }
    Rect cancelRect = new(acceptRect.xMax + 10f, acceptRect.y, BottomButtonSize.x, BottomButtonSize.y);
    if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
    {
      Cancel();
    }
    if (Prefs.DevMode)
    {
      float width = 200f;
      float num = BottomButtonSize.y / 2f;
      if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f, width, num), "Dev: Pack Instantly"))
      {
        PackInstantly();
      }
      if (Widgets.ButtonText(new Rect(0f, rect.height - 55f - 17f + num, width, num), "Dev: Select everything"))
      {
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        SetToSendEverything();
      }
    }
  }

  private void Accept()
  {
    List<TransferableOneWay> cargoToTransfer = transferables.Where(t => t.CountToTransfer > 0).ToList();
    vehicle.cargoToLoad = cargoToTransfer;
    vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(vehicle, ReservationType.LoadVehicle);
    Close();
  }

  private void Reset()
  {
    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
    CalculateAndRecacheTransferables();
  }

  private void Cancel()
  {
    Close();
  }

  private void PackInstantly()
  {
    SoundDefOf.Tick_High.PlayOneShotOnCamera();
    foreach (TransferableOneWay transferable in transferables)
    {
      List<Thing> things = transferable.things;
      int countToTransfer = transferable.CountToTransfer;
      TransferableUtility.Transfer(things, countToTransfer,
        (thing, _) => vehicle.AddOrTransfer(thing));
    }
    Close(doCloseSound: false);
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
    using TextBlock textBlock = new(textColor);
    string massText = $"{"Mass".Translate()}: {MassUsage}/{MassCapacity}";
    Widgets.Label(rect, massText);
  }

  private void AddToTransferables(Thing t, bool setToTransferMax = false)
  {
    TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables,
      TransferAsOneMode.PodsOrCaravanPacking);
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
    transferables = [];
    AddItemsToTransferables();
    itemsTransfer = new TransferableOneWayWidget(transferables, null, null, null, true, ignorePawnInventoryMass: IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload,
      includePawnsMassInMassUsage: false, availableMassGetter: () => MassCapacity - MassUsage);
    CountToTransferChanged();
  }

  private void AddItemsToTransferables()
  {
    List<Thing> things = CaravanFormingUtility.AllReachableColonyItems(vehicle.Map, VehicleMod.settings.showAllCargoItems);
    foreach (Thing thing in things)
    {
      AddToTransferables(thing);
    }
  }

  private void SetToSendEverything()
  {
    foreach (TransferableOneWay transferable in transferables)
    {
      transferable.AdjustTo(transferable.GetMaximumToTransfer());
    }
    CountToTransferChanged();
  }

  private void CountToTransferChanged()
  {
    massUsageDirty = true;
  }
}
