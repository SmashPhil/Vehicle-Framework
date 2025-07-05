using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

internal static class SectionDrawer
{
  private const GameFont ListHeaderFont = GameFont.Small;
  private const GameFont ListItemFont = GameFont.Tiny;

  private static List<VehicleDef> vehicleDefs;
  private static readonly List<VehicleDef> filteredVehicleDefs = [];
  private static readonly HashSet<string> headers = [];

  private static readonly QuickSearchFilter vehicleFilter = new();

  internal static Vector2 saveableFieldsScrollPosition;
  private static Vector2 vehicleDefsScrollPosition;

  private static float VehicleListHeight { get; set; }

  internal static List<VehicleDef> VehicleDefs
  {
    get
    {
      if (vehicleDefs.NullOrEmpty())
      {
        List<VehicleDef> allDefs = DefDatabase<VehicleDef>.AllDefsListForReading;
        if (!allDefs.NullOrEmpty())
        {
          vehicleDefs = allDefs
           .OrderBy(d => d.modContentPack.PackageId.Contains(VehicleHarmony.VehiclesUniqueId))
           .ThenBy(d2 => d2.modContentPack.PackageId).ToList();
          RecacheVehicleFilter();
        }
      }
      return vehicleDefs;
    }
  }

  private static void RecacheVehicleFilter()
  {
    filteredVehicleDefs.Clear();
    headers.Clear();
    if (!VehicleDefs.NullOrEmpty())
    {
      foreach (VehicleDef vehicleDef in VehicleDefs)
      {
        if (vehicleFilter.Text.NullOrEmpty() || vehicleFilter.Matches(vehicleDef.defName) ||
          vehicleFilter.Matches(vehicleDef.label) ||
          vehicleFilter.Matches(vehicleDef.modContentPack.Name))
        {
          headers.Add(vehicleDef.modContentPack.Name);
          filteredVehicleDefs.Add(vehicleDef);
        }
      }
      VehicleListHeight = -1;
    }
  }

  private static void RecacheVehicleListHeight(float width)
  {
    float height = 0;
    using (new TextBlock(ListHeaderFont, TextAnchor.MiddleCenter))
    {
      foreach (string header in headers)
        height += Text.CalcHeight(header, width);
    }
    using (new TextBlock(ListItemFont))
    {
      foreach (VehicleDef vehicleDef in filteredVehicleDefs)
        height += Text.CalcHeight(vehicleDef.LabelCap, width);
    }
    VehicleListHeight = height;
  }

