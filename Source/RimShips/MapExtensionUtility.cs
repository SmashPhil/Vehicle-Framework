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
            if (!mapExtensions.ContainsKey(map))
            {
                MapExtension result = new MapExtension(map);
                mapExtensions.Add(map, result);
                mapExtensions[map].ConstructComponents();
            }
            return mapExtensions[map];
        }

        public static void RemoveMapExtension(Map map)
        {
            MapExtension extToRemove = mapExtensions[map];
            if (extToRemove is null)
            {
                Log.Warning("Unable to find MapExtension for Map " + map.uniqueID + ". Error with mod RimShips");
                return;
            }
            mapExtensions.Remove(map);
        }

        public static void ClearMapExtensions()
        {
            if(mapExtensions is null) mapExtensions = new Dictionary<Map, MapExtension>();
            mapExtensions.Clear();
        }

        private static Dictionary<Map, MapExtension> mapExtensions = new Dictionary<Map, MapExtension>();
    }
}
