using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Listing_Settings : Listing_SplitColumns
	{
		public SettingsPage settings;

		public Listing_Settings(SettingsPage settings, GameFont font) : base(font)
		{
			this.settings = settings;
		}

		public Listing_Settings(SettingsPage settings)
		{
			this.settings = settings;
		}

		public Listing_Settings() : base()
		{
			settings = SettingsPage.Vehicles;
		}

		public object GetSettingsValue(VehicleDef def, SaveableField field)
		{
			try
			{
				return settings switch
				{
					SettingsPage.Vehicles => VehicleMod.settings.vehicles.fieldSettings[def.defName][field].First,
					SettingsPage.Upgrades => VehicleMod.settings.upgrades.upgradeSettings[def.defName][field].First,
					_ => throw new NotSupportedException($"Cannot use Listing_Settings with settings set to {settings}")
				};
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to retrieve field {field.name} for {def.defName}. Settings=\"{settings}\"");
				throw ex;
			}
		}

		public void SetSettingsValue<T>(VehicleDef def, SaveableField field, T value1, T value2)
		{
			switch (settings)
			{
				case SettingsPage.Vehicles:
					VehicleMod.settings.vehicles.fieldSettings[def.defName][field] = new SavedField<object>(value1, value2);
					break;
				case SettingsPage.Upgrades:
					VehicleMod.settings.upgrades.upgradeSettings[def.defName][field] = new SavedField<object>(value1, value2);
					break;
				default:
					throw new NotSupportedException($"Cannot use Listing_SplitColumns with settings set to {settings}");
			}
		}

		private void SetSettingsValue<T>(VehicleDef def, SaveableField field, T valueDup)
		{
			switch (settings)
			{
				case SettingsPage.Vehicles:
					VehicleMod.settings.vehicles.fieldSettings[def.defName][field] = new SavedField<object>(valueDup);
					break;
				case SettingsPage.Upgrades:
					VehicleMod.settings.upgrades.upgradeSettings[def.defName][field] = new SavedField<object>(valueDup);
					break;
				default:
					throw new NotSupportedException($"Cannot use Listing_SplitColumns with settings set to {settings}");
			}
		}

		public void ListLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, SettingsValueInfo settingsInfo, Func<int, string> subLabelGetter,
			Func<int, string> subTooltipGetter, Func<int, string> subDisabledTooltipGetter)
		{
			this.Header(label, ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);

			Shift();
			Rect rect = GetSplitRect(Text.LineHeight);
			rect.y -= rect.height / 2;

			Color color = GUI.color;
			if (!disabledTooltip.NullOrEmpty())
			{
				GUI.color = UIElements.InactiveColor;
				GUI.enabled = false;
				UIElements.DoTooltipRegion(rect, disabledTooltip);
			}
			else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip);
			}

			IList iList = GetSettingsValue(def, field) as IList;
			
			for (int i = 0; i < iList.Count; i++)
			{
				Shift();
				Rect itemRect = GetSplitRect(Text.LineHeight);
				itemRect.y -= itemRect.height / 2;
				object value = iList[i];
				string subLabel = subLabelGetter?.Invoke(i) ?? i.ToString();
				string subTooltip = subTooltipGetter?.Invoke(i);
				string subDisabledTooltip = subDisabledTooltipGetter?.Invoke(i);
				switch (settingsInfo.settingsType)
				{
					case UISettingsType.None:
						break;
					case UISettingsType.Checkbox:
						{
							bool refValue = (bool)value;
							CheckboxLabeled(subLabel, ref refValue, subTooltip, subDisabledTooltip, false);
							iList[i] = refValue;
						}
						break;
					case UISettingsType.IntegerBox:
						{
							int refValue = (int)value;
							IntegerBox(subLabel, ref refValue, subTooltip, subDisabledTooltip, Mathf.RoundToInt(settingsInfo.minValue), Mathf.RoundToInt(settingsInfo.maxValue));
							iList[i] = refValue;
						}
						break;
					case UISettingsType.FloatBox:
						{
							float refValue = (float)value;
							FloatBox(subLabel, ref refValue, subTooltip, subDisabledTooltip, settingsInfo.minValue, settingsInfo.maxValue);
							iList[i] = refValue;
						}
						break;
					case UISettingsType.ToggleLabel:
						break;
					case UISettingsType.SliderEnum:
						break;
					case UISettingsType.SliderInt:
						break;
					case UISettingsType.SliderFloat:
						{
							float refValue = (float)value;
							SliderLabeled(subLabel, ref refValue, subTooltip, subDisabledTooltip, settingsInfo.endSymbol, settingsInfo.minValue, settingsInfo.maxValue, settingsInfo.roundDecimalPlaces,
								settingsInfo.endValue, settingsInfo.increment);
							iList[i] = refValue;
						}
						break;
					case UISettingsType.SliderPercent:
						break;
					default:
						throw new NotImplementedException();
				}
			}

			GUI.color = color;
			GUI.enabled = true;

			SetSettingsValue(def, field, iList);
		}

		public void CheckboxLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, bool locked)
		{
			Shift();
			Rect rect = GetSplitRect(Text.LineHeight);
			rect.y -= rect.height / 2;
			bool disabled = !disabledTooltip.NullOrEmpty();
			if (disabled)
			{
				UIElements.DoTooltipRegion(rect, disabledTooltip);
			}
			else if (!tooltip.NullOrEmpty())
			{
				if (Mouse.IsOver(rect))
				{
					Widgets.DrawHighlight(rect);
				}
				UIElements.DoTooltipRegion(rect, tooltip);
			}
			bool checkState = (bool)GetSettingsValue(def, field);
			if (locked)
			{
				checkState = false;
			}
			UIElements.CheckboxLabeled(rect, label, ref checkState, disabled);
			SetSettingsValue(def, field, checkState);
		}

		public void IntegerBox(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, int min = int.MinValue, int max = int.MaxValue)
		{
			Shift();
			int value = Convert.ToInt32(GetSettingsValue(def, field));
			
			Rect rect = GetSplitRect(Text.LineHeight);
			float centerY = rect.y - rect.height / 2;
			float length = rect.width * 0.45f;
			Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
			Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, rect.height);

			Color color = GUI.color;
			if (!disabledTooltip.NullOrEmpty())
			{
				GUI.color = UIElements.InactiveColor;
				GUI.enabled = false;
				UIElements.DoTooltipRegion(rect, disabledTooltip);
			}
			else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip);
			}
			Widgets.Label(rectLeft, label);

			var align = Text.CurTextFieldStyle.alignment;
			Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
			string buffer = value.ToString();
			Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

			Text.CurTextFieldStyle.alignment = align;
			GUI.color = color;
			GUI.enabled = true;

			SetSettingsValue(def, field, value);
		}

		public void FloatBox(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue)
		{
			Shift();
			float value = Convert.ToSingle(GetSettingsValue(def, field));
			Rect rect = GetSplitRect(Text.LineHeight);
			float centerY = rect.y - rect.height / 2;
			float length = rect.width * 0.45f;
			Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
			Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, rect.height);

			Color color = GUI.color;
			if (!disabledTooltip.NullOrEmpty())
			{
				GUI.color = UIElements.InactiveColor;
				GUI.enabled = false;
				UIElements.DoTooltipRegion(rect, disabledTooltip);
			}
			else if (!tooltip.NullOrEmpty())
			{
				UIElements.DoTooltipRegion(rect, tooltip);
			}
			Widgets.Label(rectLeft, label);

			var align = Text.CurTextFieldStyle.alignment;
			Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
			string buffer = value.ToString();

			Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

			Text.CurTextFieldStyle.alignment = align;
			GUI.color = color;
			GUI.enabled = true;
			SetSettingsValue(def, field, value);
		}

		public void SliderPercentLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				float value = Convert.ToSingle(GetSettingsValue(def, field));
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
				Color color = GUI.color;
				if (!disabledTooltip.NullOrEmpty())
				{
					GUI.color = UIElements.InactiveColor;
					GUI.enabled = false;
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				float value2 = value;
				if (endValue > 0 && value2 >= max)
				{
					value2 = endValue;
				}
				GUI.enabled = true;
				GUI.color = color;
				SetSettingsValue(def, field, value, value2);
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to float. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
		}

		public void SliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, float increment = 0, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				float value = Convert.ToSingle(GetSettingsValue(def, field));
				Rect rect = GetSplitRect(24f);
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
				Color color = GUI.color;
				if (!disabledTooltip.NullOrEmpty())
				{
					GUI.color = UIElements.InactiveColor;
					GUI.enabled = false;
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				float value2 = value;
				if (increment > 0)
				{
					value = Ext_Math.RoundTo(value, increment);
					value2 = Ext_Math.RoundTo(value2, increment);
				}
				if (endValue > 0 && value2 >= max)
				{
					value2 = endValue;
				}
				GUI.color = color;
				GUI.enabled = true;
				SetSettingsValue(def, field, value, value2);
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to float. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
		}

		public void SliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, int min, int max, 
			int endValue = -1, string maxValueDisplay = "", string minValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				int value = Convert.ToInt32(GetSettingsValue(def, field));
				
				Rect rect = GetSplitRect(24f);
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
				Color color = GUI.color;
				if (!disabledTooltip.NullOrEmpty())
				{
					GUI.color = UIElements.InactiveColor;
					GUI.enabled = false;
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				int value2 = value;
				if (value2 >= max && endValue > 0)
				{
					value2 = endValue;
				}
				SetSettingsValue(def, field, value, value2);
				GUI.color = color;
				GUI.enabled = true;
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to int. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
		}

		public void EnumSliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, Type enumType, bool translate = false)
		{
			Shift();
			try
			{
				int value = Convert.ToInt32(GetSettingsValue(def, field));
				int[] enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
				string[] enumNames = Enum.GetNames(enumType);
				int min = enumValues[0];
				int max = enumValues.Last();
				Rect rect = GetSplitRect(24f);
				string format = Enum.GetName(enumType, value);
				if (translate)
				{
					format = format.Translate();
				}
				Color color = GUI.color;
				if (!disabledTooltip.NullOrEmpty())
				{
					GUI.color = UIElements.InactiveColor;
					GUI.enabled = false;
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				SetSettingsValue(def, field, value);
				GUI.color = color;
				GUI.enabled = true;
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to int. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
		}
	}
}
