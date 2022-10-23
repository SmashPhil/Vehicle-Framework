using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class VehicleStatDrawEntry
	{
		public StatCategoryDef category;
		public VehicleStatDef stat;

		private int displayOrderWithinCategory;
		private float value;
		public bool forceUnfinalizedMode;

		private IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks;
		private string label;
		private string valueString;
		private string explanationText;
		private ToStringNumberSense numberSense;
		private string overrideReportText;
		private string overrideReportTitle;

		public VehicleStatDrawEntry(StatCategoryDef category, VehicleStatDef stat, float value)
		{
			this.category = category;
			this.stat = stat;
			label = null;
			this.value = value;
			valueString = null;
			displayOrderWithinCategory = stat.displayPriorityInCategory;
		}

		public VehicleStatDrawEntry(StatCategoryDef category, string label, string valueString, string reportText, int displayPriorityWithinCategory, string overrideReportTitle = null, IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = null, bool forceUnfinalizedMode = false)
		{
			this.category = category;
			stat = null;
			this.label = label;
			value = 0f;
			this.valueString = valueString;
			displayOrderWithinCategory = displayPriorityWithinCategory;
			numberSense = ToStringNumberSense.Absolute;
			overrideReportText = reportText;
			this.overrideReportTitle = overrideReportTitle;
			this.hyperlinks = hyperlinks;
			this.forceUnfinalizedMode = forceUnfinalizedMode;
		}

		public VehicleStatDrawEntry(StatCategoryDef category, VehicleStatDef stat)
		{
			this.category = category;
			this.stat = stat;
			label = null;
			value = 0f;
			valueString = "-";
			displayOrderWithinCategory = stat.displayPriorityInCategory;
		}

		public bool ShouldDisplay => stat == null || !Mathf.Approximately(value, stat.hideAtValue);

		public int DisplayPriorityWithinCategory => displayOrderWithinCategory;

		public string LabelCap
		{
			get
			{
				if (label != null)
				{
					return label.CapitalizeFirst();
				}
				return stat.LabelCap;
			}
		}

		public string ValueString
		{
			get
			{
				if (numberSense == ToStringNumberSense.Factor)
				{
					return value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Absolute);
				}
				if (valueString == null)
				{
					return stat.Worker.GetStatDrawEntryLabel(stat, value, numberSense, !forceUnfinalizedMode);
				}
				return valueString;
			}
		}

		public IEnumerable<Dialog_InfoCard.Hyperlink> GetHyperlinks(VehiclePawn vehicle)
		{
			if (hyperlinks != null)
			{
				return hyperlinks;
			}
			if (stat != null)
			{
				return stat.Worker.GetInfoCardHyperlinks(vehicle);
			}
			return null;
		}

		public string GetExplanationText(VehiclePawn vehicle)
		{
			if (explanationText == null)
			{
				WriteExplanationTextInt();
			}
			string result;
			if (stat == null)
			{
				result = explanationText;
			}
			else
			{
				result = $"{explanationText}{Environment.NewLine}{Environment.NewLine}{stat.Worker.GetExplanationFull(vehicle, numberSense, value)}";
			}
			return result;
		}

		private void WriteExplanationTextInt()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!overrideReportTitle.NullOrEmpty())
			{
				stringBuilder.AppendLine(overrideReportTitle);
			}
			if (!overrideReportText.NullOrEmpty())
			{
				stringBuilder.AppendLine(overrideReportText);
			}
			else if (stat != null)
			{
				stringBuilder.AppendLine(stat.description);
			}
			stringBuilder.AppendLine();
			explanationText = stringBuilder.ToString().TrimEndNewlines();
		}

		public VehicleStatDrawEntry SetReportText(string reportText)
		{
			overrideReportText = reportText;
			return this;
		}

		public float Draw(float x, float y, float width, bool selected, bool highlightLabel, bool lowlightLabel, Action clickedCallback, Action mousedOverCallback, Vector2 scrollPosition, Rect scrollOutRect)
		{
			float num = width * 0.45f;
			Rect rect = new Rect(8f, y, width, Text.CalcHeight(ValueString, num));
			if (y - scrollPosition.y + rect.height >= 0f && y - scrollPosition.y <= scrollOutRect.height)
			{
				GUI.color = Color.white;
				if (selected)
				{
					Widgets.DrawHighlightSelected(rect);
				}
				else if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				if (highlightLabel)
				{
					Widgets.DrawTextHighlight(rect, 4f);
				}
				if (lowlightLabel)
				{
					GUI.color = Color.grey;
				}
				Rect rect2 = rect;
				rect2.width -= num;
				Widgets.Label(rect2, LabelCap);
				Rect rect3 = rect;
				rect3.x = rect2.xMax;
				rect3.width = num;
				Widgets.Label(rect3, ValueString);
				GUI.color = Color.white;
				if (stat != null && Mouse.IsOver(rect))
				{
					TooltipHandler.TipRegion(rect, new TipSignal(() => stat.LabelCap + ": " + stat.description, stat.GetHashCode()));
				}
				if (Widgets.ButtonInvisible(rect, true))
				{
					clickedCallback();
				}
				if (Mouse.IsOver(rect))
				{
					mousedOverCallback();
				}
			}
			return rect.height;
		}

		public bool Matching(VehicleStatDrawEntry entry)
		{
			return entry != null && (this == entry || (stat == entry.stat && label == entry.label));
		}

		public override string ToString()
		{
			return $"({LabelCap}: {ValueString})";
		}
	}
}
