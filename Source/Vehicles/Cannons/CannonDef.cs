using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{
    public class CannonDef : Def
    {
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        
        /// <summary>
        /// Fields relating to motes upon cannon firing
        /// </summary>
        public ThingDef moteCannon;
        public ThingDef moteFlash;
        public float moteSpeedThrown = 2;

        /// <summary>
        /// Fields relating to ammo
        /// </summary>
        public List<ThingDef> ammoAllowed = new List<ThingDef>();
        public int magazineCapacity = 1;
        public int numberCannons = 1;
        public bool genericAmmo = false;

        /// <summary>
        /// All fields related to gizmo or cannon related textures
        /// baseCannonTexPath is for base plate only (static texture below cannon that represents the floor or attaching point of the cannon)
        /// </summary>
        public GraphicData graphicData;

        public string baseCannonTexPath;

        public string gizmoDescription;
        public string gizmoIconTexPath;
        public float gizmoIconScale = 1f;

        public bool matchParentColor = true;

        /// <summary>
        /// Fields relating to Boats specific broadside style cannons
        /// </summary>
        public List<float> centerPoints = new List<float>();
        public List<int> cannonsPerPoint = new List<int>();
        public float spacing = 0f;
        public float offset = 0f;
        public bool splitCannonGroups = false;

        /// <summary>
        /// Fields relating to targeting, firing, or reloading
        /// </summary>
        public List<FireMode> fireModes = new List<FireMode>();
        public bool autoSnapTargeting = false;
        public float rotationSpeed = 1f;
        public float spreadRadius = 0f;
        public float maxRange = -1f;
        public float minRange = 0f;
        public float reloadTimer = 5;
        public float warmUpTimer = 3;

        public SoundDef cannonSound;
        public SoundDef reloadSound;
        
        /// <summary>
        /// Fields relating to projectile
        /// </summary>
        public ThingDef projectile;
        public ProjectileHitFlags hitFlags = ProjectileHitFlags.All;
        public float projectileOffset = 0f;
        public List<float> projectileShifting = new List<float>();
    }
}
