using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.Build;
using RimShips.Defs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.AI
{
    public static class MapExtensionUtility
    {
        public static MapExtension GetExtensionToMap(Map map)
        {
            NullListCheck();
            MapExtension result = mapExtensions.Find(x => map.uniqueID == x.MapExtensionID);
            if(result is null)
            {
                result = new MapExtension(map);
                result.ConstructComponents();
                result.StoreMapExtension();
            }
            return result;
        }

        public static void StoreMapExtension(this MapExtension mapE)
        {
            NullListCheck();
            mapExtensions.Add(mapE);
        }

        private static void NullListCheck()
        {
            if (mapExtensions is null)  mapExtensions = new List<MapExtension>();
        }

        public static void RemoveMapExtension(Map map)
        {
            NullListCheck();
            MapExtension extToRemove = mapExtensions.Find(x => map.uniqueID == x.MapExtensionID);
            if (extToRemove is null)
            {
                Log.Warning("Unable to find MapExtension for Map " + map.uniqueID + ". Error with mod RimShips");
                return;
            }
            mapExtensions.Remove(extToRemove);
        }

        public static void ClearMapExtensions()
        {
            if (mapExtensions is null) mapExtensions = new List<MapExtension>();
            mapExtensions.Clear();
        }

        private static List<MapExtension> mapExtensions;
    }
}
