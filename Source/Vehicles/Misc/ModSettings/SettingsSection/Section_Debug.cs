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

		public bool debugDraftAnyShip;

		public bool debugSpawnVehicleBuildingGodMode;

		public bool debugDrawCannonGrid;
		public bool debugDrawNodeGrid;
		public bool debugDrawHitbox;
		public bool debugDrawVehicleTracks;
		public bool debugDrawBumpers;

		public bool debugLogging;
		public bool debugPathCostChanges;
		public bool debugDrawVehiclePathCosts;

		public override void ResetSettings()
		{
			base.ResetSettings();
			debugDraftAnyShip = false;
			debugSpawnVehicleBuildingGodMode = false;

			debugDrawCannonGrid = false;
			debugDrawNodeGrid = false;
			debugDrawHitbox = false;
			debugDrawVehicleTracks = false;
			debugDrawVehiclePathCosts = false;
			debugDrawBumpers = false;

			debugLogging = false;
			debugPathCostChanges = false;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref debugDraftAnyShip, "debugDraftAnyShip");
			Scribe_Values.Look(ref debugSpawnVehicleBuildingGodMode, "debugSpawnVehicleBuildingGodMode");

			Scribe_Values.Look(ref debugDrawCannonGrid, "debugDrawCannonGrid");
			Scribe_Values.Look(ref debugDrawNodeGrid, "debugDrawNodeGrid");
			Scribe_Values.Look(ref debugDrawHitbox, "debugDrawHitbox");
			Scribe_Values.Look(ref debugDrawVehicleTracks, "debugDrawVehicleTracks");
			Scribe_Values.Look(ref debugDrawBumpers, "debugDrawBumpers");

			Scribe_Values.Look(ref debugLogging, "debugLogging");
			Scribe_Values.Look(ref debugPathCostChanges, "debugPathCostChanges");
			Scribe_Values.Look(ref debugDrawVehiclePathCosts, "debugDrawVehiclePathCosts");
		}

		public override void DrawSection(Rect rect)
		{
			Rect devModeRect = rect.ContractedBy(20);
			float buttonRowHeight = (ButtonHeight * ButtonRows + VerticalGap * (ButtonRows - 1));
			devModeRect.height = devModeRect.height - buttonRowHeight;
			
			listingStandard = new Listing_Standard();
			listingStandard.ColumnWidth = devModeRect.width / 2;
			listingStandard.Begin(devModeRect);
			{
				listingStandard.Header("VF_DevMode_Logging".Translate(), ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugLogging".Translate(), ref debugLogging, "VF_DevMode_DebugLoggingTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugPathCostRecalculationLogging".Translate(), ref debugPathCostChanges, "VF_DevMode_DebugPathCostRecalculationLoggingTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugWriteVehiclePathingCosts".Translate(), ref debugDrawVehiclePathCosts, "VF_DevMode_DebugWriteVehiclePathingCostsTooltip".Translate());

				listingStandard.Header("VF_DevMode_Troubleshooting".Translate(), ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDraftAnyVehicle".Translate(), ref debugDraftAnyShip, "VF_DevMode_DebugDraftAnyVehicleTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugSpawnVehiclesGodMode".Translate(), ref debugSpawnVehicleBuildingGodMode, "VF_DevMode_DebugSpawnVehiclesGodModeTooltip".Translate());

				listingStandard.Header("VF_DevMode_Drawers".Translate(), ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawUpgradeNodeGrid".Translate(), ref debugDrawNodeGrid, "VF_DevMode_DebugDrawUpgradeNodeGridTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawHitbox".Translate(), ref debugDrawHitbox, "VF_DevMode_DebugDrawHitboxTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawVehicleTracks".Translate(), ref debugDrawVehicleTracks, "VF_DevMode_DebugDrawVehicleTracksTooltip".Translate());
				listingStandard.CheckboxLabeled("VF_DevMode_DebugDrawBumpers".Translate(), ref debugDrawBumpers, "VF_DevMode_DebugDrawBumpersTooltip".Translate());

				//listingStandard.GapLine(16);
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
					Find.WindowStack.Add(new Dialog_UnitTesting());
				}
			}
			listingStandard.End();
		}

		internal void ShowAllUpdates()
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
					if (versionChecking == VehicleHarmony.CurrentVersion)
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
	}
}
