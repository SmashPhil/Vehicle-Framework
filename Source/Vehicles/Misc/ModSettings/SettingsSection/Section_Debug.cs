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
		public bool debugDrawVehiclePathCosts;
		public bool debugDrawBumpers;

		public bool debugDrawRegions;
		public bool debugDrawRegionLinks;
		public bool debugDrawRegionThings;

		public bool debugLogging;
		public bool debugGenerateWorldPathCostTexts;

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

			debugDrawRegions = false;
			debugDrawRegionLinks = false;
			debugDrawRegionThings = false;

			debugLogging = false;
			debugGenerateWorldPathCostTexts = false;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref debugDraftAnyShip, "debugDraftAnyShip", false);
			Scribe_Values.Look(ref debugDisableWaterPathing, "debugDisableWaterPathing", false);

			Scribe_Values.Look(ref debugSpawnVehicleBuildingGodMode, "debugSpawnVehicleBuildingGodMode", false);

			Scribe_Values.Look(ref debugDrawCannonGrid, "debugDrawCannonGrid", false);
			Scribe_Values.Look(ref debugDrawNodeGrid, "debugDrawNodeGrid", false);
			Scribe_Values.Look(ref debugDrawHitbox, "debugDrawHitbox", false);
			Scribe_Values.Look(ref debugDrawVehicleTracks, "debugDrawVehicleTracks", false);
			Scribe_Values.Look(ref debugDrawVehiclePathCosts, "debugDrawVehiclePathCosts", false);
			Scribe_Values.Look(ref debugDrawBumpers, "debugDrawBumpers", false);

			Scribe_Values.Look(ref debugDrawRegions, "debugDrawRegions", false);
			Scribe_Values.Look(ref debugDrawRegionLinks, "debugDrawRegionLinks", false);
			Scribe_Values.Look(ref debugDrawRegionThings, "debugDrawRegionThings", false);

			Scribe_Values.Look(ref debugLogging, "debugLogging", false);
			Scribe_Values.Look(ref debugGenerateWorldPathCostTexts, "debugGenerateWorldPathCostTexts", false);
		}

		public override void DrawSection(Rect rect)
		{
			float width = rect.width / 1.5f;
			Rect devMode = new Rect((rect.width - width) / 2, rect.y + 20, width, rect.height);
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
			if (listingStandard.CheckboxLabeledReturned("DebugGenerateWorldPathCostTexts".Translate(), ref debugGenerateWorldPathCostTexts, "DebugGenerateWorldPathCostTextsTooltip".Translate()))
			{
				if (Find.World != null)
				{
					LongEventHandler.QueueLongEvent(delegate ()
					{
						if (debugGenerateWorldPathCostTexts)
						{
							WorldPathTextMeshGenerator.GenerateTextMeshObjects();
						}
						else
						{
							WorldPathTextMeshGenerator.DestroyTextMeshObjects();
						}
					}, "VehiclesTextMeshBiomeGeneration", false, (Exception ex) => Log.Error($"{VehicleHarmony.LogLabel} Exception thrown while trying to generate TextMesh GameObjects for world map debugging. Please report to mod page."));
				}
			}
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

			listingStandard.CheckboxLabeled("DebugDrawRegions".Translate(), ref debugDrawRegions, "DebugDrawRegionsTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawRegionLinks".Translate(), ref debugDrawRegionLinks, "DebugDrawRegionLinksTooltip".Translate());
			listingStandard.CheckboxLabeled("DebugDrawRegionThings".Translate(), ref debugDrawRegionThings, "DebugDrawRegionThingsTooltip".Translate());

			listingStandard.GapLine(16);

			if (listingStandard.ButtonText("ShowRecentNews".Translate()))
			{
				string versionChecking = "Null";
				VehicleHarmony.updates.Clear();
				foreach (UpdateLog log in FileReader.ReadPreviousFiles(VehicleHarmony.VehicleMCP).OrderBy(log => Ext_Settings.CombineVersionString(log.UpdateData.currentVersion)))
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
						if (versionChecking == VehicleHarmony.LatestVersion)
						{
							label = "Latest Version";
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
			listingStandard.End();
		}
	}
}
