using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[HeaderTitle(Label = "VehicleProperties", Translate = true)]
	public class VehicleProperties
	{
		[PostToSettings(Label = "VehicleFishingEnabled", Tooltip = "VehicleFishingEnabledTooltip", Translate = true, UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Sea)]
		[DisableSettingConditional(MayRequireAny = new string[] { CompatibilityPackageIds.VE_Fishing })]
		public bool fishing = false;

		public VehicleTrack track;

		public List<VehicleJobLimitations> vehicleJobLimitations = new List<VehicleJobLimitations>();

		public bool diagonalRotation = true;
		[PostToSettings(Label = "ManhunterTargetsVehicle", Tooltip = "ManhunterTargetsVehicleTooltip", Translate = true, UISettingsType = UISettingsType.Checkbox)]
		public bool manhunterTargetsVehicle = false;

		public string healthLabel_Healthy = "MissingHealthyLabel";
		public string healthLabel_Injured = "MissingInjuredLabel";
		public string healthLabel_Immobile = "MissingImmobileLabel";
		public string healthLabel_Dead = "MissingDeadLabel";
		public string healthLabel_Beached = "MissingBeachedLabel";

		public string iconTexPath;
		public bool generateThingIcon = true;

		//---------------   Pathing   ---------------

		public bool defaultTerrainImpassable = false;
		public bool defaultBiomesImpassable = false;

		// Local Pathing
		/// <summary>
		/// Additional tick cost for snow, clamped between 0 and 450 ticks.
		/// </summary>
		public SimpleDictionary<SnowCategory, int> customSnowCategoryTicks;
		/// <summary>
		/// Set to 10000 for impassable terrain
		/// </summary>
		public SimpleDictionary<TerrainDef, int> customTerrainCosts;
		/// <summary>
		/// Set to 10000 for impassable thing
		/// </summary>
		public SimpleDictionary<ThingDef, int> customThingCosts;

		// World Pathing
		public float offRoadMultiplier = 1;
		public SimpleDictionary<RiverDef, float> customRiverCosts = new SimpleDictionary<RiverDef, float>();
		public SimpleDictionary<BiomeDef, float> customBiomeCosts = new SimpleDictionary<BiomeDef, float>();
		public SimpleDictionary<Hilliness, float> customHillinessCosts = new SimpleDictionary<Hilliness, float>();
		public SimpleDictionary<RoadDef, float> customRoadCosts = new SimpleDictionary<RoadDef, float>();

		//-------------------------------------------

		[PostToSettings(Label = "VehicleWinterSpeedMultiplier", Tooltip = "VehicleWinterSpeedMultiplierTooltip",Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float winterSpeedMultiplier = 2.5f;
		[PostToSettings(Label = "VehicleWorldSpeedMultiplier", Tooltip = "VehicleWorldSpeedMultiplierTooltip", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float worldSpeedMultiplier = 2.5f;

		public List<FactionDef> restrictToFactions;

		public RiverDef riverTraversability;
		public List<VehicleRole> roles  = new List<VehicleRole>();

		public IEnumerable<string> ConfigErrors()
		{
			yield break;
		}

		public void ResolveReferences(VehicleDef vehicleDef)
		{
			vehicleJobLimitations ??= new List<VehicleJobLimitations>();

			customBiomeCosts ??= new SimpleDictionary<BiomeDef, float>();
			customHillinessCosts ??= new SimpleDictionary<Hilliness, float>();
			customRoadCosts ??= new SimpleDictionary<RoadDef, float>();
			customTerrainCosts ??= new SimpleDictionary<TerrainDef, int>();
			customThingCosts ??= new SimpleDictionary<ThingDef, int>();
			customSnowCategoryTicks ??= new SimpleDictionary<SnowCategory, int>();
			
			roles.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.hitbox.Initialize(vehicleDef));
		}

		public void PostDefDatabase(VehicleDef vehicleDef)
		{
			string defName = vehicleDef.defName;

			XmlHelper.FillDefaults_Enum(defName, nameof(customSnowCategoryTicks), customSnowCategoryTicks);
			XmlHelper.FillDefaults_Def(defName, nameof(customTerrainCosts), customTerrainCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customThingCosts), customThingCosts);

			XmlHelper.FillDefaults_Def(defName, nameof(customRiverCosts), customRiverCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customBiomeCosts), customBiomeCosts);
			XmlHelper.FillDefaults_Enum(defName, nameof(customHillinessCosts), customHillinessCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customRoadCosts), customRoadCosts);
		}
	}
}