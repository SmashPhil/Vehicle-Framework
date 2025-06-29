using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;
using TransferableOneWay = RimWorld.TransferableOneWay;

namespace Vehicles.World;

[StaticConstructorOnStartup]
public sealed class TransferableVehicleWidget
{
  private const int ColumnCount = 4;
  private const float CardHeight = 300;
  private const float LabelHeight = 30;
  private const float CardIconSize = 150;
  private const float CardSpacing = 5;
  private const float CardContentPadding = 5;

  private const float FirstTransferableY = 6f;
  private const float ExtraSpaceAfterSectionTitle = 5f;

  public const float TopAreaWidth = 515f;

  public const float CountColumnWidth = 75f;
  public const float AdjustColumnWidth = 240f;
  public const float MassColumnWidth = 100f;

  // HostilityResponseModeUtility::FleeIcon
  private static readonly Texture2D pawnIcon =
    ContentFinder<Texture2D>.Get("UI/Icons/HostilityResponse/Flee");

  private static readonly Color cardColor = new(1f, 1f, 1f, 0.04f);

  private static readonly StringBuilder stringBuilder = new();

  private readonly Section vehicleSection;
  private readonly List<TransferableOneWay> pawns;
  private readonly PlanetTile tile;
  private bool transferablesCached;

  private readonly TransferableSorter sortByCategory;
  private readonly TransferableSorter sortByMarketValue;

  private readonly HashSet<VehicleDef> impassableOnTile = [];

  private Vector2 scrollPosition;

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

