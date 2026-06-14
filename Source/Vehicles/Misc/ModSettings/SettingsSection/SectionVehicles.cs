using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Rendering;
using Verse;
using Verse.Sound;

namespace Vehicles;

public class SectionVehicles : SettingsSection
{
  private const float SmallIconSize = 24;

  private static readonly FieldInfo EnabledField =
    AccessTools.Field(typeof(VehicleDef), nameof(VehicleDef.enabled));

  private static DesignationCategoryDef structureDesignationDef;

  public Dictionary<string, Dictionary<SaveableField, SavedField<object>>> fieldSettings = [];
  public Dictionary<string, Dictionary<SaveableField, object>> defaultValues = [];

  public Dictionary<string, Dictionary<string, float>> vehicleStats = [];

  // (defName, maskName)
  public Dictionary<string, PatternData> defaultGraphics = [];

  private Rot8 currentVehicleFacing;

  private float propertyHeight;

  private VehiclePortrait portrait;

  public override IEnumerable<FloatMenuOption> ResetOptions
  {
    get
    {
      if (VehicleMod.selectedDef != null)
      {
        yield return new FloatMenuOption(
          "VF_DevMode_ResetVehicle".Translate(VehicleMod.selectedDef.LabelCap), delegate
          {
            defaultGraphics.Remove(VehicleMod.selectedDef.defName);
            vehicleStats.Remove(VehicleMod.selectedDef.defName);
            SettingsCustomizableFields.PopulateSaveableFields(VehicleMod.selectedDef, true);
            VehicleMod.selectedDef.Portrait.MarkDirty();
            portrait.MarkDirty();
          });
      }
      yield return new FloatMenuOption("VF_DevMode_ResetAllVehicles".Translate(), ResetSettings);
      yield return new FloatMenuOption("VF_DevMode_ResetAll".Translate(),
        VehicleMod.ResetAllSettings);
    }
  }

  public override void OnOpen()
  {
    portrait = new VehiclePortrait();
    listingSplit = new Listing_Settings
    {
      maxOneColumn = true,
      shiftRectScrollbar = true
    };
  }

  public override void OnClose()
  {
    portrait.Dispose();
    portrait = null;
  }

  public override void Initialize()
  {
    fieldSettings ??= new Dictionary<string, Dictionary<SaveableField, SavedField<object>>>();
    vehicleStats ??= new Dictionary<string, Dictionary<string, float>>();
    defaultGraphics ??= new Dictionary<string, PatternData>();
  }

