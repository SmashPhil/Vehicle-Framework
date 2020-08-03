using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;


namespace Vehicles
{
    public class VehiclesModSettings : ModSettings
    {
        public float beachMultiplier = 0f;
        public int forceFactionCoastRadius = 1;
        public int daysToResetRegions = 2;

        public bool matchWaterTerrain;
        public bool shuffledCannonFire = true;
        public bool riverTravel = true;
        public bool boatSizeMatters = true;
        public bool passiveWaterWaves = true;

        public bool drawUpgradeInformationScreen = true;
        public bool useInGameTime = true;
        public bool fullVehiclePathing = true; //disable if you need every ounce of performance

        public float fishingMultiplier = 1f;
        public int fishingDelay = 10000;
        public int fishingSkillIncrease = 5;
        public bool fishingPersists = true;

        public bool debugDraftAnyShip;
        public bool debugDisableWaterPathing;
        public bool debugDisableSmoothPathing = true;

        public bool debugSpawnBoatBuildingGodMode;

        public bool debugDrawCannonGrid;
        public bool debugDrawNodeGrid;
        public bool debugDrawVehicleTracks;
        public bool debugDrawVehiclePathCosts;

        public bool debugDrawRegions;
        public bool debugDrawRegionLinks;
        public bool debugDrawRegionThings;

        /* Not displayed in mod settings page */
        public bool showAllCargoItems;

        public int CoastRadius => forceFactionCoastRadius;
        public float FishingSkillValue => fishingSkillIncrease / 100;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref beachMultiplier, "beachMultiplier", 0f);
            Scribe_Values.Look(ref forceFactionCoastRadius, "forceFactionCoastRadius", 1);
            Scribe_Values.Look(ref daysToResetRegions, "daysToResetRegions", 2);

            Scribe_Values.Look(ref drawUpgradeInformationScreen, "drawUpgradeInformationScreen", true);
            Scribe_Values.Look(ref useInGameTime, "useInGameTime", true);

            Scribe_Values.Look(ref matchWaterTerrain, "matchWaterTerrain", true);
            Scribe_Values.Look(ref shuffledCannonFire, "shuffledCannonFire", true);
            Scribe_Values.Look(ref riverTravel, "riverTravel", true);
            Scribe_Values.Look(ref boatSizeMatters, "boatSizeMatters", true);
            Scribe_Values.Look(ref passiveWaterWaves, "passiveWaterWaves", true);

            Scribe_Values.Look(ref fishingMultiplier, "fishingMultiplier", 1f);
            Scribe_Values.Look(ref fishingDelay, "fishingDelay", 10000);
            Scribe_Values.Look(ref fishingSkillIncrease, "fishingSkillIncrease", 5);
            Scribe_Values.Look(ref fishingPersists, "fishingPersists", true);

            /* Non Dialog Variables */
            Scribe_Values.Look(ref showAllCargoItems, "showAllCargoItems");

