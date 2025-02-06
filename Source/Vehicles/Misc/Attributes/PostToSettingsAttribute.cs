using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
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
		public VehicleType VehicleType { get; set; } = VehicleType.Universal;
		/// <summary>
		/// Class type field that contains saveable fields within
		/// </summary>
		public bool ParentHolder { get; set; }

		public string ResolvedLabel()
		{
			return Translate ? Label.Translate().ToString() : Label;
		}

		public string ResolvedTooltip()
		{
			return Translate ? Tooltip.Translate().ToString() : Tooltip;
		}

		/// <summary>
		/// Draws UI element for lister in ModSettings
		/// </summary>
		/// <param name="lister"></param>
		/// <param name="def"></param>
		/// <param name="field"></param>
		public void DrawLister(Listing_Settings lister, VehicleDef vehicleDef, FieldInfo field)
		{
			string label = ResolvedLabel();
			string tooltip = ResolvedTooltip();
			string disabledTooltip = string.Empty;

			if (field.TryGetAttribute(out DisableSettingConditionalAttribute disableSetting))
			{
				if (!disableSetting.MayRequire.NullOrEmpty() && !ModsConfig.IsActive(disableSetting.MayRequire))
				{
					disabledTooltip = "VF_DisabledSingleModDependencyTooltip".Translate(disableSetting.MayRequire);
				}
				else if (!disableSetting.MayRequireAny.NullOrEmpty() && !disableSetting.MayRequireAny.Any(packageId => ModsConfig.IsActive(packageId)))
				{
					disabledTooltip = "VF_DisabledSingleModDependencyTooltip".Translate(Environment.NewLine + string.Join(Environment.NewLine, disableSetting.MayRequireAny));
				}
				else if (!disableSetting.MayRequireAll.NullOrEmpty() && !disableSetting.MayRequireAll.All(packageId => ModsConfig.IsActive(packageId)))
				{
					disabledTooltip = "VF_DisabledMultipleModsDependencyTooltip".Translate(Environment.NewLine + string.Join(Environment.NewLine, disableSetting.MayRequireAll));
				}
				else if (disableSetting.FieldDisabled(vehicleDef, out string fieldDisabledTooltip))
				{
					disabledTooltip = "VF_SaveableFieldDisabledConditionTooltip".Translate(fieldDisabledTooltip);
				}
				else if (disableSetting.PropertyDisabled(vehicleDef, out string propertyDisabledTooltip))
				{
					disabledTooltip = "VF_SaveableFieldDisabledConditionTooltip".Translate(propertyDisabledTooltip);
				}
			}

			if (VehicleType != VehicleType.Universal && VehicleType != vehicleDef.vehicleType)
			{
				disabledTooltip = "VF_SaveableFieldDisabledTooltip".Translate();
			}
			bool locked = false;
			if (ParsingHelper.lockedFields.TryGetValue(vehicleDef.defName, out HashSet<FieldInfo> lockedFields))
			{
				if (lockedFields.Contains(field))
				{
					locked = true;
					disabledTooltip = "VF_SaveableFieldLockedTooltip".Translate();
				}
			}
			if (field.HasAttribute<DisableSettingAttribute>())
			{
				disabledTooltip = "VF_DebugDisabledTooltip".Translate();
			}
			if (field.FieldType.GetInterface(nameof(ICustomSettingsDrawer)) is ICustomSettingsDrawer settingsDrawer)
			{
				settingsDrawer.DrawSetting(lister, vehicleDef, field, label, tooltip, disabledTooltip, locked, Translate);
			}
			else
			{
				DrawSetting(lister, vehicleDef, field, UISettingsType, label, tooltip, disabledTooltip, locked, Translate);
			}
		}

		public static void DrawSetting(Listing_Settings lister, VehicleDef vehicleDef, FieldInfo field, UISettingsType settingsType, string label, string tooltip, string disabledTooltip, bool locked, bool translate)
		{
			using var textBlock = new TextBlock(GameFont.Small);
			SaveableField saveable = new SaveableField(vehicleDef, field);
			switch (settingsType)
			{
				case UISettingsType.None:
					return;
				case UISettingsType.Checkbox:
					lister.CheckboxLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, locked);
					break;
				case UISettingsType.IntegerBox:
					{
						if (field.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
						{
							lister.IntegerBox(vehicleDef, saveable, label, tooltip, disabledTooltip, Mathf.RoundToInt(inputBox.MinValue), Mathf.RoundToInt(inputBox.MaxValue));
						}
						else
						{
							lister.IntegerBox(vehicleDef, saveable, label, tooltip, disabledTooltip, 0, int.MaxValue);
						}
						break;
					}
				case UISettingsType.FloatBox:
					{
						if (field.TryGetAttribute<NumericBoxValuesAttribute>(out var inputBox))
						{
							lister.FloatBox(vehicleDef, saveable, label, tooltip, disabledTooltip, inputBox.MinValue, inputBox.MaxValue);
						}
						else
						{
							lister.FloatBox(vehicleDef, saveable, label, tooltip, disabledTooltip, 0, float.MaxValue);
						}
						break;
					}
				case UISettingsType.ToggleLabel:
					break;
				case UISettingsType.SliderEnum:
					lister.EnumSliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, field.FieldType, translate);
					break;
				case UISettingsType.SliderInt:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, (int)slider.MinValue, (int)slider.MaxValue, (int)slider.EndValue, slider.MaxValueDisplay, slider.MinValueDisplay, translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, string.Empty, 0, 100, -1, string.Empty, string.Empty, translate);
						}
					}
					break;
				case UISettingsType.SliderFloat:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, slider.MinValue, slider.MaxValue, slider.RoundDecimalPlaces, slider.EndValue, slider.Increment, slider.MaxValueDisplay, translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, string.Empty, 0f, 100f, 0, -1, -1, string.Empty, translate);
						}
					}
					break;
				case UISettingsType.SliderPercent:
					{
						if (field.TryGetAttribute<SliderValuesAttribute>(out var slider))
						{
							lister.SliderPercentLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, slider.EndSymbol, slider.MinValue, slider.MaxValue, slider.RoundDecimalPlaces, slider.EndValue, slider.MaxValueDisplay, translate);
						}
						else
						{
							SmashLog.WarningOnce($"Slider declared for SaveableField {field.Name} in {field.DeclaringType} with no SliderValues attribute. Slider will use default values instead.", field.GetHashCode());
							lister.SliderPercentLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, string.Empty, 0f, 100f, 0, -1, string.Empty, translate);
						}
					}
					break;
				default:
					Log.ErrorOnce($"{VehicleHarmony.LogLabel} {settingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify mod author.", settingsType.ToString().GetHashCode());
					break;
			}
		}

		public static void DrawSetting(Listing_Settings lister, VehicleDef vehicleDef, FieldInfo field, SettingsValueInfo settingsInfo, string label, string tooltip, string disabledTooltip, bool locked, bool translate)
		{
			SaveableField saveable = new SaveableField(vehicleDef, field);
			switch (settingsInfo.settingsType)
			{
				case UISettingsType.None:
					return;
				case UISettingsType.Checkbox:
					lister.CheckboxLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, locked);
					break;
				case UISettingsType.IntegerBox:
					{
						lister.IntegerBox(vehicleDef, saveable, label, tooltip, disabledTooltip, Mathf.RoundToInt(settingsInfo.minValue), Mathf.RoundToInt(settingsInfo.maxValue));
						break;
					}
				case UISettingsType.FloatBox:
					{
						lister.FloatBox(vehicleDef, saveable, label, tooltip, disabledTooltip, settingsInfo.minValue, settingsInfo.maxValue);
						break;
					}
				case UISettingsType.SliderEnum:
					lister.EnumSliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, field.FieldType, translate);
					break;
				case UISettingsType.SliderInt:
					{
						lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, settingsInfo.endSymbol, (int)settingsInfo.minValue, (int)settingsInfo.maxValue, (int)settingsInfo.endValue, settingsInfo.maxValueDisplay, settingsInfo.minValueDisplay, translate);
					}
					break;
				case UISettingsType.SliderFloat:
					{
						lister.SliderLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, settingsInfo.endSymbol, settingsInfo.minValue, settingsInfo.maxValue, settingsInfo.roundDecimalPlaces, settingsInfo.endValue, settingsInfo.increment, settingsInfo.maxValueDisplay, translate);
					}
					break;
				case UISettingsType.SliderPercent:
					{
						lister.SliderPercentLabeled(vehicleDef, saveable, label, tooltip, disabledTooltip, settingsInfo.endSymbol, settingsInfo.minValue, settingsInfo.maxValue, settingsInfo.roundDecimalPlaces, settingsInfo.endValue, settingsInfo.maxValueDisplay, translate);
					}
					break;
				default:
					Log.ErrorOnce($"{VehicleHarmony.LogLabel} {settingsInfo.settingsType} has not yet been implemented for PostToSettings.DrawLister. Please notify mod author.", settingsInfo.settingsType.ToString().GetHashCode());
					break;
			}
		}
	}
}
