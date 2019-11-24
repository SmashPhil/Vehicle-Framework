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
        public string label = "Label Not Set";
        public WeaponType weaponType;
        public WeaponLocation weaponLocation;
        public ThingDef projectile;
        public float spreadRadius = 0f;
        public float range = 10;
        public int numberCannons = 0;
        public float spacing = 0f;
        public int damageDealt = 0;
        public float explosionRadius = 0f;
        public bool ricochet = false;
    }
}
