using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles.World;

[StaticConstructorOnStartup]
public sealed class TransferableVehicleWidget : IDisposable
{
  private const float TopAreaHeight = 37f;
  private const int ColumnCount = 4;
  private const float CardHeight = 300;
  private const float LabelHeight = 30;
  private const float CardIconSize = 150;
  private const float CardSpacing = 5;
  private const float CardContentPadding = 5;

  private const float FirstTransferableY = 6f;
  private const float ExtraSpaceAfterSectionTitle = 5f;

  // HostilityResponseModeUtility::FleeIcon
  private static readonly Texture2D PawnIcon =
    ContentFinder<Texture2D>.Get("UI/Icons/HostilityResponse/Flee");

  private static readonly Texture2D EfficiencyHillsIcon = ContentFinder<Texture2D>.Get("UI/Icons/EfficiencyHills");
  private static readonly Texture2D EfficiencyRiverIcon = ContentFinder<Texture2D>.Get("UI/Icons/EfficiencyRiver");
  private static readonly Texture2D EfficiencyRoadsIcon = ContentFinder<Texture2D>.Get("UI/Icons/EfficiencyRoads");

  private static readonly Rect SortersRect = new(0f, 0f, 350f, 27f);
  private static readonly Color CardColor = new(1f, 1f, 1f, 0.04f);

  private static readonly List<TransferableSorterDef> AllSorterDefs = [];

  private static bool showVehicleProps = true;

  private readonly Section vehicleSection;
  private readonly List<TransferableOneWay> pawns;
  private readonly PlanetTile tile;
  private bool transferablesCached;

  private readonly TransferableSorter sortPrimary;
  private readonly TransferableSorter sortSecondary;

  private readonly HashSet<VehicleDef> impassableOnTile = [];

  private Vector2 scrollPosition;

  static TransferableVehicleWidget()
  {
    AllSorterDefs.Add(DefDatabase<TransferableSorterDef>.GetNamed("None"));
    AllSorterDefs.Add(DefDatabase<TransferableSorterDef>.GetNamed("Name"));
    AllSorterDefs.Add(TransferableSorterDefOf.MarketValue);
    AllSorterDefs.AddRange(DefDatabase<TransferableVehicleSorterDef>.AllDefsListForReading);
  }

  public TransferableVehicleWidget(string title, List<TransferableOneWay> vehicles,
    List<TransferableOneWay> pawns, PlanetTile tile = default)
  {
    vehicleSection = new Section
    {
      title = title,
      transferables = vehicles
    };
    vehicleSection.portraits = new List<VehiclePortrait>(vehicles.Count);
    for (int i = 0; i < vehicles.Count; i++)
    {
      vehicleSection.portraits.Add(new VehiclePortrait());
    }

    this.pawns = pawns;
    this.tile = tile;

    sortPrimary = new TransferableSorter(this, TransferableVehicleSorterDefOf.Type);
    sortSecondary = new TransferableSorter(this, TransferableVehicleSorterDefOf.CargoCapacity);

    Init();
  }

  private float Height { get; set; } = -1;

  private bool AnyTransferable
  {
    get
    {
      if (!transferablesCached)
      {
        CacheTransferables();
      }
      return vehicleSection.SortedTransferables.Count > 0;
    }
  }

  private void Init()
  {
    Assert.IsNotNull(vehicleSection);

    transferablesCached = false;

    if (!vehicleSection.transferables.NullOrEmpty())
    {
      WorldVehiclePathGrid worldVehiclePathGrid = Find.World.GetComponent<WorldVehiclePathGrid>();
      foreach (TransferableOneWay transferable in vehicleSection.transferables)
      {
        VehicleDef vehicleDef = transferable.ThingDef as VehicleDef;
        Assert.IsNotNull(vehicleDef, "Non-vehicle transferable in vehicles section.");
        if (!worldVehiclePathGrid.Passable(tile, vehicleDef))
        {
          impassableOnTile.Add(vehicleDef);
        }
      }
    }
  }

  public void Dispose()
  {
    vehicleSection.Dispose();
  }

  private int CompareTransferables(TransferableOneWay left, TransferableOneWay right)
  {
    int result = (!CanCaravan(left, out _)).CompareTo(!CanCaravan(right, out _));
    if (result != 0)
      return result;

    result = sortPrimary.sorterDef.Comparer.Compare(left, right);
    return result != 0 ? result : sortSecondary.sorterDef.Comparer.Compare(left, right);
  }

