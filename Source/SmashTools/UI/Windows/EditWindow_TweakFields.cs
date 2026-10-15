using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

public class EditWindow_TweakFields : EditWindow
{
  private const float VectorLabelProportion = 0.5f;
  private const float VectorSubLabelProportion = 0.15f;
  private const float FloatRangeLabelProportion = 0.5f;
  private const float FloatRangeSubLabelProportion = 0.25f;

  private const float RowHeight = 24;
  private const int ColumnCount = 2;

  private readonly Thing thing;
  private static List<TweakInfo> instanceTweaks;

  private Vector2 scrollPosition;
  private readonly Listing_SplitColumns listing = new();

  private static readonly Dictionary<FieldInfo, UiSettings> registeredFields = [];

  public override bool IsDebug => true;

  public EditWindow_TweakFields(Thing thing)
  {
    this.thing = thing;
    optionalTitle = "TweakValues";
    instanceTweaks = FindAllTweakablesRecursive(thing).OrderBy(info => info.ui.category)
     .ThenBy(info => info.ui.subCategory).ThenBy(info => info.fieldInfo.DeclaringType?.Name)
     .ThenBy(info => info.Name).ToList();
  }

  public override Vector2 InitialSize => new(UI.screenWidth / 2f, UI.screenHeight * 0.9f);

  private float CachedHeight { get; set; } = -1;

  public static void RegisterField(FieldInfo fieldInfo, string category, string subCategory,
    UISettingsType settingsType)
  {
    if (registeredFields.ContainsKey(fieldInfo))
      return;

    registeredFields.Add(fieldInfo, new UiSettings(category, subCategory, settingsType));
  }

  private static IEnumerable<TweakInfo> FindAllTweakablesRecursive(Thing thing)
  {
    foreach (TweakInfo info in FindAllTweakablesRecursive(thing.GetType(), thing, thing.Label,
      string.Empty))
    {
      yield return info;
    }
    foreach (TweakInfo info in FindAllTweakablesRecursive(thing.def.GetType(), thing.def,
      thing.def.defName, string.Empty))
    {
      yield return info;
    }
    if (!thing.def.comps.NullOrEmpty())
    {
      foreach (CompProperties compProperties in thing.def.comps)
      {
        foreach (TweakInfo info in FindAllTweakablesRecursive(compProperties.GetType(),
          compProperties,
          compProperties.GetType().Name, string.Empty))
        {
          yield return info;
        }
      }
    }
    if (thing is ThingWithComps thingWithComps)
    {
      foreach (ThingComp thingComp in thingWithComps.AllComps)
      {
        foreach (TweakInfo info in FindAllTweakablesRecursive(thingComp.GetType(), thingComp,
          thingComp.GetType().Name, string.Empty))
        {
          yield return info;
        }
      }
    }
  }

