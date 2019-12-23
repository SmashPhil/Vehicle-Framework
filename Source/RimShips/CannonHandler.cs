using System.Collections.Generic;
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
            this.moteCannon = reference.moteCannon;
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
            this.splitCannonGroups = reference.splitCannonGroups;

            if(splitCannonGroups)
            {
                foreach (float f in reference.centerPoints)
                {
                    this.centerPoints.Add(f);
                }
                foreach (int i in reference.cannonsPerPoint)
                {
                    this.cannonsPerPoint.Add(i);
                }

                if(this.cannonsPerPoint.Count != this.centerPoints.Count || (cannonsPerPoint.Count == 0 && centerPoints.Count == 0))
                {
                    Log.Warning("Could Not initialize cannon groups for " + this.pawn.LabelShort);
                    return;
                }
                int group = 0;
                for (int i = 0; i < numberCannons; i++)
                {
                    if((i+1) > (this.cannonsPerPoint[group] * (group + 1)))
                        group++;
                    cannonGroupDict.Add(i, group);
                    if(ShipHarmony.debug)
                    {
                        Log.Message(string.Concat(new object[]
                        {
                        "Initializing ", pawn.LabelShortCap,
                        " with cannon ", this.label,
                        " with ", cannonsPerPoint[group],
                        " cannons in group: ", group
                        }));
                    }
                }
            }
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
            Scribe_Defs.Look(ref moteCannon, "moteCannon");
            Scribe_Defs.Look(ref projectile, "projectile");
            Scribe_Defs.Look(ref cannonSound, "cannonSound");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref reloading, "reloading");
            Scribe_Values.Look(ref hitFlags, "hitFlags", ProjectileHitFlags.All);

            Scribe_Values.Look(ref splitCannonGroups, "splitCannonGroups");
            Scribe_Collections.Look(ref centerPoints, "centerPoints");
            Scribe_Collections.Look(ref cannonsPerPoint, "cannonsPerPoints");
            Scribe_Collections.Look(ref cannonGroupDict, "cannonGroupDict");
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

        public int CannonGroup(int cannonNumber)
        {
            if(centerPoints.Count == 0 || cannonsPerPoint.Count == 0 || centerPoints.Count != cannonsPerPoint.Count)
            {
                Log.Error("Error in Cannon Group. CenterPoints is 0, CannonsPerPoint is 0, or CannonsPerPoint and CenterPoints do not have same number of entries");
                return 0;
            }
            return cannonGroupDict[cannonNumber];
        }

        public string GetUniqueLoadID()
        {
            return "CannonHandlerGroup_" + uniqueID;
        }

        private Dictionary<int, int> cannonGroupDict = new Dictionary<int, int>();

        public bool reloading;
        public int cooldownTicks;
        public int uniqueID = -1;
        public int MaxTicks => Mathf.CeilToInt(this.cooldownTimer * 60f);

        public string label = "Label Not Set";
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public ThingDef moteCannon;
        public SoundDef cannonSound;

        public List<float> centerPoints = new List<float>();
        public List<int> cannonsPerPoint = new List<int>();

        public Pawn pawn;

        [DefaultValue(ProjectileHitFlags.All)]
        public ProjectileHitFlags hitFlags;

        public bool splitCannonGroups = false;

        public float spreadRadius = 0f;

        public float maxRange = 30f;

        public float minRange = 10f;

        public float cooldownTimer = 5;

        public int numberCannons = 1;

        public float spacing = 0f;

        public float offset = 0f;

        public float projectileOffset = 0f;

        public int baseTicksBetweenShots = 50;
    }
}
