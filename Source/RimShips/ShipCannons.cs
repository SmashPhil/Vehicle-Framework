using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using Harmony;

namespace RimShips
{
    public enum WeaponType { None, Broadside, Rotatable }
    public enum WeaponLocation { Port, Starboard, Turret }
    public class ShipCannons
    {
        public ShipCannons()
        {
            this.CooldownTicks = 0;
        }
        public float Range
        {
            get
            {
                if (this.range == 0) this.range = this.maxRange;
                return this.range;
            }
            set
            {
                this.range = SPExtended.Clamp(value, this.minRange, this.maxRange);
            }
        }

        public void DoTick()
        {
            if(this.CooldownTicks > 0)
            {
                CooldownTicks--;
            }
        }

        public bool ActivateTimer()
        {
            if(this.CooldownTicks > 0)
                return false;
            this.CooldownTicks = MaxTicks;
            return true;
        }
        public bool Reloading { get; set; }
        public int CooldownTicks { get; set; }
        public CompShips ship;

        public string label = "Label Not Set";
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public SoundDef cannonSound;
        private float range;
        

        public int MaxTicks => Mathf.CeilToInt(this.cooldownTimer * 60f);

        [DefaultValue(ProjectileHitFlags.All)]
        public ProjectileHitFlags hitFlags;

        [DefaultValue(0f)]
        public float spreadRadius;

        [DefaultValue(30f)]
        public float maxRange;

        [DefaultValue(10f)]
        public float minRange;

        [DefaultValue(5)]
        public float cooldownTimer;

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
