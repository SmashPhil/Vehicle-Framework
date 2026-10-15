using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools
{
  [StaticConstructorOnStartup]
  public static class ListingExtension
  {
    public static readonly Color HighlightColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    public static readonly Color BannerColor = new Color(0f, 0f, 0f, 0.25f);
    public static readonly Color MenuSectionBGFillColor = new ColorInt(42, 43, 44).ToColor;
    public static readonly Color LightHighlightColor = new Color(1f, 1f, 1f, 0.04f);

    public static readonly Texture2D ButtonBGAtlas =
      ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");

    public static readonly Texture2D ButtonBGAtlasMouseover =
      ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");

    public static readonly Texture2D ButtonBGAtlasClick =
      ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");

    public static void CheckboxLabeledWithMessage(this Listing_Standard lister, string label,
      Func<bool, Message> messageGetter, ref bool checkOn, string tooltip = null)
    {
      bool before = checkOn;
      lister.CheckboxLabeled(label, ref checkOn, tooltip);
      if (checkOn != before)
      {
        Message message = messageGetter(checkOn);
        if (message != null)
        {
          Messages.Message(message, false);
        }
      }
    }

    public static bool ReverseRadioButton(this Listing_Standard lister, string label, bool enabled,
      string tooltip = null, float? tooltipDelay = null)
    {
      float lineHeight = Text.LineHeight;
      Rect rect = lister.GetRect(lineHeight, 1f);
      if (lister.BoundingRectCached != null && !rect.Overlaps(lister.BoundingRectCached.Value))
      {
        return false;
      }
      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(rect))
        {
          Widgets.DrawHighlight(rect);
        }
        TipSignal tip = tooltipDelay != null ?
          new TipSignal(tooltip, tooltipDelay.Value) :
          new TipSignal(tooltip);
        TooltipHandler.TipRegion(rect, tip);
      }
      bool result = UIElements.ReverseRadioButton(rect, label, enabled);
      lister.Gap(lister.verticalSpacing);
      return result;
    }

    public static void IntegerBox(this Listing lister, string text, string tooltip, ref int value,
      float labelLength, float padding, int min = int.MinValue, int max = int.MaxValue)
    {
      Rect rect = lister.GetRect(Text.LineHeight);

      Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
      Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y,
        rect.width - labelLength - padding, rect.height);

      Widgets.Label(rectLeft, text);

      using var textBlock = new TextBlock(Text.Anchor = TextAnchor.MiddleLeft);
      string buffer = value.ToString();
      Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(rect))
        {
          Widgets.DrawHighlight(rect);
        }
        TooltipHandler.TipRegion(rect, tooltip);
      }
    }

    public static void Numericbox(this Listing lister, string text, string tooltip, ref float value,
      float labelLength, float padding, float min = -1E+09f, float max = 1E+09f)
    {
      lister.Gap(12f);
      Rect rect = lister.GetRect(Text.LineHeight);

      Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
      Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y,
        rect.width - labelLength - padding, rect.height);

      Widgets.Label(rectLeft, text);

      using var textBlock = new TextBlock(Text.Anchor = TextAnchor.MiddleLeft);
      string buffer = value.ToString();
      Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(rect))
        {
          Widgets.DrawHighlight(rect);
        }
        TooltipHandler.TipRegion(rect, tooltip);
      }
    }

    public static void EnumSliderLabeled<T>(this Listing lister, string label, ref T value,
      string tooltip, string disabledTooltip, Func<T, string> valueNameGetter = null) where T : Enum
    {
      using var textBlock = new TextBlock(Color.white);
      try
      {
        int[] enumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray();
        int min = enumValues[0];
        int max = enumValues.Last();
        Rect rect = lister.GetRect(24f);
        Rect fullRect = rect;
        rect.y += rect.height / 2;
        string rightLabel = valueNameGetter?.Invoke(value) ?? Enum.GetName(typeof(T), value);
        if (!disabledTooltip.NullOrEmpty())
        {
          GUI.enabled = false;
          TooltipHandler.TipRegion(fullRect, disabledTooltip);
        }
        else if (!tooltip.NullOrEmpty())
        {
          if (Mouse.IsOver(fullRect))
          {
            Widgets.DrawHighlight(fullRect);
          }
          TooltipHandler.TipRegion(fullRect, tooltip);
        }
        GUI.enabled = true;
        int boxedValue = (int)Convert.ChangeType(value, typeof(int));
        boxedValue = (int)Widgets.HorizontalSlider(rect, boxedValue, min, max,
          middleAlignment: false, label: null, leftAlignedLabel: label,
          rightAlignedLabel: rightLabel);
        value = (T)Enum.ToObject(typeof(T), boxedValue);
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown for slider of enum type = {typeof(T)}.\nException={ex}");
        return;
      }
    }

    public static float SliderLabeled(this Listing lister, string label, string tooltip,
      string endSymbol, float value, float min, float max, float multiplier = 1f,
      int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
    {
      SliderLabeled(lister, label, tooltip, endSymbol, ref value, min, max, multiplier: multiplier,
        decimalPlaces: decimalPlaces, endValue: endValue, maxValueDisplay: maxValueDisplay);
      return value;
    }

    // TODO 1.7 - Cleanup
    public static void SliderLabeled(this Listing lister, string label, string tooltip,
      string endSymbol, ref float value, float min, float max, float multiplier = 1f,
      int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
    {
      lister.SliderLabeled(label, tooltip, string.Empty, endSymbol, ref value, min, max,
        multiplier: multiplier, decimalPlaces: decimalPlaces, endValue: endValue, maxValueDisplay: maxValueDisplay);
    }

    // TODO 1.7 - Cleanup
    public static void SliderLabeled(this Listing lister, string label, string tooltip, string startSymbol,
      string endSymbol, ref float value, float min, float max, float multiplier = 1f,
      int decimalPlaces = 2, float endValue = -1f, string maxValueDisplay = "")
    {
      lister.Gap(12f);
      Rect rect = lister.GetRect(24f);
      Rect fullRect = rect;
      rect.y += rect.height / 2;

      string format = $"{startSymbol}{Math.Round(value * multiplier, decimalPlaces)}{endSymbol}";
      if (!maxValueDisplay.NullOrEmpty() && endValue > 0)
      {
        if (value >= endValue)
        {
          format = maxValueDisplay;
        }
      }
      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(fullRect))
        {
          Widgets.DrawHighlight(fullRect);
        }
        TooltipHandler.TipRegion(fullRect, tooltip);
      }
      value = Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null,
        leftAlignedLabel: label, rightAlignedLabel: format);
      if (endValue > 0 && value >= max)
      {
        value = endValue;
      }
    }

    public static void SliderLabeled(this Listing lister, string label, string tooltip,
      string endSymbol, ref int value, int min, int max, int roundTo = 1,
      string maxValueDisplay = "", string minValueDisplay = "")
    {
      lister.Gap(12f);
      Rect rect = lister.GetRect(24f);
      Rect fullRect = new Rect(rect);
      rect.y += rect.height / 2;

      string format = string.Format("{0}" + endSymbol, value);
      if (!maxValueDisplay.NullOrEmpty())
      {
        if (value == max)
        {
          format = maxValueDisplay;
        }
      }
      if (!minValueDisplay.NullOrEmpty())
      {
        if (value == min)
        {
          format = minValueDisplay;
        }
      }
      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(fullRect))
        {
          Widgets.DrawHighlight(fullRect);
        }
        TooltipHandler.TipRegion(fullRect, tooltip);
      }
      value = (int)Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false,
        label: null, leftAlignedLabel: label, rightAlignedLabel: format).RoundTo(roundTo);
    }

    public static void SliderPercentLabeled(this Listing listing, string label, string tooltip,
      string endSymbol, ref float value, float min, float max, int decimalPlaces = 2,
      float endValue = -1f, string endValueDisplay = "")
    {
      Rect rect = listing.GetRect(24f);
      Rect fullRect = rect;
      rect.y += rect.height / 2;
      string format = $"{Math.Round(value * 100, decimalPlaces)}" + endSymbol;

      if (!endValueDisplay.NullOrEmpty() && endValue > 0)
      {
        if (value >= endValue)
        {
          format = endValueDisplay;
        }
      }
      bool mouseOver = Mouse.IsOver(fullRect);

      if (!tooltip.NullOrEmpty())
      {
        if (mouseOver)
        {
          Widgets.DrawHighlight(fullRect);
        }
        TooltipHandler.TipRegion(fullRect, tooltip);
      }

      value = Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null,
        leftAlignedLabel: label, rightAlignedLabel: format);
      if (endValue > 0 && value >= max)
      {
        value = endValue;
      }
    }

    public static void Header(this Listing lister, string header, Color highlight,
      GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft,
      float rowGap = 22 /* Average size of GameFont.Small */)
    {
      using TextBlock textBlock = new(fontSize);
      Listing_SplitColumns splitLister = lister as Listing_SplitColumns;
      splitLister?.NextRow(rowGap);
      Rect rect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
      UIElements.Header(rect, header, highlight, fontSize: fontSize, anchor: anchor);

      splitLister?.Gap(2);
    }

    public static void Vector2Box(this Listing lister, string label, ref Vector2 value,
      string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
    {
      value = lister.Vector2Box(label, value, tooltip, labelProportion, subLabelProportions);
    }

    public static Vector2 Vector2Box(this Listing lister, string label, Vector2 value,
      string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
    {
      Rect rect = lister.GetRect(24);
      UIElements.Vector2Box(rect, label, value, tooltip, labelProportion, subLabelProportions);
      return value;
    }

    public static void Vector3Box(this Listing lister, string label, ref Vector3 value,
      string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
    {
      value = lister.Vector3Box(label, value, tooltip, labelProportion, subLabelProportions);
    }

    public static Vector3 Vector3Box(this Listing lister, string label, Vector3 value,
      string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f)
    {
      Rect rect = lister.GetRect(24);
      UIElements.Vector3Box(rect, label, value, tooltip, labelProportion, subLabelProportions);
      return value;
    }

    public static bool ListItemSelectable(this Listing lister, string header, Color hoverColor,
      bool selected = false, bool active = true, string disabledTooltip = null)
    {
      using var textBlock = new TextBlock(Color.white);
      Rect rect = lister.GetRect(20f);

      if (selected)
      {
        Widgets.DrawBoxSolid(rect, HighlightColor);
      }

      GUI.color = Color.white;
      if (!active)
      {
        GUI.color = Color.grey;
      }
      else if (Mouse.IsOver(rect))
      {
        GUI.color = hoverColor;
      }
      if (!disabledTooltip.NullOrEmpty())
      {
        TooltipHandler.TipRegion(rect, disabledTooltip);
      }

      Text.Anchor = TextAnchor.MiddleLeft;
      Widgets.Label(rect, header);

      if (active && Widgets.ButtonInvisible(rect, true))
      {
        SoundDefOf.Click.PlayOneShotOnCamera();
        return true;
      }
      return false;
    }

    public static bool ClickableLabel(this Listing lister, string label,
      bool doMouseoverSound = false)
    {
      using TextBlock textBlock = new(TextAnchor.MiddleLeft, Color.white);

      Rect rect = lister.GetRect(Text.LineHeight);

      if (Mouse.IsOver(rect))
        Widgets.DrawHighlight(rect);

      Widgets.Label(rect, label);
      lister.Gap(lister.verticalSpacing);
      return Widgets.ButtonInvisible(rect, doMouseoverSound: doMouseoverSound);
    }

    public static bool CheckboxLabeledReturned(this Listing_Standard lister, string label,
      ref bool checkOn, string tooltip = null)
    {
      Rect rect = lister.GetRect(Text.LineHeight);
      if (!tooltip.NullOrEmpty())
      {
        if (Mouse.IsOver(rect))
        {
          Widgets.DrawHighlight(rect);
        }
        TooltipHandler.TipRegion(rect, tooltip);
      }

      using TextBlock textBlock = new(Text.Anchor = TextAnchor.MiddleLeft);
      Widgets.Label(rect, label);
      bool clicked = false;
      if (Widgets.ButtonInvisible(rect, true))
      {
        checkOn = !checkOn;
        clicked = true;
        if (checkOn)
        {
          SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(null);
        }
        else
        {
          SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(null);
        }
      }
      Texture2D image = checkOn ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
      GUI.DrawTexture(new Rect(rect.x + rect.width - 24f, rect.y, 24, 24), image);

      lister.Gap(lister.verticalSpacing);
      return clicked;
    }
  }
}