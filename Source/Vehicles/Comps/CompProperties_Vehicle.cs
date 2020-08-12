using RimWorld;
using Verse;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Vehicles.Jobs;
using Vehicles.Defs;

namespace Vehicles
{
    public enum VehiclePermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum PowerType { Manual, WindPowered, Steam, Fuel, Nuclear, Archotech}
    public enum NavigationCategory { Manual, Opportunistic, Automatic }
    public enum VehicleCategory { Misc, Transport, Trader, Combat, Hybrid }
    public enum VehicleType { Sea, Air, Land}

    //public enum RiverTraversability { Creek, River, LargeRiver, HugeRiver, Impassable }
    public class CompProperties_Vehicle : CompProperties
    {

        public CompProperties_Vehicle()
        {
            compClass = typeof(CompVehicle);
        }

        public bool downable = false;
        public bool movesWhenDowned = false;
        public float ticksBetweenRepair = 250f;
        public bool nameable = false;
        
        public float hitboxOffsetX = 0f;
        public float hitboxOffsetZ = 0f;

        public bool fishing = false;
        public float wakeMultiplier = 1.6f;
        public float wakeSpeed = 1.6f;
        public float visibility = 0.5f;

        public VehicleDamageMultipliers vehicleDamageMultipliers = new VehicleDamageMultipliers()
        {
            meleeDamageMultiplier = 0.01f,
            rangedDamageMultiplier = 0.1f,
            explosiveDamageMultiplier = 10f
        };

        public List<VehicleJobLimitations> vehicleJobLimitations = new List<VehicleJobLimitations>()
        {
            new VehicleJobLimitations("UpgradeVehicle", 3),
            new VehicleJobLimitations("RepairShip", 2),
            new VehicleJobLimitations("LoadUpgradeMaterials", 2)
        };

        public float armor = 1f;
        public float cargoCapacity = 200;

        public bool diagonalRotation = true;
        public bool reserveFullHitbox = true; //Discontinued from Pathing
        internal float turnSpeed = 0.1f; //Discontinued from SmoothPathing

        public bool manhunterTargetsVehicle = false;

        public Vector2 displayUICoord;
        public Vector2 displayUISize;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Broken Down";

        public string healthLabel_Beached = "Beached";

        public string iconTexPath = "UI/DefaultVehicleIcon";
        public bool generateThingIcon = true;

        public Dictionary<TerrainDef, int> customTerrainCosts; //Add to guide
        public Dictionary<ThingDef, int> customThingCosts; //Add to guide : implement

        public Dictionary<BiomeDef, float> customBiomeCosts; //Add to guide
        public Dictionary<Hilliness, float> customHillinessCosts; //Add to guide
        public Dictionary<RoadDef, float> customRoadCosts; //Add to guide
        public float winterPathCostMultiplier = 1f; //Add to guide
        public float worldSpeedMultiplier = 1f;

        public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;
        public PowerType vehiclePowerType = PowerType.Fuel;
        public VehicleCategory vehicleCategory = VehicleCategory.Misc;
        public TechLevel vehicleTech = TechLevel.Industrial;
        public VehicleType vehicleType = VehicleType.Land;
        public NavigationCategory defaultNavigation = NavigationCategory.Opportunistic;

        public RiverDef riverTraversability = RiverDefOf.HugeRiver;
        public List<VehicleRole> roles  = new List<VehicleRole>();
        public SoundDef soundWhileDrafted; //REDO

        public ThingDef buildDef;
    }
}