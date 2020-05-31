using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Vehicles
{
    public enum VehiclePermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum PowerType { Manual, WindPowered, Steam, Fuel, Nuclear, Archotech}
    public enum NavigationCategory { Manual, Automatic}
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

        public float armor = 1f;
        public float cargoCapacity = 200;

        public bool diagonalRotation = true;
        public float turnSpeed = 0.1f;

        public bool manhunterTargetsVehicle = false;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Broken Down";

        public string healthLabel_Beached = "Beached";

        public string iconTexPath = "UI/DefaultVehicleIcon";

        public VehiclePermissions vehicleMovementPermissions = VehiclePermissions.DriverNeeded;
        public PowerType vehiclePowerType = PowerType.Fuel;
        public VehicleCategory vehicleCategory = VehicleCategory.Misc;
        public TechLevel vehicleTech = TechLevel.Industrial;
        public VehicleType vehicleType = VehicleType.Land;
        public NavigationCategory navigationCategory = NavigationCategory.Manual;

        public RiverDef riverTraversability = RiverDefOf.HugeRiver;
        public List<ShipRole> roles  = new List<ShipRole>();
        public SoundDef soundWhileDrafted; //REDO
        public ThingDef buildDef;
    }

    public struct VehicleDamageMultipliers
    {
        public float meleeDamageMultiplier;
        public float rangedDamageMultiplier;
        public float explosiveDamageMultiplier;
    }
}