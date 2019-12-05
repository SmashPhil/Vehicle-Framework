using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using Harmony;

namespace RimShips
{
    public enum ShipPermissions {NotAllowed, DriverNeeded, NoDriverNeeded}
    public enum ShipType {Paddles, Sails, Steam, Fuel, Nuclear}

    //public enum RiverTraversability { Creek, River, LargeRiver, HugeRiver, Impassable }
    public class CompProperties_Ships : CompProperties
    {

        public CompProperties_Ships()
        {
            this.compClass = typeof(CompShips);
        }

        [DefaultValue(true)]
        public bool driverRequired;

        [DefaultValue(false)]
        public bool downable;

        [DefaultValue(false)]
        public bool movesWhenDowned;

        [DefaultValue(250f)]
        public float ticksBetweenRepair;

        [DefaultValue(false)]
        public bool nameable;

        public string healthLabel_Healthy = "Operational";
        public string healthLabel_Injured = "Needs Repairs";
        public string healthLabel_Immobile = "Inoperable";
        public string healthLabel_Dead = "Sinking";
        public string healthLabel_Beached = "Beached";

        public ShipPermissions loadCargo = ShipPermissions.NotAllowed;
        public ShipPermissions moveable = ShipPermissions.DriverNeeded;
        public ShipPermissions armament = ShipPermissions.DriverNeeded;
        public ShipType shipPowerType = ShipType.Sails;

        public RiverDef riverTraversability;
        public List<ShipRole> roles  = new List<ShipRole>();
        public SoundDef soundWhileMoving;
        public ThingDef buildDef;
    }
}