  private void CacheTransferables()
  {
    transferablesCached = true;
    vehicleSection.Sort(CompareTransferables);

    RecalculateHeight();
  }

  private void RecalculateHeight()
  {
    float height = FirstTransferableY;
    height += Mathf.CeilToInt(vehicleSection.SortedTransferables.Count / (float)ColumnCount) *
      CardHeight;
    if (vehicleSection.title != null)
      height += LabelHeight + ExtraSpaceAfterSectionTitle;
    Height = height;
  }

  public void OnGUI(Rect inRect)
  {
    if (!transferablesCached)
      CacheTransferables();

    using TextBlock textBlock = new(GameFont.Small);
    DoTransferableSorters(sortPrimary.sorterDef, sortSecondary.sorterDef, sortPrimary.Sort,
      sortSecondary.Sort);

    Rect mainRect = new(inRect.x, inRect.y + TopAreaHeight, inRect.width, inRect.height - TopAreaHeight);

    if (IsFeatureEnabled(VehicleCaravanProps))
    {
      string checkboxLabel = "VF_ShowVehicleProperties".Translate();
      float labelWidth = Text.CalcSize(checkboxLabel).x;
      Rect checkboxRect = new(mainRect.xMax - labelWidth - UIElements.CheckboxSize, mainRect.y, labelWidth,
        TopAreaHeight);
      UIElements.CheckboxLabeled(checkboxRect, checkboxLabel, ref showVehicleProps);
    }
    FillMainRect(mainRect);
  }

  private bool CanCaravan(TransferableOneWay transferable, out string disableReason)
  {
    VehicleDef vehicleDef = transferable.ThingDef as VehicleDef;
    Assert.IsNotNull(vehicleDef);
    if (impassableOnTile.Contains(vehicleDef))
    {
      disableReason = "VF_ImpassableBiome";
      return false;
    }
    if (CaravanFormation.Current is not { AllowSelectionOfAllVehicles: true })
    {
      if (!vehicleDef.canCaravan)
      {
        disableReason = "VF_CaravanDisabled";
        return false;
      }
      if (transferable.AnyThing is VehiclePawn { CanMove: false })
      {
        disableReason = "VF_CaravanCantMove";
        return false;
      }
    }
    disableReason = null;
    return true;
  }

  private void FillMainRect(Rect mainRect)
  {
    if (!AnyTransferable)
    {
      using TextBlock colorBlock = new(TextAnchor.UpperCenter, Color.gray);
      Widgets.Label(mainRect, "NoneBrackets".Translate());
      return;
    }

    using TextBlock fontBlock = new(GameFont.Small);
    float curY = FirstTransferableY;
    float bottomLimit = scrollPosition.y - CardHeight;
    float topLimit = scrollPosition.y + mainRect.height;

    Rect viewRect = new(0f, 0f, mainRect.width - GenUI.ScrollBarWidth, Height);
    Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
    float cardWidth = viewRect.width / ColumnCount;

    if (vehicleSection.SortedTransferables.NullOrEmpty())
      return;

    if (vehicleSection.title != null)
    {
      Widgets.ListSeparator(ref curY, viewRect.width, vehicleSection.title);
      curY += ExtraSpaceAfterSectionTitle;
    }
    for (int i = 0; i < vehicleSection.SortedTransferables.Count; i++)
    {
      VehiclePortrait portrait = vehicleSection.portraits[i];
      TransferableOneWay transferable = vehicleSection.SortedTransferables[i];
      if (curY > bottomLimit && curY < topLimit)
      {
        int column = i % ColumnCount;
        Rect rect = new(column * cardWidth, curY, cardWidth, CardHeight);

        Widgets.BeginGroup(rect);
        rect = rect.AtZero().ContractedBy(CardSpacing / 2f);
        Widgets.DrawBoxSolidWithOutline(rect, CardColor, Widgets.SeparatorLineColor);
        DrawCard(rect.ContractedBy(CardContentPadding), portrait, transferable);
        Widgets.EndGroup();
      }

      if ((i + 1) % ColumnCount == 0)
      {
        curY += CardHeight;
      }
    }
    Widgets.EndScrollView();
  }

