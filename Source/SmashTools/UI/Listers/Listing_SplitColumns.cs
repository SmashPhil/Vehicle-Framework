using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace SmashTools
{
	public class Listing_SplitColumns : Listing
	{
		public const float GapHeight = 34;
		public const float DefSelectionLineHeight = 21f;
		public const float ColumnSplitWidth = 0.05f;
		

		protected readonly GameFont font;
		protected int columns = 2;
		protected int curColumn;
		public bool shiftRectScrollbar;
		public float columnGap = 4;

		private GameFont oldFont;

		public Listing_SplitColumns(GameFont font)
		{
			this.font = font;
		}

		public Listing_SplitColumns()
		{
			font = GameFont.Tiny;
		}

		protected int CurrentColumn => curColumn % columns;

		public override void Begin(Rect rect)
		{
			if (shiftRectScrollbar)
			{
				rect.width -= 10;
			}
			base.Begin(rect);
			oldFont = Text.Font;
			Text.Font = font;
			curColumn = 0;
		}

		public void Begin(Rect rect, int columns)
		{
			Begin(rect);
			this.columns = columns;
		}

		public void BeginScrollView(Rect rect, ref Vector2 scrollPosition, ref Rect viewRect, int columns)
		{
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			rect.height = 100000f;
			Begin(rect, columns);
		}

		public override void End()
		{
			Text.Font = oldFont;
			base.End();
		}

		public void EndScrollView(ref Rect viewRect)
		{
			viewRect = new Rect(0f, 0f, listingRect.width, curY);
			Widgets.EndScrollView();
			End();
		}

		public virtual void NextRow(float gapHeight = 16)
		{
			if (curColumn > 0)
			{
				curColumn = 0;
				curX = 0;
				curY += gapHeight;
			}
		}

		public Rect GetSplitRect(float height)
		{
			NewColumnIfNeeded(height);
			Rect result = new Rect(curX + 2, curY, ColumnWidth / (columns + (columns * columns * ColumnSplitWidth)) - columnGap, height);
			return result;
		}

		public Rect GetCurrentRect(float height)
		{
			return new Rect(curX, curY, ColumnWidth / (columns + (columns * columns * ColumnSplitWidth)) - columnGap, height);
		}

		public virtual void Shift(float gapHeight = 34)
		{
			if (curColumn > 0)
			{
				if (CurrentColumn == 0)
				{
					curX = 0;
					curY += gapHeight;
				}
				else
				{
					curX = ColumnWidth / (columns - (columns * ColumnSplitWidth)) * CurrentColumn;
				}

			}
			curColumn++;
		}

		public void Header(string header, GameFont fontSize = GameFont.Medium, TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			Shift();
			using (new TextBlock(fontSize))
			{
				Text.Font = fontSize;
				Rect rect = GetSplitRect(Text.CalcHeight(header, ColumnWidth));
				UIElements.Header(rect, header, ListingExtension.BannerColor, fontSize: fontSize, anchor: anchor);
			}
		}

		public bool ClickableLabel(string label, string value, Color mouseOver, Color textColor, Color? clickColor = null, float? lineHeight = null)
		{
			Shift();
			using (new TextBlock(GameFont.Tiny))
			{
				float height = lineHeight ?? Text.LineHeight;
				Rect rect = GetSplitRect(height);
				Rect rectLeft = new Rect(rect.x, rect.y, rect.width / 2, rect.height);
				Rect rectRight = new Rect(rect.x + rectLeft.width, rect.y, rect.width / 2, rect.height);

				if (Mouse.IsOver(rectRight))
				{
					GUI.color = mouseOver;
					if (Input.GetMouseButton(0))
					{
						clickColor ??= Color.grey;
						GUI.color = clickColor.Value;
					}
				}
				else
				{
					GUI.color = textColor;
				}
				if (!label.NullOrEmpty())
				{
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rectLeft, label);
				}
				Text.Anchor = TextAnchor.MiddleRight;
				Widgets.Label(rectRight, value);

				if (Widgets.ButtonInvisible(rectRight))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					return true;
				}
				return false;
			}
		}

		public bool Button(string label, float height = 24f, string highlightTag = null)
		{
			Shift();
			Rect rect = GetSplitRect(height);
			bool result = Widgets.ButtonText(rect, label);
			if (highlightTag != null)
			{
				UIHighlighter.HighlightOpportunity(rect, highlightTag);
			}
			return result;
		}

		public void CheckboxLabeled(string label, ref bool checkState, string tooltip, string disabledTooltip, bool locked, float? lineHeight = null)
		{
			Shift();
			float height = lineHeight ?? Text.LineHeight;
			Rect rect = GetSplitRect(height);
			bool disabled = !disabledTooltip.NullOrEmpty();
			if (disabled)
			{
				TooltipHandler.TipRegion(rect, disabledTooltip);
			}
			else if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				TooltipHandler.TipRegion(rect, tooltip);
			}
			if (locked)
			{
				checkState = false;
			}
			UIElements.CheckboxLabeled(rect, label, ref checkState, disabled);
		}

		public void IntegerBox(string label, ref int value, string tooltip, string disabledTooltip, int min = int.MinValue, int max = int.MaxValue, float? lineHeight = null, float labelProportion = 0.75f)
		{
			Shift();
			float height = lineHeight ?? Text.LineHeight;
			Rect rect = GetSplitRect(height);
			UIElements.NumericBox(rect, ref value, label, tooltip, disabledTooltip, min: min, max: max, labelProportion: labelProportion);
		}

		public void FloatBox(string label, ref float value, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue, float? lineHeight = null, float labelProportion = 0.75f)
		{
			Shift();
			float height = lineHeight ?? Text.LineHeight;
			Rect rect = GetSplitRect(height);
			UIElements.NumericBox(rect, ref value, label, tooltip, disabledTooltip, min: min, max: max, labelProportion: labelProportion);
		}

		public void SliderPercentLabeled(string label, ref float value, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			using (new TextBlock(Color.white))
			{
				Rect rect = GetSplitRect(24f);
				string format = $"{Math.Round(value * 100, decimalPlaces)}" + endSymbol;
				if (!endValueDisplay.NullOrEmpty() && endValue > 0)
				{
					if (value >= endValue)
					{
						format = endValueDisplay;
						if (translate)
						{
							format = format.Translate();
						}
					}
				}
				if (!disabledTooltip.NullOrEmpty())
				{
					GUIState.Disable();
					TooltipHandler.TipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (Mouse.IsOver(rect))
					{
						Widgets.DrawHighlight(rect);
					}
					TooltipHandler.TipRegion(rect, tooltip);
				}
				value = Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
				float value2 = value;
				if (endValue > 0 && value2 >= max)
				{
					value2 = endValue;
				}
				GUIState.Enable();
			}
		}

		public float SliderLabeled(string label, float value, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2,
			float endValue = -1f, float increment = 0, string endValueDisplay = "", bool translate = false)
		{
			SliderLabeled(label, ref value, tooltip, disabledTooltip, endSymbol, min, max, decimalPlaces, endValue: endValue, increment: increment, endValueDisplay: endValueDisplay, translate: translate);
			return value;
		}

		public void SliderLabeled(string label, ref float value, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, float increment = 0, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			Rect rect = GetSplitRect(24f);
			Rect fullRect = rect;
			rect.y += rect.height / 2;
			string format = $"{Math.Round(value, decimalPlaces)}" + endSymbol;
			if (!endValueDisplay.NullOrEmpty())
			{
				if (value >= max)
				{
					format = endValueDisplay;
					if (translate)
					{
						format = format.Translate();
					}
				}
			}
			if (!disabledTooltip.NullOrEmpty())
			{
				GUIState.Disable();
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
			value = Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
			float value2 = value;
			if (increment > 0)
			{
				value = value.RoundTo(increment);
				value2 = value2.RoundTo(increment);
			}
			if (endValue > 0 && value2 >= max)
			{
				value2 = endValue;
			}
			GUIState.Enable();
		}

		public int SliderLabeled(string label, int value, string tooltip, string disabledTooltip, string endSymbol, int min, int max,
			int endValue = -1, string maxValueDisplay = "", string minValueDisplay = "", bool translate = false)
		{
			SliderLabeled(label, ref value, tooltip, disabledTooltip, endSymbol, min, max, endValue: endValue, maxValueDisplay: maxValueDisplay, minValueDisplay: minValueDisplay, translate: translate);
			return value;
		}

		public void SliderLabeled(string label, ref int value, string tooltip, string disabledTooltip, string endSymbol, int min, int max, 
			int endValue = -1, string maxValueDisplay = "", string minValueDisplay = "", bool translate = false)
		{
			Shift();
			Rect rect = GetSplitRect(24f);
			Rect fullRect = rect;
			rect.y += rect.height / 2;
			string format = string.Format("{0}" + endSymbol, value);
			if (!maxValueDisplay.NullOrEmpty())
			{
				if (value == max)
				{
					format = maxValueDisplay;
					if (translate)
					{
						format = format.Translate();
					}
				}
			}
			if (!minValueDisplay.NullOrEmpty())
			{
				if (value == min)
				{
					format = minValueDisplay;
					if (translate)
					{
						format = format.Translate();
					}
				}
			}
			if (!disabledTooltip.NullOrEmpty())
			{
				GUIState.Disable();
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
			value = (int)Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
			int value2 = value;
			if (value2 >= max && endValue > 0)
			{
				value2 = endValue;
			}
			GUIState.Enable();
		}

		public void EnumSliderLabeled(string label, ref int value, string tooltip, string disabledTooltip, Type enumType, bool translate = false)
		{
			Shift();
			int[] enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
			string[] enumNames = Enum.GetNames(enumType);
			int min = enumValues[0];
			int max = enumValues.Last();
			Rect rect = GetSplitRect(24f);
			Rect fullRect = rect;
			rect.y += rect.height / 2;
			string format = Enum.GetName(enumType, value);
			if (translate)
			{
				format = format.Translate();
			}
			if (!disabledTooltip.NullOrEmpty())
			{
				GUIState.Disable();
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
			value = (int)Widgets.HorizontalSlider(rect, value, min, max, middleAlignment: false, label: null, leftAlignedLabel: label, rightAlignedLabel: format);
			GUIState.Enable();
		}

		public void FloatRangeBox(string label, ref FloatRange value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.25f, float buffer = 0)
		{
			value = FloatRangeBox(label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
		}

		public FloatRange FloatRangeBox(string label, FloatRange value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.25f, float buffer = 0)
		{
			Shift();
			Rect rect = GetSplitRect(24);
			value = UIElements.FloatRangeBox(rect, label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);

			return value;
		}

		public void Vector2Box(string label, ref Vector2 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			value = Vector2Box(label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
		}

		public Vector2 Vector2Box(string label, Vector2 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			Shift();
			Rect rect = GetSplitRect(24);
			value = UIElements.Vector2Box(rect, label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);

			return value;
		}

		public void Vector3Box(string label, ref Vector3 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			value = Vector3Box(label, value, tooltip, labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
		}

		public Vector3 Vector3Box(string label, Vector3 value, string tooltip = null, float labelProportion = 0.5f, float subLabelProportions = 0.15f, float buffer = 0)
		{
			Shift();
			Rect rect = GetSplitRect(24);
			value = UIElements.Vector3Box(rect, label, value, tooltip, labelProportion: labelProportion, subLabelProportions: subLabelProportions, buffer: buffer);
			return value;
		}

		public Rect Label(string label, float maxHeight = -1f, string tooltip = null)
		{
			float num = Text.CalcHeight(label, ColumnWidth);
			Shift(num);
			if (maxHeight >= 0f && num > maxHeight)
			{
				num = maxHeight;
			}
			Rect rect = GetSplitRect(num);
			Widgets.Label(rect, label);
			if (tooltip != null)
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			return rect;
		}
	}
}
