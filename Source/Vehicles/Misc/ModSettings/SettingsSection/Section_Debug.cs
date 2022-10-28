using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using UpdateLogTool;

namespace Vehicles
{
	public class Section_Debug : SettingsSection
	{
		public const float ButtonHeight = 30f;
		public const float VerticalGap = 2f;
		public const int ButtonRows = 3;
		public const int DebugSectionColumns = 2;

		public bool debugDraftAnyVehicle;
		public bool debugShootAnyTurret;
		public bool debugSpawnVehicleBuildingGodMode;

		public bool debugDrawCannonGrid;
		public bool debugDrawNodeGrid;
		public bool debugDrawHitbox;
		public bool debugDrawVehicleTracks;
		public bool debugDrawBumpers;
		public bool debugDrawLordMeetingPoint;
		public bool debugLogging;
		public bool debugPathCostChanges;

		public bool debugDrawVehiclePathCosts;

		public override void ResetSettings()
		{
			base.ResetSettings();
			debugDraftAnyVehicle = false;
			debugShootAnyTurret = false;
			debugSpawnVehicleBuildingGodMode = false;

			debugDrawCannonGrid = false;
			debugDrawNodeGrid = false;
			debugDrawHitbox = false;
			debugDrawVehicleTracks = false;
			debugDrawBumpers = false;
			debugDrawLordMeetingPoint = false;

			debugLogging = false;
			debugPathCostChanges = false;

			debugDrawVehiclePathCosts = false;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref debugDraftAnyVehicle, nameof(debugDraftAnyVehicle));
			Scribe_Values.Look(ref debugShootAnyTurret, nameof(debugShootAnyTurret));
			Scribe_Values.Look(ref debugSpawnVehicleBuildingGodMode, nameof(debugSpawnVehicleBuildingGodMode));

			Scribe_Values.Look(ref debugDrawCannonGrid, nameof(debugDrawCannonGrid));
			Scribe_Values.Look(ref debugDrawNodeGrid, nameof(debugDrawNodeGrid));
			Scribe_Values.Look(ref debugDrawHitbox, nameof(debugDrawHitbox));
			Scribe_Values.Look(ref debugDrawVehicleTracks, nameof(debugDrawVehicleTracks));
			Scribe_Values.Look(ref debugDrawBumpers, nameof(debugDrawBumpers));
			Scribe_Values.Look(ref debugDrawLordMeetingPoint, nameof(debugDrawLordMeetingPoint));

			Scribe_Values.Look(ref debugLogging, nameof(debugLogging));
			Scribe_Values.Look(ref debugPathCostChanges, nameof(debugPathCostChanges));
			Scribe_Values.Look(ref debugDrawVehiclePathCosts, nameof(debugDrawVehiclePathCosts));
		}

