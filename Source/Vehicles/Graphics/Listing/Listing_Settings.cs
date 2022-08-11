using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Listing_Settings : Listing_SplitColumns
	{
		public static readonly Color modifiedColor = new Color(0.4f, 0.4f, 1);
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

		public override void Begin(Rect rect)
		{
			base.Begin(rect);
			GUIUtility.PushGUIState();
		}

		public override void End()
		{
			base.End();
			GUIUtility.Close();
		}

		public object GetSettingsValue(VehicleDef def, SaveableField field)
		{
			try
			{
				switch (settings)
				{
					case SettingsPage.Vehicles:
						{
							if (VehicleMod.settings.vehicles.fieldSettings[def.defName].TryGetValue(field, out var value))
							{
								return value.EndValue;
							}
							return VehicleMod.settings.vehicles.defaultValues[def.defName][field];
						}
					case SettingsPage.Upgrades:
						{
							throw new NotImplementedException();
						}
					default:
						throw new NotSupportedException($"Cannot use Listing_Settings with settings set to {settings}");
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
			ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
		}

		private void SetSettingsValue<T>(VehicleDef def, SaveableField field, T value)
		{
			switch (settings)
			{
				case SettingsPage.Vehicles:
					VehicleMod.settings.vehicles.fieldSettings[def.defName][field] = new SavedField<object>(value);
					break;
				case SettingsPage.Upgrades:
					VehicleMod.settings.upgrades.upgradeSettings[def.defName][field] = new SavedField<object>(value);
					break;
				default:
					throw new NotSupportedException($"Cannot use Listing_SplitColumns with settings set to {settings}");
			}
			ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
		}

		private bool FieldModified(VehicleDef def, SaveableField field)
		{
			return VehicleMod.settings.vehicles.fieldSettings[def.defName].ContainsKey(field);
		}

		public void CheckboxLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, bool locked)
		{
			Shift();
			try
			{
				Rect rect = GetSplitRect(24);
				bool disabled = !disabledTooltip.NullOrEmpty();
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				bool checkState = (bool)GetSettingsValue(def, field);
				if (locked)
				{
					checkState = false;
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				if (UIElements.CheckboxLabeled(rect, label, ref checkState, disabled))
				{
					SetSettingsValue(def, field, checkState);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to convert to bool. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void IntegerBox(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, int min = int.MinValue, int max = int.MaxValue)
		{
			Shift();
			
			try
			{
				int value = Convert.ToInt32(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				Rect rect = GetSplitRect(24);
				float centerY = rect.y + (rect.height - Text.LineHeight) / 2;
				float length = rect.width * 0.45f;
				Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
				Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, Text.LineHeight);

				Color color = GUI.color;
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				Widgets.Label(rectLeft, label);

				Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
				string buffer = value.ToString();
				int valueBefore = value;
				Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to convert to integer. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void FloatBox(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, float min = int.MinValue, float max = int.MaxValue)
		{
			Shift();
			
			try
			{
				float value = Convert.ToSingle(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				Rect rect = GetSplitRect(24);
				float centerY = rect.y + (rect.height - Text.LineHeight) / 2;
				float length = rect.width * 0.45f;
				Rect rectLeft = new Rect(rect.x, centerY, length, rect.height);
				Rect rectRight = new Rect(rect.x + (rect.width - length), centerY, length, Text.LineHeight);

				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				Widgets.Label(rectLeft, label);

				Text.CurTextFieldStyle.alignment = TextAnchor.MiddleRight;
				string buffer = value.ToString();
				float valueBefore = value;
				Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value);
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to convert to float. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void SliderPercentLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				float value = Convert.ToSingle(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				Rect rect = GetSplitRect(24f);
				rect.y += rect.height / 2;
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
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				float valueBefore = value;
				value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				float value2 = value;
				if (endValue > 0 && value2 >= max)
				{
					value2 = endValue;
				}
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value, value2);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to float. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void SliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, float min, float max, int decimalPlaces = 2, 
			float endValue = -1f, float increment = 0, string endValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				float value = Convert.ToSingle(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				Rect rect = GetSplitRect(24f);
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
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				float valueBefore = value;
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
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value, value2);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to float. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void SliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, string endSymbol, int min, int max, 
			int endValue = -1, string maxValueDisplay = "", string minValueDisplay = "", bool translate = false)
		{
			Shift();
			try
			{
				int value = Convert.ToInt32(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				Rect rect = GetSplitRect(24f);
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
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				int valueBefore = value;
				value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				int value2 = value;
				if (value2 >= max && endValue > 0)
				{
					value2 = endValue;
				}
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value, value2);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to int. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}

		public void EnumSliderLabeled(VehicleDef def, SaveableField field, string label, string tooltip, string disabledTooltip, Type enumType, bool translate = false)
		{
			Shift();
			try
			{
				int value = Convert.ToInt32(GetSettingsValue(def, field));
				bool disabled = !disabledTooltip.NullOrEmpty();
				int[] enumValues = Enum.GetValues(enumType).Cast<int>().ToArray();
				string[] enumNames = Enum.GetNames(enumType);
				int min = enumValues[0];
				int max = enumValues.Last();
				Rect rect = GetSplitRect(24f);
				rect.y += rect.height / 2;
				string format = Enum.GetName(enumType, value);
				if (translate)
				{
					format = format.Translate();
				}
				Color color = GUI.color;
				bool mouseOver = Mouse.IsOver(rect);
				if (disabled)
				{
					GUIUtility.DisableGUI();
					UIElements.DoTooltipRegion(rect, disabledTooltip);
				}
				else if (!tooltip.NullOrEmpty())
				{
					if (mouseOver)
					{
						Widgets.DrawHighlight(rect);
					}
					UIElements.DoTooltipRegion(rect, tooltip, true);
				}
				if (!disabled && mouseOver)
				{
					if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
					{
						Event.current.Use();
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("ResetButton".Translate(), delegate ()
						{
							ActionOnSettingsInputAttribute.InvokeIfApplicable(field.FieldInfo);
							VehicleMod.settings.vehicles.fieldSettings[def.defName].Remove(field);
						}));
						FloatMenu floatMenu = new FloatMenu(options)
						{
							vanishIfMouseDistant = true
						};
						Find.WindowStack.Add(floatMenu);
					}
				}
				if (FieldModified(def, field))
				{
					label = label.Colorize(modifiedColor);
				}
				int valueBefore = value;
				value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
				if (valueBefore != value)
				{
					SetSettingsValue(def, field, value);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Unable to convert to int. Def=\"{def.defName}\" Field=\"{field.name}\" Exception={ex.Message}");
				return;
			}
			finally
			{
				GUIUtility.ResetGUIState();
				GUIUtility.EnableGUI();
			}
		}
	}
}
