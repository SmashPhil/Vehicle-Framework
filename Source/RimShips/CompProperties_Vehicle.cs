using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Vehicles
{
    public enum VehiclePermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum PowerType { Manual, WindPowered, Steam, Fuel, Nuclear, Spacer}
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

        public float armor = 5f;
        public float cargoCapacity = 1000;

        public bool diagonalRotation = true;
        public float turnSpeed = 0.1f;

        public bool manhunterTargetsVehicle = false;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Sinking";

        public string healthLabel_Beached = "Beached";

        public string iconTexPath = "UI/DefaultVehicleIcon";

        public VehiclePermissions moveable = VehiclePermissions.DriverNeeded;
        public PowerType vehiclePowerType = PowerType.WindPowered;
        public VehicleCategory vehicleCategory = VehicleCategory.Misc;
        public TechLevel vehicleTech = TechLevel.Medieval;
        public VehicleType vehicleType = VehicleType.Land;
        public NavigationCategory navigationCategory = NavigationCategory.Manual;

        public RiverDef riverTraversability;
        public List<ShipRole> roles  = new List<ShipRole>();
        public SoundDef soundWhileMoving;
        public ThingDef buildDef;
    }
}