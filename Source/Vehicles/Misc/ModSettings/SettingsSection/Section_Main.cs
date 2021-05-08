using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public class Section_Main : SettingsSection
	{
		/* Map/World Generation */
		public float beachMultiplier = 0f;
		public int forceFactionCoastRadius = 1;

		/* General */
		public bool modifiableSettings = true;
		public bool fullVehiclePathing = true; //disable if you need every ounce of performance
		public bool showDisabledVehicles = true;

		/* Turrets */
		public bool overheatMechanics = true;

		/* Boats */
		public bool passiveWaterWaves = true;
		public bool riverTravel = true;
		public bool boatSizeMatters = true;

		/* Fishing */
		public float fishingMultiplier = 1f;
		public int fishingDelay = 10000;
		public int fishingSkillIncrease = 5;
		public bool fishingPersists = true;

		/* Aerial */
		public bool burnRadiusOnRockets = true;
		public bool deployOnLanding = true;
		public float delayDeployOnLanding = 0;

		/* Upgrades */
		public bool drawUpgradeInformationScreen = true;
		public bool useInGameTime = true;

		public override void ResetSettings()
		{
			base.ResetSettings();
			/* Map/World Generation */
			beachMultiplier = 0f;
			forceFactionCoastRadius = 1;

			/* General */
			modifiableSettings = true;
			fullVehiclePathing = true; //disable if you need every ounce of performance
			showDisabledVehicles = true;

			/* Turrets */
			overheatMechanics = true;

			/* Boats */
			passiveWaterWaves = true;
			riverTravel = true;
			boatSizeMatters = true;

			/* Fishing */
			fishingMultiplier = 1f;
			fishingDelay = 10000;
			fishingSkillIncrease = 5;
			fishingPersists = true;

			/* Aerial */
			burnRadiusOnRockets = true;
			deployOnLanding = true;
			delayDeployOnLanding = 0;

			/* Upgrades */
			drawUpgradeInformationScreen = true;
			useInGameTime = true;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref beachMultiplier, "beachMultiplier", 0f);
			Scribe_Values.Look(ref forceFactionCoastRadius, "forceFactionCoastRadius", 1);

			Scribe_Values.Look(ref modifiableSettings, "modifiableSettings", true, true);
			Scribe_Values.Look(ref fullVehiclePathing, "fullVehiclePathing");
			Scribe_Values.Look(ref showDisabledVehicles, "showDisabledVehicles");

			Scribe_Values.Look(ref overheatMechanics, "overheatMechanics", true);

			Scribe_Values.Look(ref passiveWaterWaves, "passiveWaterWaves", true);
			Scribe_Values.Look(ref riverTravel, "riverTravel", true);
			Scribe_Values.Look(ref boatSizeMatters, "boatSizeMatters", true);

			Scribe_Values.Look(ref fishingMultiplier, "fishingMultiplier", 1f);
			Scribe_Values.Look(ref fishingDelay, "fishingDelay", 10000);
			Scribe_Values.Look(ref fishingSkillIncrease, "fishingSkillIncrease", 5);
			Scribe_Values.Look(ref fishingPersists, "fishingPersists", true);

			Scribe_Values.Look(ref burnRadiusOnRockets, "burnRadiusOnRockets", true);
			Scribe_Values.Look(ref deployOnLanding, "deployOnLanding", true);
			Scribe_Values.Look(ref delayDeployOnLanding, "delayDeployOnLanding", 0);

			Scribe_Values.Look(ref drawUpgradeInformationScreen, "drawUpgradeInformationScreen", true);
			Scribe_Values.Look(ref useInGameTime, "useInGameTime", true);
		}

		//REDO - TRANSLATIONS
		public override void DrawSection(Rect rect)
		{
			Rect mainSettings = new Rect(rect.x + 20f, rect.y + 40f, rect.width - 40f, rect.height);
			var color = GUI.color;
			listingStandard = new Listing_Standard();
			listingStandard.ColumnWidth = mainSettings.width / 3;
			listingStandard.Begin(mainSettings);

			listingStandard.Header("World/Map Generation", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);

			listingStandard.Gap(16);
			listingStandard.SliderLabeled("BeachGenMultiplier".Translate(), "BeachGenMultiplierTooltip".Translate(), "%", ref beachMultiplier, 0f, 2f, 100, 0);
			listingStandard.Gap(16);
			listingStandard.SliderLabeled("ForceSettlementCoast".Translate(), "ForceSettlementCoastTooltip".Translate(), "Tiles".Translate(), ref forceFactionCoastRadius, 0, 
				VehicleMod.MaxCoastalSettlementPush, "EverySettlementToCoast".Translate());
			listingStandard.Gap(12);

			listingStandard.Header("General", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);

			listingStandard.CheckboxLabeled("VehiclesModifiableSettings".Translate(), ref modifiableSettings, "VehiclesModifiableSettingsTooltip".Translate());
			listingStandard.CheckboxLabeled("FullVehiclePathing".Translate(), ref fullVehiclePathing, "FullVehiclePathingTooltip".Translate());
			listingStandard.CheckboxLabeled("ShowDisabledVehicles".Translate(), ref showDisabledVehicles, "ShowDisabledVehiclesTooltip".Translate());

			listingStandard.Header("Turrets", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.CheckboxLabeled("VehicleTurretOverheatMechanics".Translate(), ref overheatMechanics, "VehicleTurretOverheatMechanicsTooltip".Translate());

			listingStandard.Header("Boats", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);

			listingStandard.CheckboxLabeled("PassiveWaterWaves".Translate(), ref passiveWaterWaves, "PassiveWaterWavesTooltip".Translate());
			listingStandard.CheckboxLabeled("RiverTravelAllowed".Translate(), ref riverTravel, "RiverTravelAllowedTooltip".Translate());
			if (riverTravel)
			{
				listingStandard.CheckboxLabeled("BoatSizeMattersOnRivers".Translate(), ref boatSizeMatters, "BoatSizeMattersOnRiversTooltip".Translate());
			}

			listingStandard.NewColumn();
			string fishingHeader = "Fishing";
			if (!FishingCompatibility.fishingActivated)
			{
				GUI.enabled = false;
				GUI.color = UIElements.InactiveColor;
				fishingHeader = "Fishing (Not Active)";
			}
			listingStandard.Header(fishingHeader, ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(16);
			listingStandard.SliderLabeled("FishingMultiplier".Translate(), "FishingMultiplierTooltip".Translate(), "%", ref fishingMultiplier, 0.1f, 3, 100, 1);
			listingStandard.Gap();
			listingStandard.IntegerBox("FishingDelay".Translate(), "FishingDelayTooltip".Translate(), ref fishingDelay, listingStandard.ColumnWidth * 0.5f, 0, 0);
			listingStandard.Gap();
			listingStandard.IntegerBox("FishingSkill".Translate(), "FishingSkillTooltip".Translate(), ref fishingSkillIncrease, listingStandard.ColumnWidth * 0.5f, 0, 0);
			listingStandard.Gap();
			listingStandard.CheckboxLabeled("FishingPersists".Translate(), ref fishingPersists, "FishingPersistsTooltip".Translate());

			GUI.enabled = true;
			GUI.color = color;

			listingStandard.Header("Aerial", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.CheckboxLabeled("VehicleRocketsBurnRadius".Translate(), ref burnRadiusOnRockets, "VehicleRocketsBurnRadiusTooltip".Translate());
			listingStandard.CheckboxLabeled("VehicleDeployOnLanding".Translate(), ref deployOnLanding, "VehicleDeployOnLandingTooltip".Translate());
			if (deployOnLanding)
			{
				//REDO - ADD TOOLTIP TRANSLATION
				listingStandard.Gap(16);
				listingStandard.SliderLabeled("VehicleDelayOnLanding".Translate(), "VehicleDelayOnLandingTooltip".Translate(), "seconds", ref delayDeployOnLanding, 0, 3, 1, 1);
			}
			listingStandard.Gap();

			listingStandard.Header("Upgrades", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.CheckboxLabeled("DrawUpgradeInformationScreen".Translate(), ref drawUpgradeInformationScreen, "DrawUpgradeInformationScreenTooltip".Translate());
			listingStandard.CheckboxLabeled("UseIngameTime".Translate(useInGameTime ? "IngameTime".Translate() : "RealTime".Translate()), ref useInGameTime, "UseIngameTimeTooltip".Translate());

			listingStandard.End();
		}
	}
}
