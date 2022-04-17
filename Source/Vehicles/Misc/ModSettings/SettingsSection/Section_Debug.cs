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
		public bool debugDraftAnyShip;
		public bool debugDisableWaterPathing;

		public bool debugSpawnVehicleBuildingGodMode;

		public bool debugDrawCannonGrid;
		public bool debugDrawNodeGrid;
		public bool debugDrawHitbox;
		public bool debugDrawVehicleTracks;
		public bool debugDrawBumpers;

		public bool debugLogging;
		public bool debugPathCostChanges;
		public bool debugDrawVehiclePathCosts;

		public override Rect ButtonRect(Rect rect)
		{
			return new Rect(rect.x - 5, rect.y + 7.5f, rect.width, rect.height);
		}

		public override void ResetSettings()
		{
			base.ResetSettings();
			debugDraftAnyShip = false;
			debugDisableWaterPathing = false;

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
			Scribe_Values.Look(ref debugDisableWaterPathing, "debugDisableWaterPathing");

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
			Rect devMode = rect.ContractedBy(20);
			var color = GUI.color;
			
			if (!Prefs.DevMode)
			{
				GUI.enabled = false;
				GUI.color = UIElements.InactiveColor;
			}
			listingStandard = new Listing_Standard();
			listingStandard.Begin(devMode);
			listingStandard.Header("DevModeVehicles".Translate(), ListingExtension.BannerColor, GameFont.Medium, TextAnchor.MiddleCenter);

			listingStandard.GapLine(16);

			listingStandard.CheckboxLabeled("DebugLogging".Translate(), ref debugLogging, "DebugLoggingTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugPathCostRecalculationLogging".Translate(), ref debugPathCostChanges, "DebugPathCostRecalculationLoggingTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDraftAnyVehicle".Translate(), ref debugDraftAnyShip, "DebugDraftAnyVehicleTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDisablePathing".Translate(), ref debugDisableWaterPathing, "DebugDisablePathingTooltip".Translate());

			listingStandard.GapLine(16);

			listingStandard.CheckboxLabeled("DebugSpawnVehiclesGodMode".Translate(), ref debugSpawnVehicleBuildingGodMode, "DebugSpawnVehiclesGodModeTooltip".Translate());

			listingStandard.GapLine(16);

			listingStandard.CheckboxLabeled("DebugCannonDrawer".Translate(), ref debugDrawCannonGrid, "DebugCannonDrawerTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawNodeGrid".Translate(), ref debugDrawNodeGrid, "DebugDrawNodeGridTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawHitbox".Translate(), ref debugDrawHitbox, "DebugDrawHitboxTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawVehicleTracks".Translate(), ref debugDrawVehicleTracks, "DebugDrawVehicleTracksTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugWriteVehiclePathingCosts".Translate(), ref debugDrawVehiclePathCosts, "DebugWriteVehiclePathingCostsTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawBumpers".Translate(), ref debugDrawBumpers, "DebugDrawBumpersTooltip".Translate());

			listingStandard.GapLine(16);
			if (listingStandard.ButtonText("ShowRecentNews".Translate()))
			{
				ShowAllUpdates();
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
