using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Section_Vehicles : SettingsSection
	{
		private const float SmallIconSize = 24;

		private static string drawStatusMessage = string.Empty;
		public Dictionary<string, Dictionary<SaveableField, SavedField<object>>> fieldSettings = new Dictionary<string, Dictionary<SaveableField, SavedField<object>>>();
		public Dictionary<string, Dictionary<SaveableField, object>> defaultValues = new Dictionary<string, Dictionary<SaveableField, object>>();

		/// <summary>
		/// <defName, maskName>
		/// </summary>
		public Dictionary<string, PatternData> defaultGraphics = new Dictionary<string, PatternData>();

		private Dictionary<VehicleDef, Rot8> directionFacing = new Dictionary<VehicleDef, Rot8>();
		private Rot8 currentVehicleFacing;

		public override IEnumerable<FloatMenuOption> ResetOptions
		{
			get
			{
				if (VehicleMod.selectedDef != null)
				{
					yield return new FloatMenuOption("VF_DevMode_ResetVehicle".Translate(VehicleMod.selectedDef.LabelCap), delegate ()
					{
						SettingsCustomizableFields.PopulateSaveableFields(VehicleMod.selectedDef, true);
					});
				}
				yield return new FloatMenuOption("VF_DevMode_ResetAllVehicles".Translate(), () => ResetSettings());

				yield return new FloatMenuOption("VF_DevMode_ResetAll".Translate(), delegate ()
				{
					VehicleMod.ResetAllSettings();
				});
			}
		}

		public override void Initialize()
		{
			fieldSettings ??= new Dictionary<string, Dictionary<SaveableField, SavedField<object>>>();
			defaultGraphics ??= new Dictionary<string, PatternData>();
		}

		public override void ResetSettings()
		{
			base.ResetSettings();
			VehicleMod.cachedFields.Clear();
			VehicleMod.PopulateCachedFields();
			fieldSettings.Clear();
			defaultGraphics.Clear();
			if (VehicleMod.ModifiableSettings)
			{
				foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefs)
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
		}

		public override void ExposeData()
		{
			Scribe_NestedCollections.Look(ref fieldSettings, "fieldSettings", LookMode.Value, LookMode.Deep, LookMode.Undefined);
			Scribe_Collections.Look(ref defaultGraphics, "defaultGraphics", LookMode.Value, LookMode.Deep);
		}

		public override void DrawSection(Rect rect)
		{
			DrawVehicleOptions(rect);
			VehicleMod.DrawVehicleList(rect, (bool valid) => valid ? string.Empty : "VehicleSettingsDisabledTooltip".Translate().ToString(), 
				(VehicleDef def) => !VehicleMod.settingsDisabledFor.Contains(def.defName));
		}

		public override void VehicleSelected()
		{
			currentVehicleFacing = VehicleMod.selectedDef.drawProperties.displayRotation;
		}

		private void DrawVehicleOptions(Rect menuRect)
		{
			listingSplit = new Listing_Settings()
			{
				maxOneColumn = true,
				shiftRectScrollbar = true
			};

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

			listingStandard = new Listing_Standard();
			listingStandard.Begin(vehicleDetailsContainer.ContractedBy(1));
			listingStandard.Header($"{VehicleMod.selectedDef?.LabelCap ?? string.Empty}", ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
			listingStandard.End();

			if (VehicleMod.selectedDef != null)
			{
				try
				{
					drawStatusMessage = $"Drawing {VehicleMod.selectedDef}";
					Rect iconRect = menuRect.ContractedBy(10);
					iconRect.width /= 5;
					iconRect.height = iconRect.width;
					iconRect.x += menuRect.width / 4;
					iconRect.y += 35;

					drawStatusMessage = $"Creating Paintbrush. Pattern={VehicleMod.selectedPatterns.Count}";
					if (VehicleMod.selectedPatterns.Count > 1 && VehicleMod.selectedDef.graphicData.shaderType.Shader.SupportsRGBMaskTex())
					{
						Rect paintBrushRect = new Rect(iconRect.x + iconRect.width, iconRect.y, SmallIconSize, SmallIconSize);
						Widgets.DrawHighlightIfMouseover(paintBrushRect);
						Widgets.DrawTextureFitted(paintBrushRect, VehicleTex.Recolor, 1);
						TooltipHandler.TipRegionByKey(paintBrushRect, "VehiclesRecolorDefaultMaskTooltip");
						if (Widgets.ButtonInvisible(paintBrushRect))
						{
							SoundDefOf.Click.PlayOneShotOnCamera();
							Dialog_ColorPicker.OpenColorPicker(VehicleMod.selectedDef, 
							delegate (Color colorOne, Color colorTwo, Color colorThree, PatternDef pattern, Vector2 displacement, float tiles)
							{
								defaultGraphics[VehicleMod.selectedDef.defName] = new PatternData(colorOne, colorTwo, colorThree, pattern, displacement, tiles);
							});
						}
					}

					drawStatusMessage = $"Creating RotationHandle. Pattern={VehicleMod.selectedPatterns.Count}";
					if (VehicleMod.selectedDef.graphicData.drawRotated && VehicleMod.selectedDef.graphicData.Graphic is Graphic_Vehicle graphicVehicle)
					{
						Rect rotateVehicleRect = new Rect(iconRect.x + iconRect.width, iconRect.y + SmallIconSize, SmallIconSize, SmallIconSize);
						Widgets.DrawHighlightIfMouseover(rotateVehicleRect);
						Widgets.DrawTextureFitted(rotateVehicleRect, VehicleTex.ReverseIcon, 1);
						if (Widgets.ButtonInvisible(rotateVehicleRect))
						{
							SoundDefOf.Click.PlayOneShotOnCamera();
							List<Rot8> validRotations = graphicVehicle.RotationsRenderableByUI.ToList();
							for (int i = 0; i < 4; i++)
							{
								currentVehicleFacing = currentVehicleFacing.Rotated(RotationDirection.Clockwise, false);
								if (validRotations.Contains(currentVehicleFacing)) { break; }
							}
						}
					}

					drawStatusMessage = $"Fetching PatternData from defaultMasks";
					PatternData patternData = defaultGraphics.TryGetValue(VehicleMod.selectedDef.defName, VehicleMod.selectedDef.graphicData);

					drawStatusMessage = $"Drawing VehicleTex in settings";
					Widgets.BeginGroup(iconRect);
					Rect vehicleTexRect = new Rect(Vector2.zero, iconRect.size);
					drawStatusMessage = RenderHelper.DrawVehicleDef(vehicleTexRect, VehicleMod.selectedDef, null, patternData, directionFacing.TryGetValue(VehicleMod.selectedDef, currentVehicleFacing));
					if (!drawStatusMessage.NullOrEmpty())
					{
						throw new Exception();
					}
					Widgets.EndGroup();

					drawStatusMessage = $"Drawing enable button";
					Rect enableButtonRect = menuRect.ContractedBy(10);
					enableButtonRect.x += enableButtonRect.width / 4 + 5;
					EnableButton(enableButtonRect);

					Rect compVehicleRect = menuRect.ContractedBy(10);
					compVehicleRect.x += vehicleIconContainer.width * 2 - 10;
					compVehicleRect.y = iconRect.y;
					compVehicleRect.width -= vehicleIconContainer.width * 2;
					compVehicleRect.height = iconRect.height;
					
					listingSplit.Begin(compVehicleRect, 2);
					drawStatusMessage = $"Drawing main settings.";

					foreach (FieldInfo field in VehicleMod.vehicleDefFields)
					{
						if (field.TryGetAttribute(out PostToSettingsAttribute post))
						{
							post.DrawLister(listingSplit, VehicleMod.selectedDef, field);
						}
					}

					listingSplit.End();

					float scrollableFieldY = menuRect.height * 0.4f;
					Rect scrollableFieldsRect = new Rect(vehicleDetailsContainer.x + 1, menuRect.y + scrollableFieldY, vehicleDetailsContainer.width - 2, menuRect.height - scrollableFieldY - 10);

					Rect scrollableFieldsViewRect = new Rect(scrollableFieldsRect.x, scrollableFieldsRect.y, scrollableFieldsRect.width - 20, VehicleMod.scrollableViewHeight);
					//UIElements.DrawLineVerticalGrey(iconRect.x + iconRect.width + 24, iconRect.y, VehicleMod.scrollableViewHeight - 10);
					UIElements.DrawLineHorizontalGrey(scrollableFieldsRect.x, scrollableFieldsRect.y - 1, scrollableFieldsRect.width);

					drawStatusMessage = $"Drawing sub settings";
					listingSplit.BeginScrollView(scrollableFieldsRect, ref VehicleMod.saveableFieldsScrollPosition, ref scrollableFieldsViewRect, 3);
					foreach ((Type type, List<FieldInfo> fields) in VehicleMod.VehicleCompFields)
					{
						if (fields.NullOrEmpty() || fields.All(f => f.TryGetAttribute<PostToSettingsAttribute>(out var settings) 
						&& settings.VehicleType != VehicleType.Universal && settings.VehicleType != VehicleMod.selectedDef.vehicleType))
						{
							continue;
						}
						string header = string.Empty;
						if (type.TryGetAttribute(out HeaderTitleAttribute title))
						{
							header = title.Translate ? title.Label.Translate().ToString() : title.Label;
						}
						listingSplit.Header(header, ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter, 24);
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
				catch (Exception ex)
				{
					Log.Error($"Exception thrown while trying to select {VehicleMod.selectedDef.defName}. LastTask={drawStatusMessage} Disabling vehicle to preserve mod settings.\nException={ex.Message}");
					VehicleMod.settingsDisabledFor.Add(VehicleMod.selectedDef.defName);
					VehicleMod.selectedDef = null;
					VehicleMod.selectedPatterns.Clear();
					VehicleMod.selectedDefUpgradeComp = null;
					VehicleMod.selectedNode = null;
					VehicleMod.SetVehicleTex(null);
				}
			}
		}

		public void EnableButton(Rect rect)
		{
			if (VehicleMod.selectedDef is null)
			{
				Log.Error($"SelectedDef is null while trying to create Enable button for VehicleDef.");
				return;
			}
			var gameFont = Text.Font;
			Text.Font = GameFont.Medium;
			FieldInfo enabledField = AccessTools.Field(typeof(VehicleDef), nameof(VehicleDef.enabled));
			SaveableField saveableField = new SaveableField(VehicleMod.selectedDef, enabledField);
			VehicleEnabledFor enabledFor = VehicleEnabledFor.Everyone;
			if (fieldSettings[VehicleMod.selectedDef.defName].TryGetValue(saveableField, out var modifiedValue))
			{
				enabledFor = (VehicleEnabledFor)modifiedValue.EndValue;
			}
			(string text, Color color) = EnabledStatus(enabledFor);
			Vector2 size = Text.CalcSize(text);
			Rect enabledButtonRect = new Rect(rect.x, rect.y, size.x, size.y);
			TooltipHandler.TipRegion(enabledButtonRect, "VehicleEnableButtonTooltip".Translate());

			Color highlightedColor = new Color(color.r + 0.25f, color.g + 0.25f, color.b + 0.25f);
			if (UIElements.ClickableLabel(enabledButtonRect, text, highlightedColor, color, GameFont.Medium, TextAnchor.MiddleLeft, new Color(color.r - 0.15f, color.g - 0.15f, color.b - 0.15f)))
			{
				List<VehicleEnabledFor> enabledForValues = Enum.GetValues(typeof(VehicleEnabledFor)).Cast<VehicleEnabledFor>().ToList();
				enabledFor = enabledForValues.Next(enabledFor);
				fieldSettings[VehicleMod.selectedDef.defName][saveableField] = new SavedField<object>(enabledFor);
				if (enabledFor == (VehicleEnabledFor)enabledField.GetValue(VehicleMod.selectedDef))
				{
					fieldSettings[VehicleMod.selectedDef.defName].Remove(saveableField);
				}
				GizmoHelper.DesignatorsChanged(DesignationCategoryDefOf.Structure);
			}
			Text.Font = gameFont;
		}

		private (string text, Color color) EnabledStatus(VehicleEnabledFor status)
		{
			return status switch
			{
				VehicleEnabledFor.Everyone => ("VehicleEnabled".Translate(), Color.green),
				VehicleEnabledFor.None => ("VehicleDisabled".Translate(), Color.red),
				VehicleEnabledFor.Player => ("VehiclePlayerOnly".Translate(), new Color(0.1f, 0.85f, 0.85f)),
				VehicleEnabledFor.Raiders => ("VehicleRaiderOnly".Translate(), new Color(0.9f, 0.53f, 0.1f)),
				_ => ("[Err] Uncaught Status", Color.red)
			};
		}
	}
}