  public TransferableVehicleWidget(string title, List<TransferableOneWay> vehicles,
    List<TransferableOneWay> pawns,
    PlanetTile tile = default)
  {
    vehicleSection = new Section
    {
      title = title,
      transferables = vehicles,
    };
    this.pawns = pawns;
    this.tile = tile;

    sortByCategory = new TransferableSorter(this, TransferableSorterDefOf.Category);
    sortByMarketValue = new TransferableSorter(this, TransferableSorterDefOf.MarketValue);

    Init();
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
          impassableOnTile.Add(vehicleDef);
      }
    }
  }

  private void CacheTransferables()
  {
    transferablesCached = true;
    vehicleSection.SortedTransferables.Clear();
    vehicleSection.SortedTransferables.AddRange(vehicleSection.transferables
     .OrderBy(transferableOneWay => CanCaravan(transferableOneWay, out _))
     .ThenBy(transferOneWay => transferOneWay, sortByCategory.sorterDef.Comparer)
     .ThenBy(transferOneWay => transferOneWay, sortByMarketValue.sorterDef.Comparer)
     .ThenBy(TransferableUIUtility.DefaultListOrderPriority));
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
    TransferableUIUtility.DoTransferableSorters(sortByCategory.sorterDef,
      sortByMarketValue.sorterDef, sortByCategory.Sort, sortByMarketValue.Sort);

    Rect mainRect = new(inRect.x, inRect.y + 37f, inRect.width,
      inRect.height - 37f);
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

    Rect viewRect = new(0f, 0f, mainRect.width - 16f, Height);
    Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);
    float cardWidth = viewRect.width / ColumnCount;

    if (vehicleSection.SortedTransferables.NullOrEmpty())
      return;

    if (vehicleSection.title != null)
    {
      Widgets.ListSeparator(ref curY, viewRect.width, vehicleSection.title);
      curY += ExtraSpaceAfterSectionTitle;
    }
    for (int i = 0; i < vehicleSection.transferables.Count; i++)
    {
      TransferableOneWay transferable = vehicleSection.transferables[i];
      if (curY > bottomLimit && curY < topLimit)
      {
        int column = i % ColumnCount;
        Rect rect = new(column * cardWidth, curY, cardWidth, CardHeight);

        Widgets.BeginGroup(rect);
        rect = rect.AtZero().ContractedBy(CardSpacing / 2f);
        Widgets.DrawBoxSolidWithOutline(rect, cardColor, Widgets.SeparatorLineColor);
        DrawCard(rect.ContractedBy(CardContentPadding), vehicleSection, transferable);
        Widgets.EndGroup();
      }
      if ((i + 1) % ColumnCount == 0)
        curY += CardHeight;
    }
    vehicleSection.texturesDirty = false;
    Widgets.EndScrollView();
  }

  private void DrawCard(Rect rect, Section section, TransferableOneWay transferable)
  {
    const float Margin = 15;
    const float CheckboxSize = 24;

    VehiclePawn vehicle = transferable.AnyThing as VehiclePawn;
    VehicleDef vehicleDef = transferable.ThingDef as VehicleDef;
    Assert.IsNotNull(vehicleDef);
    bool canCaravan = CanCaravan(transferable, out string disableReason);

    Rect iconBar = rect with { height = CardIconSize };
    Rect iconRect = iconBar.ToSquare();

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
          Find.WindowStack.Add(new Dialog_AssignSeats(pawns, transferable));
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
          CaravanFormation.formation.NotifyTransferablesChanged();
        }
      }
    }

    //Rect specialPropsRect = iconBar with
    //{
    //  y = iconBar.yMax - SpecialPropIconSize, height = SpecialPropIconSize
    //};
    //DrawSpecialProperties(specialPropsRect, vehicleDef, vehicle);

    RenderTextureBuffer buffer = section.buffers.TryGetValue(transferable);

    Widgets.BeginGroup(iconRect);
    Rect textureRect = iconRect.AtZero();
    if (section.texturesDirty)
    {
      BlitRequest blitRequest = vehicle != null ?
        BlitRequest.For(vehicle) :
        BlitRequest.For(vehicleDef);
      buffer ??= section.buffers[transferable] =
        VehicleGui.CreateRenderTextureBuffer(textureRect, in blitRequest);
      VehicleGui.Blit(buffer.GetWrite(), textureRect, in blitRequest);
    }
    GUI.DrawTexture(textureRect, buffer.Read);
    Widgets.EndGroup();

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
    if (vehicle != null)
    {
      DrawIcon(ref rect, VehicleTex.DraftVehicle, vehicle.PawnCountToOperate.ToString());
      DrawIcon(ref rect, pawnIcon, vehicle.PawnsByHandlingType[HandlingType.None].Count.ToString());
    }
    else
    {
      int movementSlots = vehicleDef.properties.RoleSeats(HandlingType.Movement);
      int nonMovementSlots = vehicleDef.properties.TotalSeats - movementSlots;
      DrawIcon(ref rect, VehicleTex.DraftVehicle, movementSlots.ToString());
      DrawIcon(ref rect, pawnIcon, nonMovementSlots.ToString());
    }
    return;

    static void DrawIcon(ref Rect rect, Texture2D icon, string label)
    {
      Rect iconRect = new(rect.x, rect.y, rect.height, rect.height);
      rect.xMin += iconRect.width;
      GUI.DrawTexture(iconRect, icon);
      Widgets.Label(rect, label);
      rect.xMin += Text.CalcSize(label).x;
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
      moveSpeed /= 60; // normalize
      int ticksPerTile = VehicleCaravanTicksPerMoveUtility.TicksFromMoveSpeed(moveSpeed);
      tilesPerDay = GenDate.TicksPerDay / (float)ticksPerTile;
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

  private class TransferableSorter(
    TransferableVehicleWidget widget,
    TransferableSorterDef sorterDef)
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
    public readonly Dictionary<TransferableOneWay, RenderTextureBuffer> buffers = [];
    public bool texturesDirty = true;

    public List<TransferableOneWay> SortedTransferables { get; } = [];

    void IDisposable.Dispose()
    {
      if (!buffers.NullOrEmpty())
      {
        foreach (RenderTextureBuffer buffer in buffers.Values)
        {
          buffer.Dispose();
        }
        buffers.Clear();
      }
      GC.SuppressFinalize(this);
    }
  }
}