  private void DrawCard(Rect rect, VehiclePortrait portrait, TransferableOneWay transferable)
  {
    const float Margin = 15;
    const float CheckboxSize = 24;

    VehiclePawn vehicle = transferable.AnyThing as VehiclePawn;
    VehicleDef vehicleDef = transferable.ThingDef as VehicleDef;

    Assert.IsNotNull(vehicleDef);
    bool canCaravan = CanCaravan(transferable, out string disableReason);

    Rect iconBar = rect with { height = CardIconSize };
    Rect iconRect = iconBar.ToSquare();

    string label = vehicle?.LabelCap ?? vehicleDef.LabelCap;

    // Assign seats checkbox
    bool checkOn = transferable.CountToTransfer > 0;
    Rect checkboxRect = new(iconBar.xMax - CheckboxSize, iconBar.y, CheckboxSize, CheckboxSize);
    Widgets.Checkbox(checkboxRect.position, ref checkOn, disabled: !canCaravan, size: CheckboxSize);

    if (!canCaravan)
      TooltipHandler.TipRegionByKey(checkboxRect, disableReason);

    if (checkOn != transferable.CountToTransfer > 0)
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      if (checkOn)
      {
        if (vehicle != null)
          Find.WindowStack.Add(new Dialog_AssignSeats(CaravanFormation.Current, pawns, transferable));
        else
          transferable.ForceTo(transferable.GetMaximumToTransfer());
      }
      else
      {
        transferable.ForceTo(0);
        if (vehicle != null)
        {
          foreach (AssignedSeat seat in CaravanHelper.assignedSeats.GetAssignments(vehicle))
          {
            TransferableOneWay pawnTransferable =
              pawns.FirstOrDefault(trnsf => trnsf.AnyThing == seat.pawn);
            if (pawnTransferable != null && !pawnTransferable.AnyThing.InVehicle())
              pawnTransferable.ForceTo(0);
          }
          // Update all onboard pawns set as readonly transferables
          foreach (Pawn pawn in vehicle.AllPawnsAboard)
          {
            TransferableOneWay pawnTransferable =
              pawns.FirstOrDefault(trnsf => trnsf.AnyThing == pawn);
            Assert.IsNotNull(pawnTransferable);
            pawnTransferable.ForceTo(0);
          }
          CaravanHelper.assignedSeats.RemoveAssignments(vehicle);
          CaravanFormation.Current.NotifyTransferablesChanged();
        }
      }
    }

    if (showVehicleProps && IsFeatureEnabled(VehicleCaravanProps))
    {
      DrawSpecialProperties(rect, vehicleDef, vehicle);
    }

    BlitRequest request = vehicle != null ? BlitRequest.For(vehicle) : BlitRequest.For(vehicleDef);
    portrait.Draw(iconRect, in request);

    float textHeight = Text.CalcHeight(label, iconBar.width);
    Rect labelRect = new(iconBar.x, iconRect.yMax - textHeight, iconBar.width, textHeight);
    using (new TextBlock(GameFont.Small, TextAnchor.UpperCenter, true))
    {
      Widgets.Label(labelRect, label);
    }
    Widgets.DrawLineHorizontal(rect.x, iconRect.yMax, rect.width, Widgets.SeparatorLineColor);