  public override void ResetSettings()
  {
    base.ResetSettings();
    VehicleMod.CachedFields.Clear();
    VehicleMod.PopulateCachedFields();
    fieldSettings.Clear();
    vehicleStats.Clear();
    defaultGraphics.Clear();
    portrait.MarkDirty();
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      vehicleDef.Portrait.MarkDirty();
    }
    if (VehicleMod.ModifiableSettings)
    {
      foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefsListForReading)
      {
        SettingsCustomizableFields.PopulateSaveableFields(def, true);
      }
    }
  }

  public override void PostDefDatabase()
  {
    foreach (PatternData patternData in defaultGraphics.Values)
    {
      patternData.ExposeDataPostDefDatabase();
    }
    structureDesignationDef = DefDatabase<DesignationCategoryDef>.GetNamed("Structure");
  }

  public override void ExposeData()
  {
    Scribe_NestedCollections.Look(ref fieldSettings, nameof(fieldSettings), LookMode.Value,
      LookMode.Deep, LookMode.Undefined);
    Scribe_NestedCollections.Look(ref vehicleStats, nameof(vehicleStats), LookMode.Value,
      LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref defaultGraphics, nameof(defaultGraphics), LookMode.Value,
      LookMode.Deep);
  }

  public override void OnGUI(Rect rect)
  {
    DrawVehicleOptions(rect);
    SectionDrawer.DrawVehicleList(rect,
      isValid => isValid ? string.Empty : "VF_SettingsDisabledTooltip".Translate().ToString(),
      vehicleDef => !VehicleMod.SettingsDisabledFor.Contains(vehicleDef.defName));
  }

  private void RecalculateHeight()
  {
    float propertySectionHeight = 5; // Buffer for bottom scrollable
    foreach (List<FieldInfo> fields in VehicleMod.VehicleCompFields.Values)
    {
      if (fields.NullOrEmpty() || fields.All(f =>
        f.TryGetAttribute(out PostToSettingsAttribute settings) &&
        settings.VehicleType != VehicleType.Universal &&
        settings.VehicleType != VehicleMod.selectedDef.type))
      {
        continue;
      }
      int rows = Mathf.CeilToInt((float)fields.Count / 3);
      propertySectionHeight += 50 + rows * 16; //72
    }
    propertyHeight = propertySectionHeight;
  }

  public override void VehicleSelected()
  {
    currentVehicleFacing = VehicleMod.selectedDef.drawProperties.displayRotation;
    RecalculateHeight();
    portrait.MarkDirty();
  }

  private void DrawVehicleWindow(Rect menuRect, Rect vehicleIconContainer, Rect vehicleDetailsContainer)
  {
    Rect iconRect = menuRect.ContractedBy(10);
    iconRect.width /= 5;
    iconRect.height = iconRect.width;
    iconRect.x += menuRect.width / 4;
    iconRect.y += 35;

    DoShowcaseButtons(iconRect);

    BlitRequest request = BlitRequest.For(VehicleMod.selectedDef) with
    {
      rot = currentVehicleFacing
    };
    portrait.Draw(iconRect, in request);

    Rect enableButtonRect = menuRect.ContractedBy(10);
    enableButtonRect.x += enableButtonRect.width / 4 + 5;
    EnableButton(enableButtonRect);

    Rect compVehicleRect = menuRect.ContractedBy(10);
    compVehicleRect.x += vehicleIconContainer.width * 2 - 10;
    compVehicleRect.y = iconRect.y;
    compVehicleRect.width -= vehicleIconContainer.width * 2;
    compVehicleRect.height = iconRect.height;

    listingSplit.Begin(compVehicleRect, 2);

    foreach (FieldInfo field in VehicleMod.vehicleDefFields)
    {
      if (field.TryGetAttribute(out PostToSettingsAttribute post))
      {
        post.DrawLister(listingSplit, VehicleMod.selectedDef, field);
      }
    }

    listingSplit.Shift();
    Rect buttonRect = listingSplit.GetSplitRect(24);
    if (Widgets.ButtonText(buttonRect, "VF_VehicleStats".Translate()))
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      Find.WindowStack.Add(new Dialog_StatSettings(VehicleMod.selectedDef));
    }

    listingSplit.End();

    float scrollableFieldY = menuRect.height * 0.4f;
    Rect scrollableFieldsRect = new(vehicleDetailsContainer.x + 1,
      menuRect.y + scrollableFieldY, vehicleDetailsContainer.width - 2,
      menuRect.height - scrollableFieldY - 10);

    Rect scrollableFieldsViewRect = new(scrollableFieldsRect.x, scrollableFieldsRect.y,
      scrollableFieldsRect.width - 20, propertyHeight);
    //UIElements.DrawLineVerticalGrey(iconRect.x + iconRect.width + 24, iconRect.y, VehicleMod.scrollableViewHeight - 10);
    UIElements.DrawLineHorizontalGrey(scrollableFieldsRect.x, scrollableFieldsRect.y - 1,
      scrollableFieldsRect.width);

    listingSplit.BeginScrollView(scrollableFieldsRect,
      ref SectionDrawer.saveableFieldsScrollPosition, ref scrollableFieldsViewRect, 3);

    foreach ((Type type, List<FieldInfo> fields) in VehicleMod.VehicleCompFields)
    {
      if (fields.NullOrEmpty() || fields.All(f =>
        f.TryGetAttribute(out PostToSettingsAttribute settings)
        && settings.VehicleType != VehicleType.Universal &&
        settings.VehicleType != VehicleMod.selectedDef.type))
      {
        continue;
      }
      string header = string.Empty;
      if (type.TryGetAttribute(out HeaderTitleAttribute title))
      {
        header = title.Translate ? title.Label.Translate().ToString() : title.Label;
      }
      listingSplit.Header(header, ListingExtension.BannerColor, fontSize: GameFont.Small,
        anchor: TextAnchor.MiddleCenter, rowGap: 24);
      foreach (FieldInfo field in fields)
      {
        if (field.TryGetAttribute(out PostToSettingsAttribute post))
        {
          post.DrawLister(listingSplit, VehicleMod.selectedDef, field);
        }
      }
    }
    listingSplit.EndScrollView(ref scrollableFieldsViewRect);
    
  }

  private void DrawVehicleOptions(Rect menuRect)
  {
    Rect vehicleIconContainer = menuRect.ContractedBy(10);
    vehicleIconContainer.width /= 4;
    vehicleIconContainer.height = vehicleIconContainer.width;
    vehicleIconContainer.x += vehicleIconContainer.width;

    Rect vehicleDetailsContainer = menuRect.ContractedBy(10);
    vehicleDetailsContainer.x += vehicleIconContainer.width - 1;
    vehicleDetailsContainer.width -= vehicleIconContainer.width;

    Widgets.DrawBoxSolid(vehicleDetailsContainer, Color.grey);
    Rect vehicleDetailsRect = vehicleDetailsContainer.ContractedBy(1);
    Widgets.DrawBoxSolid(vehicleDetailsRect, ListingExtension.MenuSectionBGFillColor);

    UIElements.Header(
      (vehicleDetailsContainer with { height = Text.LineHeightOf(GameFont.Medium) })
     .ContractedBy(1),
      $"{VehicleMod.selectedDef?.LabelCap ?? string.Empty}", ListingExtension.BannerColor,
      fontSize: GameFont.Medium, anchor: TextAnchor.MiddleCenter);

    if (VehicleMod.selectedDef != null)
    {
      try
      {
        DrawVehicleWindow(menuRect, vehicleIconContainer, vehicleDetailsContainer);
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Exception thrown while trying to select {VehicleMod.selectedDef.defName}. Disabling vehicle to preserve mod settings.\nException={ex}");
        VehicleMod.SettingsDisabledFor.Add(VehicleMod.selectedDef.defName);
        VehicleMod.selectedDef = null;
        VehicleMod.selectedPatterns.Clear();
        VehicleMod.selectedDefUpgradeComp = null;
        VehicleMod.selectedNode = null;
      }
    }
  }

  private void DoShowcaseButtons(Rect iconRect)
  {
    Rect showcaseIconRect = new(iconRect.x + iconRect.width, iconRect.y, SmallIconSize,
      SmallIconSize);

    if (VehicleMod.selectedDef.graphicData.drawRotated &&
      VehicleMod.selectedDef.graphicData.Graphic is Graphic_Vehicle graphicVehicle)
    {
      if (VehicleShowcaseButton(showcaseIconRect, VehicleTex.Rotate))
      {
        const int RotationCount = 4;

        List<Rot8> validRotations = graphicVehicle.RotationsRenderableByUI.ToList();
        for (int i = 0; i < RotationCount; i++)
        {
          currentVehicleFacing = currentVehicleFacing.Rotated(RotationDirection.Clockwise, diagonals: false);
          if (validRotations.Contains(currentVehicleFacing))
          {
            portrait.MarkDirty();
            break;
          }
        }
      }
      showcaseIconRect.y += SmallIconSize;
    }

    if (VehicleMod.selectedPatterns.Count > 1 && VehicleMod.settings.main.useCustomShaders &&
      VehicleMod.selectedDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
    {
      if (VehicleShowcaseButton(showcaseIconRect, VehicleTex.Recolor,
        "VF_RecolorDefaultMaskTooltip"))
      {
        Dialog_VehiclePainter.OpenColorPicker(VehicleMod.selectedDef, SetDefaultColor);
      }
      showcaseIconRect.y += SmallIconSize;
    }
  }

  private void SetDefaultColor(Color colorOne, Color colorTwo, Color colorThree, PatternDef pattern,
    Vector2 displacement, float tiles)
  {
    defaultGraphics[VehicleMod.selectedDef.defName] = new PatternData(colorOne, colorTwo,
      colorThree, pattern, displacement, tiles);
    portrait.MarkDirty();
    VehicleMod.selectedDef.Portrait.MarkDirty();
  }

  private static bool VehicleShowcaseButton(Rect rect, Texture2D icon, string tooltipKey = null)
  {
    Widgets.DrawHighlightIfMouseover(rect);
    Widgets.DrawTextureFitted(rect, icon, 1);
    if (!tooltipKey.NullOrEmpty())
    {
      TooltipHandler.TipRegionByKey(rect, tooltipKey);
    }
    if (Widgets.ButtonInvisible(rect))
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
      return true;
    }
    return false;
  }

  private void EnableButton(Rect rect)
  {
    if (VehicleMod.selectedDef is null)
    {
      Log.Error("SelectedDef is null while trying to create Enable button for VehicleDef.");
      return;
    }
    using TextBlock textBlock = new(GameFont.Medium);

    SaveableField saveableField = new(VehicleMod.selectedDef, EnabledField);
    VehicleEnabled.For enabledFor = VehicleEnabled.For.Everyone;
    if (fieldSettings[VehicleMod.selectedDef.defName]
     .TryGetValue(saveableField, out SavedField<object> modifiedValue))
    {
      enabledFor = (VehicleEnabled.For)modifiedValue.EndValue;
    }
    (string text, Color color) = VehicleEnabled.GetStatus(enabledFor);
    Vector2 size = Text.CalcSize(text);
    Rect enabledButtonRect = new(rect.x, rect.y, size.x, size.y);
    TooltipHandler.TipRegion(enabledButtonRect, "VF_EnableButtonTooltip".Translate());

    Color highlightedColor = new(color.r + 0.25f, color.g + 0.25f, color.b + 0.25f);
    if (UIElements.ClickableLabel(enabledButtonRect, text, highlightedColor, color,
      GameFont.Medium, TextAnchor.MiddleLeft,
      new Color(color.r - 0.15f, color.g - 0.15f, color.b - 0.15f)))
    {
      enabledFor = enabledFor.Next();
      fieldSettings[VehicleMod.selectedDef.defName][saveableField] =
        new SavedField<object>(enabledFor);
      if (enabledFor == (VehicleEnabled.For)EnabledField.GetValue(VehicleMod.selectedDef))
      {
        fieldSettings[VehicleMod.selectedDef.defName].Remove(saveableField);
      }
      bool allowed = enabledFor is VehicleEnabled.For.Player or VehicleEnabled.For.Everyone;
      if (Current.ProgramState == ProgramState.Playing)
      {
        Current.Game.Rules.SetAllowBuilding(VehicleMod.selectedDef.buildDef, allowed);
      }
      GizmoHelper.DesignatorsChanged(VehicleMod.selectedDef.designationCategory ??
        structureDesignationDef);
    }
  }
}