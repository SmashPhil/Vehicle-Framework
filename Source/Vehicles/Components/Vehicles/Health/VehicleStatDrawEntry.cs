using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatDrawEntry
	{
		private StatCategoryDef category;
		public VehicleStatDef stat;

		public string categoryLabel;
		public int categoryDisplayOrder;

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

		public VehicleStatDrawEntry(StatCategoryDef category, string label, string valueString, string reportText, int displayPriorityWithinCategory, 
			string overrideReportTitle = null, IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = null, bool forceUnfinalizedMode = false)
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

		public VehicleStatDrawEntry(string categoryLabel, int categoryDisplayOrder, string label, string valueString, string reportText, int displayPriorityWithinCategory, 
			string overrideReportTitle = null, IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = null, bool forceUnfinalizedMode = false) : this(null, label, valueString, 
				reportText, displayPriorityWithinCategory, overrideReportTitle, hyperlinks, forceUnfinalizedMode)
		{
			this.categoryLabel = categoryLabel;
			this.categoryDisplayOrder = categoryDisplayOrder;
		}

		public string CategoryLabel => category?.LabelCap ?? categoryLabel;

		public int CategoryDisplayOrder => category?.displayOrder ?? categoryDisplayOrder;

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
			if (!hyperlinks.NullOrEmpty())
			{
				foreach (Dialog_InfoCard.Hyperlink hyperlink in hyperlinks)
				{
					yield return hyperlink;
				}
			}
			if (stat != null && vehicle != null)
			{
				foreach (Dialog_InfoCard.Hyperlink hyperlink in stat.Worker.GetInfoCardHyperlinks(vehicle))
				{
					yield return hyperlink;
				}
			}
		}

		public string GetExplanationText(VehicleDef vehicleDef, VehiclePawn forVehicle = null)
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
				result = $"{explanationText}{Environment.NewLine}{Environment.NewLine}{stat.Worker.GetExplanationFull(vehicleDef, numberSense, value, forVehicle)}";
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
			float textWidth = width * 0.45f;
			Rect rect = new Rect(x, y, width, Text.CalcHeight(ValueString, textWidth));
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
				Rect labelRect = rect;
				labelRect.width -= textWidth;
				Widgets.Label(labelRect, LabelCap);
				Rect valueRect = rect;
				valueRect.xMin = labelRect.xMax;
				Widgets.Label(valueRect, ValueString);
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
