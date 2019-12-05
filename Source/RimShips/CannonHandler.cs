using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace RimShips
{
    public enum WeaponType { None, Broadside, Rotatable }
    public enum WeaponLocation { Port, Starboard, Turret }
    public class CannonHandler : IExposable, ILoadReferenceable
    {
        public CannonHandler()
        {
        }
        public CannonHandler(Pawn pawn, CannonHandler reference)
        {
            this.pawn = pawn;
            uniqueID = Find.UniqueIDsManager.GetNextThingID();

            this.label = reference.label;
            this.weaponType = reference.weaponType;
            this.weaponLocation = reference.weaponLocation;
            this.projectile = reference.projectile;
            this.cannonSound = reference.cannonSound;
            this.numberCannons = reference.numberCannons;
            this.cooldownTimer = reference.cooldownTimer;
            this.baseTicksBetweenShots = reference.baseTicksBetweenShots;
            this.spacing = reference.spacing;
            this.hitFlags = reference.hitFlags;
            this.spreadRadius = reference.spreadRadius;
            this.minRange = reference.minRange;
            this.maxRange = reference.maxRange;
            this.offset = reference.offset;
            this.projectileOffset = reference.projectileOffset;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref spreadRadius, "spreadRadius");
            Scribe_Values.Look(ref maxRange, "maxRange");
            Scribe_Values.Look(ref minRange, "minRange");
            Scribe_Values.Look(ref cooldownTimer, "cooldownTimer");
            Scribe_Values.Look(ref numberCannons, "numberCannons");
            Scribe_Values.Look(ref spacing, "spacing");
            Scribe_Values.Look(ref baseTicksBetweenShots, "baseTicksBetweenShots");
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_Values.Look(ref offset, "offset");
            Scribe_Values.Look(ref projectileOffset, "projectileOffset");
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref weaponType, "weaponType");
            Scribe_Values.Look(ref weaponLocation, "weaponLocation");
            Scribe_Defs.Look(ref projectile, "projectile");
            Scribe_Defs.Look(ref cannonSound, "cannonSound");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref reloading, "reloading");
            Scribe_Values.Look(ref hitFlags, "hitFlags", ProjectileHitFlags.All);
        }

        public bool ActivateTimer()
        {
            if (this.cooldownTicks > 0)
                return false;
            this.cooldownTicks = MaxTicks;
            return true;
        }

        public void DoTick()
        {
            if (this.cooldownTicks > 0 && this.reloading)
            {
                cooldownTicks--;
            }
        }


        public int TicksPerShot
        {
            get
            {
                return baseTicksBetweenShots;
            }
        }

        public string GetUniqueLoadID()
        {
            return "CannonHandlerGroup_" + uniqueID;
        }

        public bool reloading;
        public int cooldownTicks;
        public int uniqueID = -1;
        public int MaxTicks => Mathf.CeilToInt(this.cooldownTimer * 60f);

        public string label = "Label Not Set";
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public SoundDef cannonSound;

        public Pawn pawn;

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

        [DefaultValue(0f)]
        public float spacing;

        [DefaultValue(0f)]
        public float offset;

        [DefaultValue(0f)]
        public float projectileOffset;

        [DefaultValue(50)]
        public int baseTicksBetweenShots;
    }
}