  private static IEnumerable<TweakInfo> FindAllTweakablesRecursive(Type type, object parent,
    string category, string subCategory)
  {
    foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
      BindingFlags.Instance | BindingFlags.Static))
    {
      if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is not null ||
        registeredFields.ContainsKey(fieldInfo))
      {
        Assert.IsNotNull(fieldInfo.DeclaringType);
        if (fieldInfo.IsStatic)
        {
          Log.Error(
            $"Cannot use TweakFieldAttribute on static fields. Use vanilla's TweakValues for static fields instead. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
          continue;
        }
        if (fieldInfo.IsLiteral)
        {
          Log.Error(
            $"Cannot use TweakFieldAttribute on constants. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
          continue;
        }
        if (fieldInfo.FieldType.IsClass)
        {
          if (fieldInfo.FieldType.IsIList())
          {
            if (!fieldInfo.FieldType.GetGenericArguments()[0].IsClass)
            {
              Log.Error(
                $"Cannot use TweakFieldAttribute on list of non-reference types. Field={fieldInfo.DeclaringType.Name}.{fieldInfo.Name}");
              continue;
            }
            IList list = (IList)fieldInfo.GetValue(parent);
            if (list != null)
            {
              int index = 0;
              foreach (object item in list)
              {
                (string newCategory, string newSubCategory) =
                  GetCategory(fieldInfo, item, category, subCategory);
                List<TweakInfo> infos =
                  FindAllTweakablesRecursive(item.GetType(), item, newCategory, newSubCategory)
                   .ToList();
                foreach (TweakInfo info in infos)
                {
                  if (list.Count > 1)
                  {
                    info.IndexInList = index;
                  }
                  yield return info;
                }
                index++;
              }
            }
          }
          else
          {
            object instance = fieldInfo.GetValue(parent);
            if (instance != null)
            {
              (string newCategory, string newSubCategory) =
                GetCategory(fieldInfo, instance, category, subCategory);
              foreach (TweakInfo info in FindAllTweakablesRecursive(fieldInfo.FieldType, instance,
                newCategory, newSubCategory))
              {
                yield return info;
              }
            }
          }
        }
        else
        {
          UISettingsType settingsType = UISettingsType.None;
          if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is { } tweakFieldAttribute)
          {
            settingsType = tweakFieldAttribute.SettingsType;
          }
          else if (registeredFields.TryGetValue(fieldInfo, out UiSettings info))
          {
            settingsType = info.settingsType;
          }
          yield return CreateInfo(fieldInfo, parent, category, subCategory, settingsType);
        }
      }
    }
  }

  private static (string category, string subCategory) GetCategory(FieldInfo fieldInfo,
    object instance,
    string category, string subCategory)
  {
    if (instance is ITweakFields tweakFields)
    {
      if (!tweakFields.Category.NullOrEmpty())
      {
        category = tweakFields.Category;
      }
      if (!tweakFields.Label.NullOrEmpty())
      {
        subCategory = tweakFields.Label;
      }
    }
    else if (fieldInfo.TryGetAttribute<TweakFieldAttribute>() is { } tweakFieldAttribute)
    {
      if (!tweakFieldAttribute.Category.NullOrEmpty())
      {
        category = tweakFieldAttribute.Category;
      }
      if (!tweakFieldAttribute.SubCategory.NullOrEmpty())
      {
        subCategory = tweakFieldAttribute.SubCategory;
      }
    }
    else if (registeredFields.TryGetValue(fieldInfo,
      out UiSettings ui))
    {
      if (!ui.category.NullOrEmpty())
      {
        category = ui.category;
      }
      if (!ui.subCategory.NullOrEmpty())
      {
        subCategory = ui.subCategory;
      }
    }
    return (category, subCategory);
  }

  private static TweakInfo CreateInfo(FieldInfo fieldInfo, object instance, string category,
    string subCategory, UISettingsType settingsType)
  {
    TweakInfo info = new()
    {
      fieldInfo = fieldInfo,
      instance = instance,
      ui = new UiSettings(category, subCategory, settingsType),
      IndexInList = -1 //Default to -1, 0 and above indicate it's in a list
    };
    return info;
  }

  private void RecacheHeight(float viewWidth)
  {
    CachedHeight = 0;

    string currentCategory = string.Empty;
    string currentSubCategory = string.Empty;

    int rowCount = 0;
    foreach (TweakInfo info in instanceTweaks)
    {
      if (info.ui.category != currentCategory)
      {
        currentCategory = info.ui.category;
        if (!currentCategory.NullOrEmpty())
        {
          using TextBlock catBlock = new(GameFont.Medium);
          float rowHeight = Text.CalcHeight(currentCategory, viewWidth) + 2; // add 2 for gap
          CachedHeight += rowHeight;
          rowCount = 0;
        }
      }
      if (info.ui.subCategory != currentSubCategory)
      {
        currentSubCategory = info.ui.subCategory;
        if (!currentSubCategory.NullOrEmpty())
        {
          using TextBlock subCatBlock = new(GameFont.Small);
          float rowHeight = Text.CalcHeight(currentSubCategory, viewWidth) + 2;
          CachedHeight += rowHeight;
          rowCount = 0;
        }
      }
      using TextBlock fieldFontBlock = new(GameFont.Tiny);
      if (DrawField(info, out _))
      {
        rowCount++;
        if (rowCount > ColumnCount) //Mark for new column
          rowCount = 1;
        if (rowCount == 1) //Only count for first item in row
          CachedHeight += Listing_SplitColumns.GapHeight;
      }
    }
  }

  public override void DoWindowContents(Rect inRect)
  {
    using TextBlock textFont = new(GameFont.Small);
    Rect rect;
    Rect outRect = rect = inRect.ContractedBy(4f);
    rect.xMax -= 33f;
    float viewWidth = rect.width - 16;
    if (CachedHeight < 0)
      RecacheHeight(viewWidth);
    Rect viewRect = new Rect(0f, 0f, viewWidth, CachedHeight).ContractedBy(4);
    // Start ScrollView
    Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
    listing.Begin(viewRect, ColumnCount);
    string currentCategory = string.Empty;
    string currentSubCategory = string.Empty;
    foreach (TweakInfo info in instanceTweaks)
    {
      if (info.ui.category != currentCategory)
      {
        currentCategory = info.ui.category;
        if (!currentCategory.NullOrEmpty())
        {
          listing.Header(currentCategory, ListingExtension.BannerColor, fontSize: GameFont.Medium,
            anchor: TextAnchor.MiddleCenter, rowGap: RowHeight);
        }
      }
      if (info.ui.subCategory != currentSubCategory)
      {
        currentSubCategory = info.ui.subCategory;
        if (!currentSubCategory.NullOrEmpty())
        {
          listing.Header(currentSubCategory, ListingExtension.BannerColor,
            fontSize: GameFont.Small,
            anchor: TextAnchor.MiddleCenter, rowGap: RowHeight);
        }
      }
      using TextBlock fontBlock = new(GameFont.Tiny);
      if (DrawField(info, out bool fieldChange) && fieldChange)
      {
        FieldChanged();
      }
    }
    listing.End();
    Widgets.EndScrollView();
    // End ScrollView

    if (Mouse.IsOver(inRect) && Event.current.type == EventType.ScrollWheel)
    {
      Event.current.Use();
    }
  }

  /// <summary>
  /// Draw field for editing in the TweakValues menu
  /// </summary>
  private bool DrawField(TweakInfo info, out bool fieldChanged)
  {
    fieldChanged = false;
    UISettingsType settingsType = info.ui.settingsType;
    switch (info.ui.settingsType)
    {
      case UISettingsType.None:
        return false;
      case UISettingsType.Checkbox:
        if (info.TryGetValue(out bool checkOn))
        {
          bool checkAfter = checkOn;
          listing.CheckboxLabeled(info.Name, ref checkAfter, string.Empty, string.Empty, false,
            lineHeight: RowHeight);
          if (checkOn != checkAfter)
          {
            info.SetValue(checkAfter);
            fieldChanged = true;
          }
          return true;
        }
        return false;
      case UISettingsType.IntegerBox:
      {
        if (info.TryGetValue(out int value))
        {
          int newValue = value;
          if (info.fieldInfo.TryGetAttribute(out NumericBoxValuesAttribute inputBox))
          {
            listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty,
              min: Mathf.RoundToInt(inputBox.MinValue), max: Mathf.RoundToInt(inputBox.MaxValue),
              lineHeight: RowHeight);
          }
          else
          {
            listing.IntegerBox(info.Name, ref newValue, string.Empty, string.Empty,
              min: 0, max: int.MaxValue, lineHeight: RowHeight);
          }
          if (value != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
        return false;
      }
      case UISettingsType.FloatBox:
      {
        if (info.TryGetValue(out Vector2 vector2))
        {
          Vector2 newValue = vector2;
          listing.Vector2Box(info.Name, ref newValue, labelProportion: VectorLabelProportion,
            subLabelProportions: VectorSubLabelProportion, buffer: 5);
          if (vector2 != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
        else if (info.TryGetValue(out FloatRange floatRange))
        {
          FloatRange newValue = floatRange;
          listing.FloatRangeBox(info.Name, ref newValue,
            labelProportion: FloatRangeLabelProportion,
            subLabelProportions: FloatRangeSubLabelProportion, buffer: 5);
          if (floatRange != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
        else if (info.TryGetValue(out Vector3 vector3))
        {
          Vector3 newValue = vector3;
          listing.Vector3Box(info.Name, ref newValue, labelProportion: VectorLabelProportion,
            subLabelProportions: VectorSubLabelProportion, buffer: 5);
          if (vector3 != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
        else if (info.TryGetValue(out float value))
        {
          float newValue = value;
          if (info.fieldInfo.TryGetAttribute(out NumericBoxValuesAttribute inputBox))
          {
            listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty,
              min: inputBox.MinValue,
              max: inputBox.MaxValue, lineHeight: RowHeight);
          }
          else
          {
            listing.FloatBox(info.Name, ref newValue, string.Empty, string.Empty, min: 0,
              max: float.MaxValue,
              lineHeight: RowHeight);
          }
          if (!Mathf.Approximately(value, newValue))
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
        return false;
      }
      case UISettingsType.ToggleLabel:

        Color color = Color.white;
        Color highlightedColor = new(0.1f, 0.85f, 0.85f);
        Color clickColor = new(highlightedColor.r - 0.15f, highlightedColor.g - 0.15f,
          highlightedColor.b - 0.15f);

        if (info.TryGetValue(out Rot4 rot4))
        {
          if (listing.ClickableLabel(info.Name, rot4.ToStringWord(), highlightedColor, color,
            clickColor: clickColor, lineHeight: RowHeight))
          {
            rot4.Rotate(RotationDirection.Clockwise);
            info.SetValue(rot4);
            fieldChanged = true;
          }
          return true;
        }
        if (info.TryGetValue(out Rot8 rot8))
        {
          if (listing.ClickableLabel(info.Name, rot8.ToStringNamed(), highlightedColor, color,
            clickColor: clickColor, lineHeight: RowHeight))
          {
            rot8.Rotate(RotationDirection.Clockwise);
            info.SetValue(rot8);
            fieldChanged = true;
          }
          return true;
        }
        return false;
      case UISettingsType.SliderEnum:
      {
        if (info.TryGetValue(out int value))
        {
          int newValue = value;
          listing.EnumSliderLabeled(info.Name, ref newValue, string.Empty, string.Empty,
            info.fieldInfo.FieldType);
          if (value != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
      }
        return false;
      case UISettingsType.SliderInt:
      {
        if (info.TryGetValue(out int value))
        {
          int newValue = value;
          if (info.fieldInfo.TryGetAttribute(out SliderValuesAttribute slider))
          {
            listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty,
              slider.EndSymbol, Mathf.RoundToInt(slider.MinValue),
              Mathf.RoundToInt(slider.MaxValue), endValue: (int)slider.EndValue,
              maxValueDisplay: slider.MaxValueDisplay, minValueDisplay: slider.MinValueDisplay);
          }
          else
          {
            Log.WarningOnce(
              $"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no " +
              $"SliderValues attribute. Slider will use default values instead.",
              info.fieldInfo.GetHashCode());
            listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty,
              string.Empty, 0, 100);
          }
          if (value != newValue)
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
      }
        return false;
      case UISettingsType.SliderFloat:
      {
        if (info.TryGetValue(out float value))
        {
          float newValue = value;
          if (info.fieldInfo.TryGetAttribute(out SliderValuesAttribute slider))
          {
            listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty,
              slider.EndSymbol, slider.MinValue, slider.MaxValue,
              decimalPlaces: slider.RoundDecimalPlaces, endValue: slider.EndValue,
              increment: slider.Increment);
          }
          else
          {
            Log.WarningOnce(
              $"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no " +
              $"SliderValues attribute. Slider will use default values instead.",
              info.fieldInfo.GetHashCode());
            listing.SliderLabeled(info.Name, ref newValue, string.Empty, string.Empty,
              string.Empty, 0, 100);
          }
          if (!Mathf.Approximately(value, newValue))
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
      }
        return false;
      case UISettingsType.SliderPercent:
      {
        if (info.TryGetValue(out float value))
        {
          float newValue = value;
          if (info.fieldInfo.TryGetAttribute(out SliderValuesAttribute slider))
          {
            listing.SliderPercentLabeled(info.Name, ref newValue, string.Empty, string.Empty,
              slider.EndSymbol, slider.MinValue, slider.MaxValue,
              decimalPlaces: slider.RoundDecimalPlaces, endValue: slider.EndValue);
          }
          else
          {
            Log.WarningOnce(
              $"Slider declared {info.fieldInfo.DeclaringType}.{info.fieldInfo.Name} with no " +
              $"SliderValues attribute. Slider will use default values instead.",
              info.fieldInfo.GetHashCode());
            listing.SliderPercentLabeled(info.Name, ref newValue, string.Empty, string.Empty, "%",
              0, 1, decimalPlaces: 0);
          }
          if (!Mathf.Approximately(value, newValue))
          {
            info.SetValue(newValue);
            fieldChanged = true;
          }
          return true;
        }
      }
        return false;
      default:
        Log.ErrorOnce(
          $"{settingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify SmashPhil.",
          settingsType.ToString().GetHashCode());
        return false;
    }
  }

  private static void FieldChanged()
  {
    foreach (TweakInfo info in instanceTweaks)
    {
      if (info.instance is ITweakFields tweakFields)
      {
        tweakFields.OnFieldChanged();
      }
    }
  }

  private class UiSettings(string category, string subCategory, UISettingsType settingsType)
  {
    public readonly string category = category;
    public readonly string subCategory = subCategory;
    public readonly UISettingsType settingsType = settingsType;
  }

  private class TweakInfo
  {
    public FieldInfo fieldInfo;
    public object instance;

    public UiSettings ui;

    public string Name
    {
      get
      {
        if (IndexInList >= 0)
        {
          return $"{fieldInfo.Name}_{IndexInList + 1}"; //Xml indices are 1-based
        }
        return fieldInfo.Name;
      }
    }

    public int IndexInList { get; internal set; }

    public bool TryGetValue<T>(out T value) where T : struct
    {
      value = default;
      if (typeof(T) != fieldInfo.FieldType &&
        typeof(T) != Nullable.GetUnderlyingType(fieldInfo.FieldType))
      {
        if (!fieldInfo.FieldType.IsEnum || typeof(T) != typeof(int))
        {
          return false;
        }
      }
      object rawValue = fieldInfo.GetValue(instance);
      if (Nullable.GetUnderlyingType(fieldInfo.FieldType) == typeof(T))
      {
        T? nullableValue = (T?)rawValue;
        if (!nullableValue.HasValue)
        {
          return false;
        }
        rawValue = nullableValue.Value;
      }
      if (rawValue.GetType() != typeof(T) &&
        (!fieldInfo.FieldType.IsEnum || typeof(T) != typeof(int)))
      {
        Log.Error($"Invalid Cast: {rawValue.GetType()} to {typeof(T)}");
        return false;
      }
      value = (T)rawValue;
      return true;
    }

    public void SetValue<T>(T value)
    {
      fieldInfo.SetValue(instance, value);
    }
  }
}