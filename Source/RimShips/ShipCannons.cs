using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;

namespace RimShips
{
    public enum WeaponType { None, Broadside, Rotatable }
    public enum WeaponLocation { Port, Starboard, Turret }
    public class ShipCannons
    {
        public float Range
        {
            get
            {
                if(this.range == 0) this.range = this.maxRange;
                return this.range;
            }
            set
            {
                this.range = SPExtended.Clamp(value, this.minRange, this.maxRange);
            }
        }

        public string label = "Label Not Set";
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public SoundDef cannonSound;
        private float range;

        [DefaultValue(0f)]
        public float spreadRadius;

        [DefaultValue(30f)]
        public float maxRange;

        [DefaultValue(10f)]
        public float minRange;

        [DefaultValue(1)]
        public int numberCannons;

        [DefaultValue(0)]
        public float spacing;

        [DefaultValue(5)]
        public int damageDealt;

        [DefaultValue(2f)]
        public float explosionRadius;
    }
}
