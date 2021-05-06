using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Retrieves and saves input values for fields defined in VehicleDef and VehicleComp objects for access in ModSettings
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class PostToSettingsAttribute : Attribute
	{
		/// <summary>
		/// label displayed in Lister element in ModSettings
		/// </summary>
		public string Label { get; set; }
		/// <summary>
		/// Tooltip over Lister element in ModSettings
		/// </summary>
		public string Tooltip { get; set; }
		/// <summary>
		/// Translate Label and EnumSlider UISettingsType
		/// </summary>
		public bool Translate { get; set; }
		/// <summary>
		/// Settings Draw Type
		/// </summary>
		/// <remarks>
		/// If none specified, field with attribute will not be drawn in ModSettings page.
		/// </remarks>
		public UISettingsType UISettingsType { get; set; }
		/// <summary>
		/// Field is only available to a certain VehicleType
		/// </summary>
		/// <remarks>
		/// If left blank, field will be available to all vehicle types
		/// </remarks>
		public VehicleType VehicleType { get; set; }
		/// <summary>
		/// Class type field that contains saveable fields within
		/// </summary>
		public bool ParentHolder { get; set; }

		/// <summary>
		/// Draws UI element for lister in ModSettings
		/// </summary>
		/// <param name="lister"></param>
		/// <param name="def"></param>
		/// <param name="field"></param>
		public void DrawLister(Listing_Settings lister, VehicleDef def, FieldInfo field)
		{
			string label = Translate ? Label.Translate().ToString() : Label;
			string tooltip = Translate ? Tooltip.Translate().ToString() : Tooltip;
			SaveableField saveable = new SaveableField(def, field);
			string disabledTooltip = string.Empty;
			if (VehicleType != VehicleType.Undefined && VehicleType != def.vehicleType)
			{
				disabledTooltip = "VehicleSaveableFieldDisabledTooltip".Translate();
			}
			bool locked = false;
			if (ParsingHelper.lockedFields.TryGetValue(def.defName, out HashSet<FieldInfo> lockedFields))
			{
				if (lockedFields.Contains(field))
				{
					locked = true;
					disabledTooltip = "VehicleSaveableFieldLockedTooltip".Translate();
				}
			}
			if (field.HasAttribute<DisableSettingAttribute>())
			{
				disabledTooltip = "VehicleDebugDisabledTooltip".Translate();
			}
			
			switch (UISettingsType)
			{
				case UISettingsType.None:
					return;
				case UISettingsType.Checkbox:
					lister.CheckboxLabeled(def, saveable, label, tooltip, disabledTooltip, locked);
					break;
				case UISettingsType.IntegerBox:
					{
						if (field.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
						{
							lister.IntegerBox(def, saveable, label, tooltip, disabledTooltip, Mathf.RoundToInt(inputBox.MinValue), Mathf.RoundToInt(inputBox.MaxValue));
						}
						else
						{
							lister.IntegerBox(def, saveable, label, tooltip, disabledTooltip, 0, int.MaxValue);
						}
						break;
					}
				case UISettingsType.FloatBox:
					{
						if (field.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
						{
							lister.FloatBox(def, saveable, label, tooltip, disabledTooltip, inputBox.MinValue, inputBox.MaxValue);
						}
						else
						{
							lister.FloatBox(def, saveable, label, tooltip, disabledTooltip, 0, float.MaxValue);
						}
						break;
					}
				case UISettingsType.ToggleLabel:
					break;
				case UISettingsType.SliderEnum:
					lister.EnumSliderLabeled(def, saveable, label, tooltip, disabledTooltip, field.FieldType, Translate);
					break;
				case UISettingsType.SliderInt:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderLabeled(def, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, (int)slider.MinValue, (int)slider.MaxValue, (int)slider.EndValue, slider.MaxValueDisplay, slider.MinValueDisplay, Translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderLabeled(def, saveable, label, tooltip, disabledTooltip, string.Empty, 0, 100, -1, string.Empty, string.Empty, Translate);
						}
					}
					break;
				case UISettingsType.SliderFloat:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderLabeled(def, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, slider.MinValue, slider.MaxValue, slider.RoundDecimalPlaces, slider.EndValue, slider.Increment, slider.MaxValueDisplay, Translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderLabeled(def, saveable, label, tooltip, disabledTooltip, string.Empty, 0f, 100f, 0, -1, -1, string.Empty, Translate);
						}
					}
					break;
				case UISettingsType.SliderPercent:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderPercentLabeled(def, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, slider.MinValue, slider.MaxValue, slider.RoundDecimalPlaces, slider.EndValue, slider.MaxValueDisplay, Translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderPercentLabeled(def, saveable, label, tooltip, disabledTooltip, string.Empty, 0f, 100f, 0, -1, string.Empty, Translate);
						}
					}
					break;
				default:
					Log.ErrorOnce($"{VehicleHarmony.LogLabel} {UISettingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify mod author.", UISettingsType.ToString().GetHashCode());
					break;
			}
		}
	}
}
