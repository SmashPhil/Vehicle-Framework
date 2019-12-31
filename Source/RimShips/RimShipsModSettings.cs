using UnityEngine;
using Verse;
using System;

namespace RimShips
{
    public class RimshipsModSettings : ModSettings
    {
        public float beachMultiplier = 0f;
        public bool forceFactionCoastOption = true;
        public int forceFactionCoastRadius = 1;

        public bool matchWaterTerrain;
        public bool shuffledCannonFire = true;
        public bool riverTravel = true;
        public bool boatSizeMatters = true;

        public float fishingMultiplier = 1f;
        public int fishingDelay = 10000;
        public int fishingSkillIncrease = 5;
        public bool fishingPersists = true;

        public bool debugDraftAnyShip;
        public bool debugDisableWaterPathing;
        public bool debugDrawRegions;
        public bool debugDrawRegionLinks;
        public bool debugDrawRegionThings;
        public int CoastRadius => forceFactionCoastOption ? forceFactionCoastRadius : 0;
        public float FishingSkillValue => fishingSkillIncrease / 100;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref beachMultiplier, "beachMultiplier");
            Scribe_Values.Look(ref forceFactionCoastRadius, "forceFactionCoastRadius", 1);
            Scribe_Values.Look(ref forceFactionCoastOption, "forceFactionCoastOption", true);

            Scribe_Values.Look(ref matchWaterTerrain, "matchWaterTerrain", true);
            Scribe_Values.Look(ref shuffledCannonFire, "shuffledCannonFire", true);
            Scribe_Values.Look(ref riverTravel, "riverTravel", true);
            Scribe_Values.Look(ref boatSizeMatters, "boatSizeMatters", true);

            Scribe_Values.Look(ref fishingMultiplier, "fishingMultiplier", 1f);
            Scribe_Values.Look(ref fishingDelay, "fishingDelay", 10000);
            Scribe_Values.Look(ref fishingSkillIncrease, "fishingSkillIncrease", 5);
            Scribe_Values.Look(ref fishingPersists, "fishingPersists", true);
            base.ExposeData();
        }
    }

    public class RimShipMod : Mod
    {
        public RimshipsModSettings settings;
        public static RimShipMod mod;

        public RimShipMod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<RimshipsModSettings>();
            mod = this;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            Rect group1 = new Rect(inRect.x, inRect.y, inRect.width/2, inRect.height);
            listingStandard.Begin(group1);
            bool beachLarge = settings.beachMultiplier > 150f;
            listingStandard.Label(beachLarge ? "BeachGenMultiplierLarge".Translate(Mathf.Round(settings.beachMultiplier)) : "BeachGenMultiplier".Translate(Mathf.Round(settings.beachMultiplier)),
                -1f, beachLarge ? "BeachGenMultiplierLargeTooltip".Translate() : "BeachGenMultiplierTooltip".Translate());
            settings.beachMultiplier = listingStandard.Slider(settings.beachMultiplier, 0f, 200f);
            listingStandard.GapLine(16f);

            listingStandard.CheckboxLabeled("ForceSettlementCoastOption".Translate(), ref settings.forceFactionCoastOption, "ForceSettlementCoastTooltip".Translate());
            if(settings.forceFactionCoastOption)
            {
                listingStandard.Label("ForceSettlementCoast".Translate(Mathf.Round(settings.forceFactionCoastRadius)));
                settings.forceFactionCoastRadius = (int)listingStandard.Slider((float)settings.forceFactionCoastRadius, 0f, 10f);
            }
            //listingStandard.CheckboxLabeled("MatchWaterTerrain".Translate(), ref settings.matchWaterTerrain, "MatchWaterTerrainTooltip".Translate());
            listingStandard.GapLine(16f);

            listingStandard.CheckboxLabeled("ShuffledCannonFire".Translate(), ref settings.shuffledCannonFire, "ShuffledCannonFireTooltip".Translate());
            listingStandard.CheckboxLabeled("RiverTravelAllowed".Translate(), ref settings.riverTravel, "RiverTravelAllowedTooltip".Translate());

            if(settings.riverTravel)
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

            listingStandard.Gap(20f);
            listingStandard.Label("DevModeShips".Translate(), -1f, "DevModeShipsTooltip".Translate());
            if (Prefs.DevMode)
            {
                
                listingStandard.GapLine(16f);
                listingStandard.CheckboxLabeled("DebugDraftAnyShip".Translate(), ref settings.debugDraftAnyShip, "DebugDraftAnyShipTooltip".Translate());
                listingStandard.CheckboxLabeled("DebugDisablePathing".Translate(), ref settings.debugDisableWaterPathing, "DebugDisablePathingTooltip".Translate());
                listingStandard.CheckboxLabeled("DebugDrawRegions".Translate(), ref settings.debugDrawRegions);
                listingStandard.CheckboxLabeled("DebugDrawRegionLinks".Translate(), ref settings.debugDrawRegionLinks);
                listingStandard.CheckboxLabeled("DebugDrawRegionThings".Translate(), ref settings.debugDrawRegionThings);
            }

            listingStandard.End();
            base.DoSettingsWindowContents(group1);
        }

        public override string SettingsCategory()
        {
            return "RimShips".Translate();
        }
    }
}