            if(Prefs.DevMode)
            {
                Scribe_Values.Look(ref debugDraftAnyShip, "debugDraftAnyShip", false);
                Scribe_Values.Look(ref debugDisableWaterPathing, "debugDisableWaterPathing", false);
                Scribe_Values.Look(ref debugDisableSmoothPathing, "debugDisableSmoothPathing", false);

                Scribe_Values.Look(ref debugSpawnBoatBuildingGodMode, "debugSpawnBoatBuidingGodMode", false);

                Scribe_Values.Look(ref debugDrawCannonGrid, "debugDrawCannonGrid", false);
                Scribe_Values.Look(ref debugDrawNodeGrid, "debugDrawNodeGrid", false);
                Scribe_Values.Look(ref debugDrawVehicleTracks, "debugDrawVehicleTracks", false);
                Scribe_Values.Look(ref debugDrawVehiclePathCosts, "debugDrawVehiclePathCosts", false);

                Scribe_Values.Look(ref debugDrawRegions, "debugDrawRegions", false);
                Scribe_Values.Look(ref debugDrawRegionLinks, "debugDrawRegionLinks", false);
                Scribe_Values.Look(ref debugDrawRegionThings, "debugDrawRegionThings", false);
            }
            base.ExposeData();
        }
    }

    public class VehicleMod : Mod
    {
        public VehiclesModSettings settings;
        public static VehicleMod mod;

        public VehicleMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<VehiclesModSettings>();
            mod = this;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var font = Text.Font;
            Text.Font = GameFont.Tiny;
            string credit = "Vehicles! - by Smash Phil";
            Widgets.Label(new Rect(inRect.width - (6 * credit.Count()), inRect.height + 64f, inRect.width, inRect.height), credit);
            Text.Font = font;

            Listing_Standard listingStandard = new Listing_Standard();
            Rect settingsCategory = new Rect(inRect.width / 2 - (inRect.width / 12), inRect.y, inRect.width / 6, inRect.height);
            Rect pageRect = new Rect(settingsCategory.x - settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);

            if(Prefs.DevMode || currentPage == SettingsPage.DevMode)
            {
                Rect emergencyReset = new Rect(inRect.width - settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);
                listingStandard.Begin(emergencyReset);
                if(listingStandard.ButtonText("DevModeReset".Translate()))
                {
                    this.ResetToDefaultValues();
                }
                listingStandard.End();
            }
            //BoatsResetToDefault

            listingStandard.Begin(pageRect);
            if(currentPage == SettingsPage.MainSettings || currentPage == SettingsPage.DevMode)
            {
                listingStandard.ButtonText(string.Empty);
            }
            else if(currentPage == SettingsPage.Stats || currentPage == SettingsPage.Research)
            {
                
            }
            listingStandard.End();

            listingStandard.Begin(settingsCategory);
            if(listingStandard.ButtonText(EnumToString(currentPage)))
            {
                FloatMenuOption op1 = new FloatMenuOption("MainSettings".Translate(), () => currentPage = SettingsPage.MainSettings, MenuOptionPriority.Default, null, null, 0f, null, null);
                FloatMenuOption op2 = new FloatMenuOption("Vehicles".Translate(), () => currentPage = SettingsPage.Boats, MenuOptionPriority.Default, null, null, 0f, null, null);
                List<FloatMenuOption> options = new List<FloatMenuOption>() { op1, op2 };
                if(Prefs.DevMode)
                {
                    FloatMenuOption op3 = new FloatMenuOption("DevModeShips".Translate(), () => currentPage = SettingsPage.DevMode, MenuOptionPriority.Default, null, null, 0f, null, null);
                    options.Add(op3);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
            listingStandard.End();

            Rect propsReset = new Rect(settingsCategory.x + settingsCategory.width, settingsCategory.y, settingsCategory.width, settingsCategory.height);
            listingStandard.Begin(propsReset);
            if(listingStandard.ButtonText("BoatsReset".Translate()))
            {
                if (currentPage == SettingsPage.MainSettings)
                {
                    this.ResetToDefaultValues();
                }
                /*else if (currentPage == SRTS.SettingsCategory.Stats || currentPage == SRTS.SettingsCategory.Research)
                {
                    FloatMenuOption op1 = new FloatMenuOption("ResetThisSRTS".Translate(), () => props.ResetToDefaultValues(), MenuOptionPriority.Default, null, null, 0f, null, null);
                    FloatMenuOption op2 = new FloatMenuOption("ResetAll".Translate(), delegate ()
                    {
                        for (int i = 0; i < settings.defProperties.Count; i++)
                        {
                            SRTS_DefProperties p = settings.defProperties.ElementAt(i).Value;
                            this.ReferenceDefCheck(ref p);
                            p.ResetToDefaultValues();
                        }
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { op1, op2 }));
                }*/
            }
            listingStandard.End();

            if (currentPage == SettingsPage.MainSettings)
            {
                Rect mainSettings = new Rect(inRect.x, inRect.y + 30f, inRect.width / 3, inRect.height);
                listingStandard.Begin(mainSettings);
                bool beachLarge = settings.beachMultiplier > 150f;
                listingStandard.Settings_SliderLabeled(beachLarge ? "BeachGenMultiplierLarge".Translate() : "BeachGenMultiplier".Translate(),
                    "%", ref settings.beachMultiplier, 0f, 200f, 1f, 0);
                
                listingStandard.Gap(16f);

                listingStandard.Settings_SliderLabeled("ForceSettlementCoast".Translate(), "Tiles".Translate(), ref settings.forceFactionCoastRadius, 0, 11, 9999, "Everything".Translate());

                listingStandard.Gap(16f);

                listingStandard.CheckboxLabeled("DrawUpgradeInformationScreen".Translate(), ref settings.drawUpgradeInformationScreen, "DrawUpgradeInformationScreenTooltip".Translate());
                listingStandard.CheckboxLabeled("UseIngameTime".Translate(settings.useInGameTime ? "IngameTime".Translate() : "RealTime".Translate()), ref settings.useInGameTime, "UseIngameTimeTooltip".Translate());
                listingStandard.CheckboxLabeled("FullVehiclePathing".Translate(), ref settings.fullVehiclePathing, "FullVehiclePathingTooltip".Translate());

                listingStandard.Gap(16f);

                listingStandard.CheckboxLabeled("PassiveWaterWaves".Translate(), ref settings.passiveWaterWaves, "PassiveWaterWavesTooltip".Translate());
                listingStandard.CheckboxLabeled("ShuffledCannonFire".Translate(), ref settings.shuffledCannonFire, "ShuffledCannonFireTooltip".Translate());
                listingStandard.CheckboxLabeled("RiverTravelAllowed".Translate(), ref settings.riverTravel, "RiverTravelAllowedTooltip".Translate());
                
                //listingStandard.Settings_SliderLabeled("ResetWaterRegions".Translate(), "Days".Translate(), ref settings.daysToResetRegions, 0, 7, -1, string.Empty, 0, "Disabled".Translate());

                if (settings.riverTravel)
                {
                    listingStandard.CheckboxLabeled("BoatSizeMattersOnRivers".Translate(), ref settings.boatSizeMatters, "BoatSizeMattersOnRiversTooltip".Translate());
                }
                if(FishingCompatibility.fishingActivated)
                {
                    listingStandard.GapLine(16f);
                    string fishDelay = settings.fishingDelay.ToString();
                    string fishSkill = settings.fishingSkillIncrease.ToString();
                    listingStandard.Label("FishingMultiplier".Translate(Math.Round(settings.fishingMultiplier, 2)), -1, "FishingMultiplierTooltip".Translate());
                    settings.fishingMultiplier = listingStandard.Slider((float)settings.fishingMultiplier, 1f, 4f);
                    listingStandard.Label("FishingDelay".Translate(), -1, "FishingDelayTooltip".Translate());
                    listingStandard.IntEntry(ref settings.fishingDelay, ref fishDelay);
                    listingStandard.Label("FishingSkill".Translate(), -1, "FishingSkillTooltip".Translate());
                    listingStandard.IntEntry(ref settings.fishingSkillIncrease, ref fishSkill);
                    listingStandard.CheckboxLabeled("FishingPersists".Translate(), ref settings.fishingPersists, "FishingPersistsTooltip".Translate());
                }
                listingStandard.End();
            }
            else if(currentPage == SettingsPage.DevMode)
            {
                float width = inRect.width / 1.5f;
                Rect devMode = new Rect((inRect.width - width) / 2, inRect.y + 45f, width, inRect.height);
                listingStandard.Begin(devMode);
                listingStandard.Settings_Header("DevModeShips".Translate(), SPSettings.highlightColor, GameFont.Medium, TextAnchor.MiddleCenter);

                listingStandard.GapLine(16f);
                listingStandard.CheckboxLabeled("DebugDraftAnyShip".Translate(), ref settings.debugDraftAnyShip, "DebugDraftAnyShipTooltip".Translate());
                listingStandard.CheckboxLabeled("DebugDisablePathing".Translate(), ref settings.debugDisableWaterPathing, "DebugDisablePathingTooltip".Translate());
                //listingStandard.CheckboxLabeled("DebugDisableSmoothPathing".Translate(), ref settings.debugDisableSmoothPathing, "DebugDisableSmoothPathingTooltip".Translate());
                listingStandard.CheckboxLabeled("DebugSpawnVehiclesGodMode".Translate(), ref settings.debugSpawnBoatBuildingGodMode);

                listingStandard.CheckboxLabeled("DebugCannonDrawer".Translate(), ref settings.debugDrawCannonGrid);
                listingStandard.CheckboxLabeled("DebugDrawNodeGrid".Translate(), ref settings.debugDrawNodeGrid);
                listingStandard.CheckboxLabeled("DebugDrawVehicleTracks".Translate(), ref settings.debugDrawVehicleTracks);
                listingStandard.CheckboxLabeled("DebugWriteVehiclePathingCosts".Translate(), ref settings.debugDrawVehiclePathCosts);

                listingStandard.CheckboxLabeled("DebugDrawRegions".Translate(), ref settings.debugDrawRegions);
                listingStandard.CheckboxLabeled("DebugDrawRegionLinks".Translate(), ref settings.debugDrawRegionLinks);
                listingStandard.CheckboxLabeled("DebugDrawRegionThings".Translate(), ref settings.debugDrawRegionThings);

                listingStandard.End();
            }

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Vehicles".Translate();
        }

        private void ResetToDefaultValues()
        {
            SoundDefOf.Click.PlayOneShotOnCamera(null);
            settings.beachMultiplier = 0f;
            settings.forceFactionCoastRadius = 0;

            settings.shuffledCannonFire = true;
            settings.riverTravel = true;
            settings.boatSizeMatters = true;

            settings.fishingMultiplier = 1f;
            settings.fishingDelay = 10000;
            settings.fishingSkillIncrease = 5;
            settings.fishingPersists = true;

            settings.debugDraftAnyShip = false;
            settings.debugDisableWaterPathing = false;
            settings.debugDisableSmoothPathing = true;
            settings.debugSpawnBoatBuildingGodMode = false;

            settings.debugDrawCannonGrid = false;
            settings.debugDrawNodeGrid = false;
            settings.debugDrawVehicleTracks = false;
            settings.debugDrawVehiclePathCosts = false;

            settings.debugDrawRegions = false;
            settings.debugDrawRegionLinks = false;
            settings.debugDrawRegionThings = false;
        }

        private string EnumToString(SettingsPage page)
        {
            switch(page)
            {
                case SettingsPage.MainSettings:
                    return "MainSettings".Translate();
                case SettingsPage.DevMode:
                    return "DevModeShips".Translate();
                case SettingsPage.Boats:
                    return "Vehicles".Translate();
            }
            Log.Error(page.ToString() + " Page has not been implemented yet. - Smash Phil");
            return page.ToString();
        }

        enum SettingsPage { MainSettings, Boats, Stats, Research, Upgrades, DevMode}
        enum StatName { }


        public string currentKey;

        public Vector2 scrollPosition;

        private SettingsPage currentPage;
    }
}
