using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles.Rendering;

public static class UIHelper
{
  /// <summary>
  /// Create new Widget for VehicleCaravan with <paramref name="transferables"/>
  /// </summary>
  public static void CreateVehicleCaravanTransferableWidgets(List<TransferableOneWay> transferables,
    out TransferableOneWayWidget pawnsTransfer, out TransferableVehicleWidget vehiclesTransfer,
    out TransferableOneWayWidget itemsTransfer, string thingCountTip,
    IgnorePawnsInventoryMode ignorePawnInventoryMass, Func<float> availableMassGetter,
    bool ignoreSpawnedCorpseGearAndInventoryMass, PlanetTile tile,
    bool playerPawnsReadOnly = false)
  {
    pawnsTransfer = new TransferableOneWayWidget(null, null, null, thingCountTip, drawMass: true,
      ignorePawnInventoryMass: ignorePawnInventoryMass, false,
      availableMassGetter: availableMassGetter, 0f,
      ignoreSpawnedCorpseGearAndInventoryMass: ignoreSpawnedCorpseGearAndInventoryMass, tile: tile,
      drawMarketValue: true,
      drawEquippedWeapon: true, drawNutritionEatenPerDay: true, false, drawItemNutrition: true,
      false,
      playerPawnsReadOnly);

    AddVehicleAndPawnSections(pawnsTransfer, out vehiclesTransfer, transferables, tile);
    itemsTransfer = new TransferableOneWayWidget(
      transferables.Where(t => t.ThingDef.category != ThingCategory.Pawn), null, null,
      thingCountTip, true, ignorePawnInventoryMass, false, availableMassGetter, 0f,
      ignoreSpawnedCorpseGearAndInventoryMass, tile, true, false, false, true, false, true, false);
  }

  /// <summary>
  /// Create sections in VehicleCaravan dialog for proper listing
  /// </summary>
  private static void AddVehicleAndPawnSections(TransferableOneWayWidget pawnWidget,
    out TransferableVehicleWidget vehicleWidget, List<TransferableOneWay> transferables,
    PlanetTile tile)
  {
    IEnumerable<TransferableOneWay> source =
      transferables.Where(t => t.ThingDef.category == ThingCategory.Pawn);

    List<TransferableOneWay> vehicles = [];
    List<TransferableOneWay> pawns = [];
    foreach (TransferableOneWay transferable in transferables)
    {
      if (transferable.ThingDef.category != ThingCategory.Pawn)
        continue;

      switch (transferable.AnyThing)
      {
        case VehiclePawn:
          vehicles.Add(transferable);
          break;
        case Pawn pawn and not VehiclePawn:
          if (pawn.IsFreeColonist)
            pawns.Add(transferable);
          break;
      }
    }
    vehicleWidget = Find.CurrentMap != null ?
        new TransferableVehicleWidget(vehicles, pawns, Find.CurrentMap) :
        new TransferableVehicleWidget(vehicles, pawns, tile);
    pawnWidget.AddSection("ColonistsSection".Translate(),
      source.Where(t => t.AnyThing is Pawn { IsFreeColonist: true }));
    pawnWidget.AddSection("PrisonersSection".Translate(),
      source.Where(t => t.AnyThing is Pawn { IsPrisoner: true }));
    pawnWidget.AddSection("CaptureSection".Translate(),
      source.Where(t =>
        t.AnyThing is Pawn { Downed: true } pawn &&
        CaravanUtility.ShouldAutoCapture(pawn, Faction.OfPlayer)));
    pawnWidget.AddSection("AnimalsSection".Translate(),
      source.Where(t => t.AnyThing is Pawn pawn && pawn.RaceProps.Animal));
  }

  public static bool DrawPagination(Rect rect, ref int pageNumber, int pageCount)
  {
    bool pageChanged = false;
    Rect leftButtonRect = new(rect.x, rect.y, rect.height, rect.height);
    Rect rightButtonRect =
      new(rect.x + rect.width - rect.height, rect.y, rect.height, rect.height);
    rect.xMin += leftButtonRect.width;
    rect.xMax -= rightButtonRect.width;
    if (Widgets.ButtonImage(leftButtonRect, VehicleTex.LeftArrow))
    {
      pageChanged = true;
      pageNumber = (--pageNumber).Clamp(1, pageCount);
      SoundDefOf.PageChange.PlayOneShotOnCamera();
    }
    if (Widgets.ButtonImage(rightButtonRect, VehicleTex.RightArrow))
    {
      pageChanged = true;
      pageNumber = (++pageNumber).Clamp(1, pageCount);
      SoundDefOf.PageChange.PlayOneShotOnCamera();
    }
    float numbersLength = rect.width - rect.height * 2f;
    int pageNumbersDisplayedTotal = Mathf.CeilToInt(numbersLength / 1.5f / rect.height);
    int pageNumbersDisplayedHalf = Mathf.FloorToInt(pageNumbersDisplayedTotal / 2f);

    using TextBlock tb = new(GameFont.Small, TextAnchor.MiddleCenter);
    float pageNumberingOrigin = rect.x + rect.height + numbersLength / 2;
    Rect pageRect = new(pageNumberingOrigin, rect.y, rect.height, rect.height);
    Widgets.ButtonText(pageRect, pageNumber.ToString(), drawBackground: false,
      doMouseoverSound: false);

    Text.Font = GameFont.Tiny;
    int offsetRight = 1;
    for (int pageLeftDisplayNum = pageNumber + 1;
      pageLeftDisplayNum <= (pageNumber + pageNumbersDisplayedHalf) &&
      pageLeftDisplayNum <= pageCount;
      pageLeftDisplayNum++, offsetRight++)
    {
      pageRect.x = pageNumberingOrigin + (numbersLength / pageNumbersDisplayedTotal * offsetRight);
      if (Widgets.ButtonText(pageRect, pageLeftDisplayNum.ToString(), drawBackground: false))
      {
        pageChanged = true;
        pageNumber = pageLeftDisplayNum;
        SoundDefOf.PageChange.PlayOneShotOnCamera();
      }
    }
    int offsetLeft = 1;
    for (int pageRightDisplayNum = pageNumber - 1;
      pageRightDisplayNum >= (pageNumber - pageNumbersDisplayedHalf) && pageRightDisplayNum >= 1;
      pageRightDisplayNum--, offsetLeft++)
    {
      pageRect.x = pageNumberingOrigin - (numbersLength / pageNumbersDisplayedTotal * offsetLeft);
      if (Widgets.ButtonText(pageRect, pageRightDisplayNum.ToString(), drawBackground: false))
      {
        pageChanged = true;
        pageNumber = pageRightDisplayNum;
        SoundDefOf.PageChange.PlayOneShotOnCamera();
      }
    }
    return pageChanged;
  }
}