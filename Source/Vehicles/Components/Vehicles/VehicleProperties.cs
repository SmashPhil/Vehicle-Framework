using System.Reflection;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
	[HeaderTitle(Label = "VehicleProperties", Translate = true)]
	public class VehicleProperties
	{
		[PostToSettings(Label = "VehicleVisibilityWorldMap", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 1, MinValueDisplay = "Invisible", MaxValueDisplay = "FullyVisible", RoundDecimalPlaces = 1)]
		public float visibility = 0.5f;

		[PostToSettings(Label = "VehicleFishingEnabled", Translate = true, UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Sea)]
		public bool fishing = false;
		public float wakeMultiplier = 1.6f;
		public float wakeSpeed = 1.6f;

		[PostToSettings(Label = "VehicleDamageMultipliers", Translate = true, ParentHolder = true)]
		public VehicleDamageMultipliers vehicleDamageMultipliers = new VehicleDamageMultipliers()
		{
			meleeDamageMultiplier = 0.01f,
			rangedDamageMultiplier = 0.1f,
			explosiveDamageMultiplier = 2.5f
		};
		//[PostToSettings(Label = "VehicleJobLimitations", Translate = true)]
		public List<VehicleJobLimitations> vehicleJobLimitations = new List<VehicleJobLimitations>()
		{
			new VehicleJobLimitations("UpgradeVehicle", 3),
			new VehicleJobLimitations("RepairVehicle", 2),
			new VehicleJobLimitations("LoadUpgradeMaterials", 2),
		};

		public bool diagonalRotation = true;
		[PostToSettings(Label = "ManhunterTargetsVehicle", Tooltip = "ManhunterTargetsVehicleTooltip", Translate = true, UISettingsType = UISettingsType.Checkbox)]
		public bool manhunterTargetsVehicle = false;

		//REDO - Add Translation
		public string healthLabel_Healthy = "Operational";
		public string healthLabel_Injured = "Needs Repairs";
		public string healthLabel_Immobile = "Inoperable";
		public string healthLabel_Dead = "Broken Down";

		public string healthLabel_Beached = "Beached";

		public string iconTexPath;
		public bool generateThingIcon = true;

		public bool defaultTerrainImpassable = false;
		public int pathTurnCost = 10;
		public float snowPathingMultiplier = 0.5f;
		public Dictionary<TerrainDef, int> customTerrainCosts;
		public Dictionary<ThingDef, int> customThingCosts;

		public Dictionary<BiomeDef, float> customBiomeCosts = new Dictionary<BiomeDef, float>();
		public Dictionary<Hilliness, float> customHillinessCosts = new Dictionary<Hilliness, float>();
		public Dictionary<RoadDef, float> customRoadCosts = new Dictionary<RoadDef, float>();

		[PostToSettings(Label = "VehicleWinterCostMultiplier", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float winterPathCostMultiplier = 1f;
		[PostToSettings(Label = "VehicleWorldSpeedMultiplier", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float worldSpeedMultiplier = 1f;

		public SimpleCurve overweightSpeedCurve;

		public List<FactionDef> restrictToFactions;

		public RiverDef riverTraversability;
		public List<VehicleRole> roles  = new List<VehicleRole>();
		public SoundDef soundWhileDrafted; //REDO
	}
}