		public override void DrawSection(Rect rect)
		{
			Rect devModeRect = rect.ContractedBy(10);
			float buttonRowHeight = (ButtonHeight * ButtonRows + VerticalGap * (ButtonRows - 1));
			devModeRect.height = devModeRect.height - buttonRowHeight;
			
			listingStandard = new Listing_Standard();
			listingStandard.ColumnWidth = (devModeRect.width / DebugSectionColumns) - 4 * DebugSectionColumns;
			listingStandard.Begin(devModeRect);
			{
				listingStandard.Header("VF_DevMode_Logging".Translate(), ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugLogging".Translate(), ref debugLogging, "VF_DevMode_DebugLoggingTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugPathCostRecalculationLogging".Translate(), ref debugPathCostChanges, "VF_DevMode_DebugPathCostRecalculationLoggingTooltip".Translate());

				listingStandard.Header("VF_DevMode_Troubleshooting".Translate(), ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDraftAnyVehicle".Translate(), ref debugDraftAnyVehicle, "VF_DevMode_DebugDraftAnyVehicleTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugShootAnyTurret".Translate(), ref debugShootAnyTurret, "VF_DevMode_DebugShootAnyTurretTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugSpawnVehiclesGodMode".Translate(), ref debugSpawnVehicleBuildingGodMode, "VF_DevMode_DebugSpawnVehiclesGodModeTooltip".Translate());

				listingStandard.Header("VF_DevMode_Drawers".Translate(), ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawUpgradeNodeGrid".Translate(), ref debugDrawNodeGrid, "VF_DevMode_DebugDrawUpgradeNodeGridTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawHitbox".Translate(), ref debugDrawHitbox, "VF_DevMode_DebugDrawHitboxTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawVehicleTracks".Translate(), ref debugDrawVehicleTracks, "VF_DevMode_DebugDrawVehicleTracksTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawBumpers".Translate(), ref debugDrawBumpers, "VF_DevMode_DebugDrawBumpersTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawLordMeetingPoint".Translate(), ref debugDrawLordMeetingPoint, "VF_DevMode_DebugDrawLordMeetingPointTooltip".Translate());

				listingStandard.NewColumn();

				listingStandard.Header("VF_DevMode_Pathing".Translate(), ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawVehiclePathingCosts".Translate(), ref debugDrawVehiclePathCosts, "VF_DevMode_DebugDrawVehiclePathingCostsTooltip".Translate());
				if (listingStandard.ButtonText("VF_DevMode_DebugPathfinderDebugging".Translate(), "VF_DevMode_DebugPathfinderDebuggingTooltip".Translate()))
				{
					RegionDebugMenu();
				}
			}
			listingStandard.End();

			Rect devModeButtonsRect = new Rect(devModeRect);
			devModeButtonsRect.y = devModeRect.yMax;
			devModeButtonsRect.height = buttonRowHeight;

			listingStandard.ColumnWidth = devModeRect.width / 3;
			listingStandard.Begin(devModeButtonsRect);
			{
				if (listingStandard.ButtonText("VF_DevMode_ShowRecentNews".Translate()))
				{
					ShowAllUpdates();
				}
				if (listingStandard.ButtonText("VF_DevMode_OpenQuickTestSettings".Translate()))
				{
					UnitTesting.OpenMenu();
				}
				if (listingStandard.ButtonText("VF_DevMode_GraphEditor".Translate()))
				{
					Find.WindowStack.Add(new Dialog_GraphEditor());
				}
			}
			listingStandard.End();
		}

		public void RegionDebugMenu()
		{
			List<Toggle> vehicleDefToggles = new List<Toggle>();
			vehicleDefToggles.Add(new Toggle("None", () => DebugHelper.drawRegionsFor == null || DebugHelper.debugRegionType == DebugRegionType.None, delegate (bool value)
			{
				if (value)
				{
					DebugHelper.drawRegionsFor = null;
					DebugHelper.debugRegionType = DebugRegionType.None;
				}
			}));
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.defName))
			{
				vehicleDefToggles.Add(new Toggle(vehicleDef.defName, () => DebugHelper.drawRegionsFor == vehicleDef, (value) => { }, onToggle: delegate (bool value)
				{
					if (value)
					{
						List<Toggle> debugOptionToggles = DebugHelper.RegionToggles(vehicleDef).ToList();
						Find.WindowStack.Add(new Dialog_ToggleMenu("VF_DevMode_DebugPathfinderDebugging".Translate(), debugOptionToggles));
					}
					else
					{
						DebugHelper.drawRegionsFor = null;
						DebugHelper.debugRegionType = DebugRegionType.None;
					}
				}));
			}
			Find.WindowStack.Add(new Dialog_RadioButtonMenu("VF_DevMode_DebugPathfinderDebugging".Translate(), vehicleDefToggles));
		}

		public void ShowAllUpdates()
		{
			string versionChecking = "Null";
			VehicleHarmony.updates.Clear();
			foreach (UpdateLog log in FileReader.ReadPreviousFiles(VehicleHarmony.VehicleMCP).OrderByDescending(log => Ext_Settings.CombineVersionString(log.UpdateData.currentVersion)))
			{
				VehicleHarmony.updates.Add(log);
			}
			try
			{
				List<DebugMenuOption> versions = new List<DebugMenuOption>();
				foreach (UpdateLog update in VehicleHarmony.updates)
				{
					versionChecking = update.UpdateData.currentVersion;
					string label = versionChecking;
					if (versionChecking == VehicleHarmony.Version.VersionString)
					{
						label += " (Current)";
					}
					versions.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate ()
					{
						Find.WindowStack.Add(new Dialog_NewUpdate(new HashSet<UpdateLog>() { update }));
					}));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(versions));
			}
			catch (Exception ex)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Unable to show update for {versionChecking} Exception = {ex.Message}");
			}
		}

		public static void DebugDrawRegions()
		{
			List<DebugMenuOption> listCheckbox = new List<DebugMenuOption>();
			listCheckbox.Add(new DebugMenuOption("Clear", DebugMenuOptionMode.Action, delegate ()
			{
				DebugHelper.drawRegionsFor = null;
				DebugHelper.debugRegionType = DebugRegionType.None;
			}));
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs.OrderBy(d => d.defName))
			{
				listCheckbox.Add(new DebugMenuOption(vehicleDef.defName, DebugMenuOptionMode.Action, delegate ()
				{
					List<DebugMenuOption> listCheckbox2 = new List<DebugMenuOption>();

					foreach (string name in Enum.GetNames(typeof(DebugRegionType)))
					{
						listCheckbox2.Add(new DebugMenuOption(name, DebugMenuOptionMode.Action, delegate ()
						{
							DebugHelper.drawRegionsFor = vehicleDef;
							DebugHelper.debugRegionType = (DebugRegionType)Enum.Parse(typeof(DebugRegionType), name);
						}));
					}
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(listCheckbox2));
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(listCheckbox));
		}
	}
}
