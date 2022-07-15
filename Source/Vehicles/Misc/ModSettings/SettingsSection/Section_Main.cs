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

		public float meleeDamageMultiplier = 1;
		public float rangedDamageMultiplier = 1;
		public float explosiveDamageMultiplier = 1;

		/* Turrets */
		public bool overheatMechanics = true;

		/* Boats */
		public bool passiveWaterWaves = true;

		/* Fishing */
		public float fishingMultiplier = 1f;
		public int fishingDelay = 10000;
		public int fishingSkillIncrease = 5;
		public bool fishingPersists = true;

		/* Aerial */
		public bool burnRadiusOnRockets = true;
		public bool deployOnLanding = true;
		public bool airDefenses = true;
		public bool dynamicWorldDrawing = true;
		public float delayDeployOnLanding = 0;

		/* Upgrades */
		public bool drawUpgradeInformationScreen = true;
		public bool overrideDrawColors = true;
		//REDO - Add hover over option for displaying info window?

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

			meleeDamageMultiplier = 1;
			rangedDamageMultiplier = 1;
			explosiveDamageMultiplier = 1;

			/* Turrets */
			overheatMechanics = true;

			/* Boats */
			passiveWaterWaves = true;

			/* Fishing */
			fishingMultiplier = 1f;
			fishingDelay = 10000;
			fishingSkillIncrease = 5;
			fishingPersists = true;

			/* Aerial */
			burnRadiusOnRockets = true;
			deployOnLanding = true;
			airDefenses = true;
			dynamicWorldDrawing = true;
			delayDeployOnLanding = 0;

			/* Upgrades */
			drawUpgradeInformationScreen = true;
			overrideDrawColors = true;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref beachMultiplier, nameof(beachMultiplier), 0f);
			Scribe_Values.Look(ref forceFactionCoastRadius, nameof(forceFactionCoastRadius), 1);

			Scribe_Values.Look(ref modifiableSettings, nameof(modifiableSettings), true, true);
			Scribe_Values.Look(ref fullVehiclePathing, nameof(fullVehiclePathing));
			Scribe_Values.Look(ref showDisabledVehicles, nameof(showDisabledVehicles));

			Scribe_Values.Look(ref meleeDamageMultiplier, nameof(meleeDamageMultiplier), 1);
			Scribe_Values.Look(ref rangedDamageMultiplier, nameof(rangedDamageMultiplier), 1);
			Scribe_Values.Look(ref explosiveDamageMultiplier, nameof(explosiveDamageMultiplier), 1);

			Scribe_Values.Look(ref overheatMechanics, nameof(overheatMechanics), true);

			Scribe_Values.Look(ref passiveWaterWaves, nameof(passiveWaterWaves), true);

			Scribe_Values.Look(ref fishingMultiplier, nameof(fishingMultiplier), 1f);
			Scribe_Values.Look(ref fishingDelay, nameof(fishingDelay), 10000);
			Scribe_Values.Look(ref fishingSkillIncrease, nameof(fishingSkillIncrease), 5);
			Scribe_Values.Look(ref fishingPersists, nameof(fishingPersists), true);

			Scribe_Values.Look(ref burnRadiusOnRockets, nameof(burnRadiusOnRockets), true);
			Scribe_Values.Look(ref deployOnLanding, nameof(deployOnLanding), true);
			Scribe_Values.Look(ref airDefenses, nameof(airDefenses), true);
			Scribe_Values.Look(ref dynamicWorldDrawing, nameof(dynamicWorldDrawing), true);
			Scribe_Values.Look(ref delayDeployOnLanding, nameof(delayDeployOnLanding), 0);

			Scribe_Values.Look(ref drawUpgradeInformationScreen, nameof(drawUpgradeInformationScreen), true);
			Scribe_Values.Look(ref overrideDrawColors, nameof(overrideDrawColors), true);
		}

		//REDO - TRANSLATIONS
		public override void DrawSection(Rect rect)
		{
			listingStandard = new Listing_Standard();

			Rect mainSettings = new Rect(rect.x + 20f, rect.y + 40f, rect.width - 40f, rect.height);
			var color = GUI.color;
			
			listingStandard.ColumnWidth = mainSettings.width / 3;
			listingStandard.Begin(mainSettings);

			listingStandard.Header("World/Map Generation", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(4);
			listingStandard.SliderLabeled("BeachGenMultiplier".Translate(), "BeachGenMultiplierTooltip".Translate(), "%", ref beachMultiplier, 0f, 2f, 100, 0);
			listingStandard.SliderLabeled("ForceSettlementCoast".Translate(), "ForceSettlementCoastTooltip".Translate(), "Tiles".Translate(), ref forceFactionCoastRadius, 0, 
				VehicleMod.MaxCoastalSettlementPush, 1, "EverySettlementToCoast".Translate());
			
			listingStandard.Header("General".Translate(), ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(4);
			listingStandard.CheckboxLabeled("VehiclesModifiableSettings".Translate(), ref modifiableSettings, "VehiclesModifiableSettingsTooltip".Translate());
			listingStandard.CheckboxLabeled("FullVehiclePathing".Translate(), ref fullVehiclePathing, "FullVehiclePathingTooltip".Translate());
			bool checkBefore = showDisabledVehicles;
			listingStandard.CheckboxLabeled("ShowDisabledVehicles".Translate(), ref showDisabledVehicles, "ShowDisabledVehiclesTooltip".Translate());
			listingStandard.Gap(4);

			if (checkBefore != showDisabledVehicles)
			{
				GizmoHelper.DesignatorsChanged(DesignationCategoryDefOf.Structure);
			}

			listingStandard.Header("VehicleDamageMultipliers".Translate(), ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(4);
			listingStandard.SliderLabeled("MeleeDamageMultiplier".Translate(), string.Empty, "%", ref meleeDamageMultiplier, 0, 2, multiplier: 100);
			listingStandard.SliderLabeled("RangedDamageMultiplier".Translate(), string.Empty, "%", ref rangedDamageMultiplier, 0, 2, multiplier: 100);
			listingStandard.SliderLabeled("ExplosiveDamageMultiplier".Translate(), string.Empty, "%", ref explosiveDamageMultiplier, 0, 2, multiplier: 100);

			listingStandard.Header("Turrets", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.CheckboxLabeled("VehicleTurretOverheatMechanics".Translate(), ref overheatMechanics, "VehicleTurretOverheatMechanicsTooltip".Translate());

			listingStandard.Header("Boats", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);

			listingStandard.CheckboxLabeled("PassiveWaterWaves".Translate(), ref passiveWaterWaves, "PassiveWaterWavesTooltip".Translate());

			listingStandard.NewColumn();
			string fishingHeader = "Fishing";
			if (!FishingCompatibility.fishingActivated)
			{
				GUI.enabled = false;
				GUI.color = UIElements.InactiveColor;
				fishingHeader = "Fishing (Not Active)";
			}
			listingStandard.Header(fishingHeader, ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(4);
			listingStandard.SliderLabeled("FishingMultiplier".Translate(), "FishingMultiplierTooltip".Translate(), "%", ref fishingMultiplier, 0.1f, 3, 100, 1);
			listingStandard.IntegerBox("FishingDelay".Translate(), "FishingDelayTooltip".Translate(), ref fishingDelay, listingStandard.ColumnWidth * 0.5f, 0, 0);
			listingStandard.Gap(8);
			listingStandard.IntegerBox("FishingSkill".Translate(), "FishingSkillTooltip".Translate(), ref fishingSkillIncrease, listingStandard.ColumnWidth * 0.5f, 0, 0);
			listingStandard.Gap(8);
			listingStandard.CheckboxLabeled("FishingPersists".Translate(), ref fishingPersists, "FishingPersistsTooltip".Translate());
			listingStandard.Gap(4);

			GUI.enabled = true;
			GUI.color = color;

			listingStandard.Header("Aerial", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.Gap(4);
			listingStandard.CheckboxLabeled("VehicleRocketsBurnRadius".Translate(), ref burnRadiusOnRockets, "VehicleRocketsBurnRadiusTooltip".Translate());
			listingStandard.CheckboxLabeled("VehicleAirDefensesActive".Translate(), ref airDefenses, "VehicleAirDefensesActiveTooltip".Translate());
			listingStandard.CheckboxLabeled("VehicleDeployOnLanding".Translate(), ref deployOnLanding, "VehicleDeployOnLandingTooltip".Translate());
			if (deployOnLanding)
			{
				//REDO - ADD TOOLTIP TRANSLATION
				listingStandard.Gap(16);
				listingStandard.SliderLabeled("VehicleDelayOnLanding".Translate(), "VehicleDelayOnLandingTooltip".Translate(), "seconds", ref delayDeployOnLanding, 0, 5, 1, 1);
			}
			listingStandard.CheckboxLabeled("VehicleDynamicDrawing".Translate(), ref dynamicWorldDrawing, "VehicleDynamicDrawingTooltip".Translate());
			listingStandard.Gap(8);

			GUI.enabled = false; //Upgrades disabled for now
			GUI.color = UIElements.InactiveColor;

			listingStandard.Header("Upgrades", ListingExtension.BannerColor, GameFont.Small, TextAnchor.MiddleCenter);
			listingStandard.CheckboxLabeled("DrawUpgradeInformationScreen".Translate(), ref drawUpgradeInformationScreen, "DrawUpgradeInformationScreenTooltip".Translate());
			listingStandard.CheckboxLabeled("VehicleOverrideDrawColor".Translate(), ref overrideDrawColors, "VehicleOverrideDrawColorTooltip".Translate());

			GUI.enabled = true;
			GUI.color = color;

			listingStandard.End();
		}
	}
}
