using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.Jobs;
using RimShips.UI;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.AI
{
    public static class GenGridShips
    {
        public static bool InNoBuildEdgeArea(this IntVec3 c, Map map)
        {
            return c.CloseToEdge(map, 10);
        }

        public static bool InNoZoneEdgeArea(this IntVec3 c, Map map)
        {
            return c.CloseToEdge(map, 5);
        }

        public static bool CloseToEdge(this IntVec3 c, Map map, int edgeDist)
        {
            IntVec3 size = map.Size;
            return c.x < edgeDist || c.z < edgeDist || c.x >= size.x - edgeDist || c.z >= size.z - edgeDist;
        }

        public static bool OnEdge(this IntVec3 c, Map map)
        {
            IntVec3 size = map.Size;
            return c.x == 0 || c.x == size.x - 1 || c.z == 0 || c.z == size.z - 1;
        }

        public static bool OnEdge(this IntVec3 c, Map map, Rot4 dir)
        {
            if (dir == Rot4.North)
            {
                return c.z == 0;
            }
            if (dir == Rot4.South)
            {
                return c.z == map.Size.z - 1;
            }
            if (dir == Rot4.West)
            {
                return c.x == 0;
            }
            if (dir == Rot4.East)
            {
                return c.x == map.Size.x - 1;
            }
            Log.ErrorOnce("Invalid edge direction", 55370769, false);
            return false;
        }

        public static bool InBounds(this IntVec3 c, Map map)
        {
            IntVec3 size = map.Size;
            return (ulong)c.x < (ulong)((long)size.x) && (ulong)c.z < (ulong)((long)size.z);
        }

        public static bool InBounds(this Vector3 v, Map map)
        {
            IntVec3 size = map.Size;
            return v.x >= 0f && v.z >= 0f && v.x < (float)size.x && v.z < (float)size.z;
        }

        public static bool Walkable(this IntVec3 c, MapExtension mapE)
        {
            return mapE.getShipPathGrid.Walkable(c);
        }

        public static bool Standable(this IntVec3 c, Map map, MapExtension mapE)
        {
            if(!mapE.getShipPathGrid.Walkable(c))
            {
                return false;
            }
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            foreach(Thing t in list)
            {
                if(t.def.passability != Traversability.Standable)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Impassable(this IntVec3 c, Map map)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            foreach(Thing t in list)
            {
                if(t.def.passability is Traversability.Impassable)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool SupportsStructureType(this IntVec3 c, Map map, TerrainAffordanceDef surfaceType)
        {
            return c.GetTerrain(map).affordances.Contains(surfaceType);
        }

        public static bool CanBeSeenOver(this IntVec3 c, Map map)
        {
            if (!c.InBounds(map))
            {
                return false;
            }
            Building edifice = c.GetEdifice(map);
            return edifice == null || edifice.CanBeSeenOver();
        }

        public static bool CanBeSeenOverFast(this IntVec3 c, Map map)
        {
            Building edifice = c.GetEdifice(map);
            return edifice == null || edifice.CanBeSeenOver();
        }

        public static bool CanBeSeenOver(this Building b)
        {
            if (b.def.Fillage == FillCategory.Full)
            {
                Building_Door building_Door = b as Building_Door;
                return building_Door != null && building_Door.Open;
            }
            return true;
        }

        public static SurfaceType GetSurfaceType(this IntVec3 c, Map map)
        {
            if(!c.InBounds(map))
            {
                return SurfaceType.None;
            }
            List<Thing> list = c.GetThingList(map);
            foreach(Thing t in list)
            {
                if(t.def.surfaceType != SurfaceType.None)
                {
                    return t.def.surfaceType;
                }
            }
            return SurfaceType.None;
        }

        public static bool HasEatSurface(this IntVec3 c, Map map)
        {
            return c.GetSurfaceType(map) == SurfaceType.Eat;
        }

        //Necessary?
        public const int NoBuildEdgeWidth = 10;
        public const int NoZoneEdgeWidth = 5;
    }
}