  public static void DrawVehicleList(Rect rect, Func<bool, string> tooltipGetter = null,
    Predicate<VehicleDef> validator = null)
  {
    Rect scrollContainer = rect.ContractedBy(10);
    scrollContainer.width /= 4;

    Widgets.DrawBoxSolid(scrollContainer, Color.grey);
    Rect innerContainer = scrollContainer.ContractedBy(1);
    Widgets.DrawBoxSolid(innerContainer, ListingExtension.MenuSectionBGFillColor);

    Rect searchBoxRect = innerContainer with { height = Text.LineHeight };
    using TextBlock textFont = new(GameFont.Small);
    Widgets.Label(searchBoxRect, "VF_ListSearchText".Translate());
    searchBoxRect.y += searchBoxRect.height;
    string searchText = Widgets.TextField(searchBoxRect, vehicleFilter.Text);
    if (searchText != vehicleFilter.Text)
    {
      vehicleFilter.Text = searchText;
      RecacheVehicleFilter();
    }

    // No need to render list if no vehicle mods active, and also calling get_VehicleDefs
    // will trigger recache if mod settings page is opened directly to Vehicles tab
    if (filteredVehicleDefs.NullOrEmpty() && VehicleDefs.NullOrEmpty())
      return;

    if (VehicleMod.selectedDef != null)
    {
      if (KeyBindingDefOf.MapDolly_Up.KeyDownEvent)
      {
        int index = filteredVehicleDefs.IndexOf(VehicleMod.selectedDef) - 1;
        if (index < 0)
        {
          index = filteredVehicleDefs.Count - 1;
        }
        VehicleMod.SelectVehicle(filteredVehicleDefs[index]);
      }
      if (KeyBindingDefOf.MapDolly_Down.KeyDownEvent)
      {
        int index = filteredVehicleDefs.IndexOf(VehicleMod.selectedDef) + 1;
        if (index >= filteredVehicleDefs.Count)
        {
          index = 0;
        }
        VehicleMod.SelectVehicle(filteredVehicleDefs[index]);
      }
    }

    Rect scrollList = (innerContainer with { yMin = searchBoxRect.yMax }).ContractedBy(1);
    float viewWidth = scrollList.width - 16; // - scrollbar width
    if (VehicleListHeight < 0)
      RecacheVehicleListHeight(viewWidth);
    Rect scrollView = new(0, 0, viewWidth, VehicleListHeight);

    // Begin ScrollView
    Widgets.BeginScrollView(scrollList, ref vehicleDefsScrollPosition, scrollView);
    string currentModTitle = string.Empty;
    float curY = 0;
    foreach (VehicleDef vehicleDef in filteredVehicleDefs)
    {
      try
      {
        if (currentModTitle != vehicleDef.modContentPack.Name)
        {
          currentModTitle = vehicleDef.modContentPack.Name;
          float headerHeight = Text.CalcHeight(currentModTitle, scrollView.width);
          Rect headerTitle = new(0, curY, scrollView.width, headerHeight);
          UIElements.Header(headerTitle, currentModTitle, ListingExtension.BannerColor,
            ListHeaderFont,
            TextAnchor.MiddleCenter);
          curY += headerTitle.height;
        }
        bool validated = validator is null || validator(vehicleDef);
        string tooltip = tooltipGetter != null ? tooltipGetter(validated) : string.Empty;
        using TextBlock fontBlock = new(ListItemFont);
        float labelHeight = Text.CalcHeight(vehicleDef.LabelCap, scrollView.width);
        Rect labelRect = new(0, curY, scrollView.width, labelHeight);
        if (ListItemSelectable(labelRect, vehicleDef.LabelCap, Color.yellow,
          VehicleMod.selectedDef == vehicleDef, validated, tooltip))
        {
          if (VehicleMod.selectedDef == vehicleDef)
            VehicleMod.DeselectVehicle();
          else
            VehicleMod.SelectVehicle(vehicleDef);
        }
        curY += labelHeight;
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Exception thrown while trying to select {vehicleDef}. Disabling vehicle to preserve mod settings.\nException={ex}");
        VehicleMod.selectedDef = null;
        VehicleMod.selectedPatterns.Clear();
        VehicleMod.selectedDefUpgradeComp = null;
        VehicleMod.selectedNode = null;
        VehicleMod.SettingsDisabledFor.Add(vehicleDef.defName);
      }
    }
    Widgets.EndScrollView();
    // End ScrollView
  }

  private static bool ListItemSelectable(Rect rect, string label, Color hoverColor,
    bool selected = false,
    bool active = true, string disabledTooltip = null)
  {
    using TextBlock textBlock = new(Color.white);

    if (selected)
      Widgets.DrawBoxSolid(rect, ListingExtension.HighlightColor);

    try
    {
      if (!active)
        GUIState.Disable();
      else if (Mouse.IsOver(rect))
        GUI.color = hoverColor;

      if (!disabledTooltip.NullOrEmpty())
        TooltipHandler.TipRegion(rect, disabledTooltip);

      Widgets.Label(rect, label);

      if (Widgets.ButtonInvisible(rect))
      {
        SoundDefOf.Click.PlayOneShotOnCamera();
        return true;
      }
      return false;
    }
    finally
    {
      GUIState.Enable();
    }
  }
}