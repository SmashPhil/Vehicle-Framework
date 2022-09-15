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
		[PostToSettings(Label = "VehicleVisibilityWorldMap", Tooltip = "VehicleVisibilityWorldMapTooltip", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 3, RoundDecimalPlaces = 1)]
		public float visibility = 2.5f;

		[PostToSettings(Label = "VehicleFishingEnabled", Tooltip = "VehicleFishingEnabledTooltip", Translate = true, UISettingsType = UISettingsType.Checkbox, VehicleType = VehicleType.Sea)]
		[DisableSettingConditional(MayRequireAny = new string[] { ConditionalPatchApplier.VE_Fishing })]
		public bool fishing = false;
		public float wakeMultiplier = 1.6f;
		public float wakeSpeed = 1.6f;

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

		/// <summary>
		/// Do not use snow costs to try and set impassable terrain based on snow depth. It is not designed for that, and if it was it would lag
		/// </summary>
		public Dictionary<SnowCategory, int> customSnowCosts;
		/// <summary>
		/// Set to -1 or >= to 10000 for impassable terrain
		/// </summary>
		public Dictionary<TerrainDef, int> customTerrainCosts;
		/// <summary>
		/// Set to -1 or >= to 10000 for impassable thing
		/// </summary>
		public Dictionary<ThingDef, int> customThingCosts;

		public bool defaultBiomesImpassable = false;
		public Dictionary<RiverDef, float> customRiverCosts = new Dictionary<RiverDef, float>();
		public Dictionary<BiomeDef, float> customBiomeCosts = new Dictionary<BiomeDef, float>();
		public Dictionary<Hilliness, float> customHillinessCosts = new Dictionary<Hilliness, float>();
		public Dictionary<RoadDef, float> customRoadCosts = new Dictionary<RoadDef, float>();
		//-------------------------------------------

		[PostToSettings(Label = "VehicleWinterSpeedMultiplier", Tooltip = "VehicleWinterSpeedMultiplierTooltip",Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float winterSpeedMultiplier = 2.5f;
		[PostToSettings(Label = "VehicleWorldSpeedMultiplier", Tooltip = "VehicleWorldSpeedMultiplierTooltip", Translate = true, UISettingsType = UISettingsType.SliderFloat)]
		[SliderValues(MinValue = 0, MaxValue = 10, RoundDecimalPlaces = 1)]
		public float worldSpeedMultiplier = 4;

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

			customBiomeCosts ??= new Dictionary<BiomeDef, float>();
			customHillinessCosts ??= new Dictionary<Hilliness, float>();
			customRoadCosts ??= new Dictionary<RoadDef, float>();
			customTerrainCosts ??= new Dictionary<TerrainDef, int>();
			customThingCosts ??= new Dictionary<ThingDef, int>();
			customSnowCosts ??= new Dictionary<SnowCategory, int>();

			roles.OrderBy(c => c.hitbox.side == VehicleComponentPosition.BodyNoOverlap).ForEach(c => c.hitbox.Initialize(vehicleDef));
		}

		public void PostVehicleDefLoad(VehicleDef vehicleDef)
		{
			string defName = vehicleDef.defName;

			XmlHelper.FillDefaults_Enum(defName, nameof(customSnowCosts), customSnowCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customTerrainCosts), customTerrainCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customThingCosts), customThingCosts);

			XmlHelper.FillDefaults_Def(defName, nameof(customRiverCosts), customRiverCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customBiomeCosts), customBiomeCosts);
			XmlHelper.FillDefaults_Enum(defName, nameof(customHillinessCosts), customHillinessCosts);
			XmlHelper.FillDefaults_Def(defName, nameof(customRoadCosts), customRoadCosts);
		}
	}
}