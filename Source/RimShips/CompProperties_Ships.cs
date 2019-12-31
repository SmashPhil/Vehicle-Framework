using RimWorld;
using Verse;
using System.Collections.Generic;

namespace RimShips
{
    public enum ShipPermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum ShipType {Paddles, Sails, Steam, Fuel, Nuclear}

    public enum ShipCategory { Misc, Carrier, Trader, Combat }

    //public enum RiverTraversability { Creek, River, LargeRiver, HugeRiver, Impassable }
    public class CompProperties_Ships : CompProperties
    {

        public CompProperties_Ships()
        {
            this.compClass = typeof(CompShips);
        }

        public bool downable = false;
        public bool movesWhenDowned = false;
        public float ticksBetweenRepair = 250f;
        public bool nameable = false;
        public bool fishing = false;
        public float hitboxOffsetX = 0f;
        public float hitboxOffsetZ = 0f;
        public float wakeMultiplier = 1.25f;
        public float wakeSpeed = 1.5f;
        public float visibility = 0.5f;

        public bool diagonalRotation = true;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Sinking";
        public string healthLabel_Beached = "Beached";

        public ShipPermissions moveable = ShipPermissions.DriverNeeded;
        public ShipType shipPowerType = ShipType.Sails;
        public ShipCategory shipCategory = ShipCategory.Misc;
        public TechLevel shipTech = TechLevel.Medieval;

        public RiverDef riverTraversability;
        public List<ShipRole> roles  = new List<ShipRole>();
        public SoundDef soundWhileMoving;
        public ThingDef buildDef;
    }
}