    Rect infoRect = (rect with { yMin = iconRect.yMax }).ContractedBy(Margin, 0);
    infoRect.yMin += 10;
    DrawVehicleInfo(infoRect, transferable);
  }

  private static void DrawVehicleInfo(Rect infoRect, TransferableOneWay transferable)
  {
    const float LinePadding = 2;

    VehiclePawn vehicle = transferable.AnyThing as VehiclePawn;
    VehicleDef vehicleDef = transferable.ThingDef as VehicleDef;
    Assert.IsNotNull(vehicleDef);

    Rect lineRect = infoRect with { height = Text.LineHeight };
    DrawMoveSpeed(lineRect, transferable);
    lineRect.y += lineRect.height + LinePadding;

    //DrawMass(rect, transferable, availableMass);
    //lineRect.y += lineRect.height * LinePadding;

    DrawCargoCapacity(lineRect, transferable);
    lineRect.y += lineRect.height + LinePadding;

    if (vehicleDef.tradeability is Tradeability.Sellable or Tradeability.All)
    {
      DrawMarketValue(lineRect, transferable);
      lineRect.y += lineRect.height + LinePadding;
    }
    if (vehicle != null)
    {
      foreach (ThingComp comp in vehicle.AllComps)
      {
        if (comp is VehicleComp vehicleComp)
        {
          float heightUsed = vehicleComp.CompStatCard(lineRect);
          if (heightUsed > 0)
            lineRect.y += heightUsed;
        }
      }
    }
  }

  private static void DrawSpecialProperties(Rect rect, VehicleDef vehicleDef, VehiclePawn vehicle)
  {
    const float SpecialPropOffset = 5;
    const float SpecialPropIconSize = 32;

    Rect iconRect = new(rect.x + SpecialPropOffset, rect.y + SpecialPropOffset, SpecialPropIconSize,
      SpecialPropIconSize);
    if (vehicle != null)
    {
      DrawIcon(ref iconRect, EfficiencyHillsIcon, vehicle.PawnCountToOperate.ToString());
      DrawIcon(ref iconRect, EfficiencyRiverIcon, vehicle.PawnsByHandlingType[HandlingType.None].Count.ToString());
      DrawIcon(ref iconRect, EfficiencyRoadsIcon, vehicle.PawnsByHandlingType[HandlingType.None].Count.ToString());
    }
    else
    {
      int movementSlots = vehicleDef.properties.RoleSeats(HandlingType.Movement);
      int nonMovementSlots = vehicleDef.properties.TotalSeats - movementSlots;
      DrawIcon(ref iconRect, VehicleTex.DraftVehicle, movementSlots.ToString());
      DrawIcon(ref iconRect, PawnIcon, nonMovementSlots.ToString());
    }
    return;

    static void DrawIcon(ref Rect rect, Texture2D icon, string tooltip)
    {
      GUI.DrawTexture(rect, icon);
      TooltipHandler.TipRegion(rect, tooltip);
      rect.y += SpecialPropIconSize + SpecialPropOffset;
    }
  }

  private static void DrawMoveSpeed(Rect rect, TransferableOneWay trad)
  {
    Widgets.DrawHighlightIfMouseover(rect);
    rect.SplitVertically(rect.width / 2, out Rect labelRect, out Rect valueRect);
    using TextBlock fontBlock = new(GameFont.Small);
    TooltipHandler.TipRegionByKey(rect, "VF_Caravan_MoveSpeed");

    float moveSpeed;
    if (trad.AnyThing is VehiclePawn vehicle)
    {
      moveSpeed = vehicle.statHandler.GetStatValue(VehicleStatDefOf.MoveSpeed) *
        vehicle.WorldSpeedMultiplier;
    }
    else
    {
      VehicleDef vehicleDef = trad.ThingDef as VehicleDef;
      Assert.IsNotNull(vehicleDef);
      moveSpeed = vehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed) *
        vehicleDef.properties.worldSpeedMultiplier;
    }
    float tilesPerDay = 0;
    if (moveSpeed > 0)
    {
      // Conversion for tiles per day
      float ticksPerTile = VehicleCaravanTicksPerMoveUtility.MoveSpeedToTileSpeed(moveSpeed);
      tilesPerDay = GenDate.TicksPerDay / ticksPerTile;
    }
    using (new TextBlock(TextAnchor.MiddleLeft))
    {
      Widgets.Label(labelRect, VehicleStatDefOf.MoveSpeed.LabelCap);
    }
    using (new TextBlock(TextAnchor.MiddleRight))
    {
      Widgets.Label(valueRect, $"{tilesPerDay:0.#} {"TilesPerDay".Translate()}");
    }
  }

  //private static void DrawMass(Rect rect, TransferableOneWay trad, float massCapacity)
  //{
  //  Widgets.DrawHighlightIfMouseover(rect);
  //  rect.SplitVertically(rect.width / 2, out Rect labelRect, out Rect valueRect);
  //  TooltipHandler.TipRegion(rect, "ItemWeightTip".Translate());
  //  using TextBlock fontBlock = new(GameFont.Small, TextAnchor.MiddleRight);

  //  using (new TextBlock(TextAnchor.MiddleLeft))
  //  {
  //    Widgets.Label(labelRect, VehicleStatDefOf.Mass.LabelCap);
  //  }
  //  float mass = trad.AnyThing is VehiclePawn vehicle ?
  //    vehicle.statHandler.GetStatValue(VehicleStatDefOf.Mass) :
  //    (trad.ThingDef as VehicleDef).GetStatValueAbstract(VehicleStatDefOf.Mass);

  //  using (new TextBlock(TextAnchor.MiddleRight,
  //    mass > massCapacity ? TransferableOneWayWidget.ItemMassColor : ColorLibrary.RedReadable))
  //  {
  //    Widgets.Label(valueRect, mass.ToStringMass());
  //  }
  //}

  private static void DrawCargoCapacity(Rect rect, TransferableOneWay trad)
  {
    Widgets.DrawHighlightIfMouseover(rect);
    rect.SplitVertically(rect.width / 2, out Rect labelRect, out Rect valueRect);
    using TextBlock fontBlock = new(GameFont.Small, TextAnchor.MiddleRight);
    TooltipHandler.TipRegion(rect, VehicleStatDefOf.CargoCapacity.description);

    using (new TextBlock(TextAnchor.MiddleLeft))
    {
      Widgets.Label(labelRect, VehicleStatDefOf.CargoCapacity.LabelCap);
    }
    float cargoCapacity = trad.AnyThing is VehiclePawn vehicle ?
      vehicle.statHandler.GetStatValue(VehicleStatDefOf.CargoCapacity) :
      (trad.ThingDef as VehicleDef).GetStatValueAbstract(VehicleStatDefOf.CargoCapacity);

    using (new TextBlock(TextAnchor.MiddleRight, cargoCapacity > 0 ? Color.green : Color.gray))
    {
      Widgets.Label(valueRect, cargoCapacity.ToStringMassOffset());
    }
  }

  private static void DrawMarketValue(Rect rect, TransferableOneWay trad)
  {
    Widgets.DrawHighlightIfMouseover(rect);
    rect.SplitVertically(rect.width / 2, out Rect labelRect, out Rect valueRect);
    using TextBlock fontBlock = new(GameFont.Small, TextAnchor.MiddleRight);
    TooltipHandler.TipRegion(rect, StatDefOf.MarketValue.description);

    using (new TextBlock(TextAnchor.MiddleLeft))
    {
      Widgets.Label(labelRect, StatDefOf.MarketValue.LabelCap);
    }
    using (new TextBlock(TextAnchor.MiddleRight))
    {
      Widgets.Label(valueRect, trad.AnyThing.MarketValue.ToStringMoney());
    }
  }

  private static void DoTransferableSorters(TransferableSorterDef sorterPrimary, TransferableSorterDef sorterSecondary,
    Action<TransferableSorterDef> primarySetter, Action<TransferableSorterDef> secondarySetter)
  {
    const float TopBarHeight = 27;
    const float LabelWidth = 60;
    const float ButtonWidth = 130;
    const float ButtonGap = 10;

    Widgets.BeginGroup(SortersRect);
    using TextBlock fontBlock = new(GameFont.Tiny);
    Rect labelRect = new(0f, 0f, LabelWidth, TopBarHeight);
    using (new TextBlock(TextAnchor.MiddleLeft))
    {
      Widgets.Label(labelRect, "SortBy".Translate());
    }
    Rect buttonRect = new(labelRect.xMax + ButtonGap, 0f, ButtonWidth, TopBarHeight);
    if (Widgets.ButtonText(buttonRect, sorterPrimary.LabelCap.Truncate(buttonRect.width - 2f)))
    {
      OpenSorterChangeFloatMenu(primarySetter);
    }
    buttonRect.x += buttonRect.width + ButtonGap;
    if (Widgets.ButtonText(buttonRect, sorterSecondary.LabelCap.Truncate(buttonRect.width - 2f)))
    {
      OpenSorterChangeFloatMenu(secondarySetter);
    }
    Widgets.EndGroup();
    return;

    static void OpenSorterChangeFloatMenu(Action<TransferableSorterDef> sorterSetter)
    {
      List<FloatMenuOption> list = [];
      foreach (TransferableSorterDef sorterDef in AllSorterDefs)
      {
        list.Add(new FloatMenuOption(sorterDef.LabelCap, () => sorterSetter(sorterDef)));
      }
      Find.WindowStack.Add(new FloatMenu(list));
    }
  }

  private class TransferableSorter(TransferableVehicleWidget widget, TransferableSorterDef sorterDef)
  {
    public TransferableSorterDef sorterDef = sorterDef;

    public void Sort(TransferableSorterDef def)
    {
      sorterDef = def;
      widget.CacheTransferables();
    }
  }

  private class Section : IDisposable
  {
    public string title;
    public List<TransferableOneWay> transferables;
    public List<VehiclePortrait> portraits;

    public List<TransferableOneWay> SortedTransferables => transferables;

    private void MarkAllDirty()
    {
      foreach (VehiclePortrait portrait in portraits)
      {
        portrait.MarkDirty();
      }
    }

    public void Sort(Comparison<TransferableOneWay> comparison)
    {
      SortedTransferables.Sort(comparison);
      MarkAllDirty();
    }

    public void Dispose()
    {
      foreach (VehiclePortrait portrait in portraits)
      {
        portrait.Dispose();
      }
      portraits.Clear();
    }
  }
}