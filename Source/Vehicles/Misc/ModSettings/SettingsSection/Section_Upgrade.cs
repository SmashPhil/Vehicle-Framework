using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Section_Upgrade : SettingsSection
	{
		public Dictionary<string, Dictionary<SaveableField, SavedField<object>>> upgradeSettings = new Dictionary<string, Dictionary<SaveableField, SavedField<object>>>();

		public override IEnumerable<FloatMenuOption> ResetOptions
		{
			get
			{
				if (VehicleMod.selectedDef != null)
				{
					yield return new FloatMenuOption("DevModeResetVehicle".Translate(VehicleMod.selectedDef.LabelCap), delegate ()
					{
						SettingsCustomizableFields.PopulateSaveableUpgrades(VehicleMod.selectedDef, true);
					});
				}
				yield return new FloatMenuOption("DevModeResetAllVehicles".Translate(), () => ResetSettings());

				yield return new FloatMenuOption("DevModeResetAll".Translate(), delegate ()
				{
					VehicleMod.ResetAllSettings();
				});
			}
		}

		public override void Initialize()
		{
			upgradeSettings ??= new Dictionary<string, Dictionary<SaveableField, SavedField<object>>>();
		}

		public override void ResetSettings()
		{
			base.ResetSettings();
			upgradeSettings.Clear();
			if (VehicleMod.ModifiableSettings)
			{
				foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefs)
				{
					SettingsCustomizableFields.PopulateSaveableUpgrades(def, true);
				}
			}
		}

		public override void ExposeData()
		{
			Scribe_NestedCollections.Look(ref upgradeSettings, "upgradeSettings", LookMode.Value, LookMode.Deep, LookMode.Undefined, true);
		}

		public override void DrawSection(Rect rect)
		{
			DrawVehicleUpgrades(rect);
			VehicleMod.DrawVehicleList(rect, (bool valid) => valid ? string.Empty : "VehicleNonUpgradeableSettingsTooltip".Translate().ToString(),
						(VehicleDef def) => !VehicleMod.settingsDisabledFor.Contains(def.defName) && def.HasComp(typeof(CompUpgradeTree)));

		}

		private void DrawVehicleUpgrades(Rect menuRect)
		{
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
			vehicleDetailsContainer.y += 5;

			if (VehicleMod.selectedDef != null && VehicleMod.selectedDefUpgradeComp != null)
			{
				try
				{
					foreach (UpgradeNode upgradeNode in VehicleMod.selectedDefUpgradeComp.upgrades)
					{
						if (!upgradeNode.prerequisiteNodes.NullOrEmpty())
						{
							foreach (UpgradeNode prerequisite in VehicleMod.selectedDefUpgradeComp.upgrades.FindAll(x => upgradeNode.prerequisiteNodes.Contains(x.upgradeID)))
							{
								Vector2 start = new Vector2(vehicleDetailsContainer.x + ITab_Vehicle_Upgrades.GridOrigin.x + (ITab_Vehicle_Upgrades.GridSpacing.x * prerequisite.GridCoordinate.x),
									vehicleDetailsContainer.y + ITab_Vehicle_Upgrades.GridOrigin.y + (ITab_Vehicle_Upgrades.GridSpacing.y * prerequisite.GridCoordinate.z) + (ITab_Vehicle_Upgrades.TopPadding * 2));
								Vector2 end = new Vector2(vehicleDetailsContainer.x + ITab_Vehicle_Upgrades.GridOrigin.x + (ITab_Vehicle_Upgrades.GridSpacing.x * upgradeNode.GridCoordinate.x),
									vehicleDetailsContainer.y + ITab_Vehicle_Upgrades.GridOrigin.y + (ITab_Vehicle_Upgrades.GridSpacing.y * upgradeNode.GridCoordinate.z) + (ITab_Vehicle_Upgrades.TopPadding * 2));
								Color color = Color.grey;
								Widgets.DrawLine(start, end, color, 2f);
							}
						}
					}

					for (int i = 0; i < VehicleMod.selectedDefUpgradeComp.upgrades.Count; i++)
					{
						UpgradeNode upgradeNode = VehicleMod.selectedDefUpgradeComp.upgrades[i];
						float imageWidth = ITab_Vehicle_Upgrades.TotalIconSizeScalar / upgradeNode.UpgradeImage.width;
						float imageHeight = ITab_Vehicle_Upgrades.TotalIconSizeScalar / upgradeNode.UpgradeImage.height;

						Rect upgradeRect = new Rect(vehicleDetailsContainer.x + ITab_Vehicle_Upgrades.GridOrigin.x + (ITab_Vehicle_Upgrades.GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth / 2),
							vehicleDetailsContainer.y + ITab_Vehicle_Upgrades.GridOrigin.y + (ITab_Vehicle_Upgrades.GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight / 2) + (ITab_Vehicle_Upgrades.TopPadding * 2),
							imageWidth, imageHeight);
						Widgets.DrawTextureFitted(upgradeRect, upgradeNode.UpgradeImage, 1);

						if (upgradeNode.displayLabel)
						{
							float textWidth = Text.CalcSize(upgradeNode.label).x;
							Rect nodeLabelRect = new Rect(upgradeRect.x - (textWidth - upgradeRect.width) / 2, upgradeRect.y - 20f, 10f * upgradeNode.label.Length, 25f);
							Widgets.Label(nodeLabelRect, upgradeNode.label);
						}
						Rect buttonRect = new Rect(vehicleDetailsContainer.x + ITab_Vehicle_Upgrades.GridOrigin.x + (ITab_Vehicle_Upgrades.GridSpacing.x * upgradeNode.GridCoordinate.x) - (imageWidth / 2),
							vehicleDetailsContainer.y + ITab_Vehicle_Upgrades.GridOrigin.y + (ITab_Vehicle_Upgrades.GridSpacing.y * upgradeNode.GridCoordinate.z) - (imageHeight / 2) + (ITab_Vehicle_Upgrades.TopPadding * 2),
							imageWidth, imageHeight);

						if (Mouse.IsOver(upgradeRect) || VehicleMod.selectedNode == upgradeNode)
						{
							GUI.DrawTexture(upgradeRect, TexUI.HighlightTex);
						}
						if (Mouse.IsOver(upgradeRect))
						{
							TooltipHandler.TipRegion(upgradeRect, upgradeNode.label);
						}

						if (Widgets.ButtonInvisible(buttonRect, true))
						{
							if (VehicleMod.selectedNode != upgradeNode)
							{
								VehicleMod.selectedNode = upgradeNode;
								Find.WindowStack.Add(new Dialog_NodeSettings(VehicleMod.selectedDef, VehicleMod.selectedNode, new Vector2(buttonRect.x + imageWidth * 2, buttonRect.y + imageHeight / 2)));
								SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
							}
							else
							{
								VehicleMod.selectedNode = null;
								SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Exception thrown while trying to select {VehicleMod.selectedDef.defName}. Disabling vehicle to preserve mod settings.\nException={ex.Message}");
					VehicleMod.settingsDisabledFor.Add(VehicleMod.selectedDef.defName);
					VehicleMod.selectedDef = null;
					VehicleMod.selectedPatterns.Clear();
					VehicleMod.selectedDefUpgradeComp = null;
					VehicleMod.selectedNode = null;
					VehicleMod.SetVehicleTex(null);
				}
			}
		}
	}
}
