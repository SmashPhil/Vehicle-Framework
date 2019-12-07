using System.Collections.Generic;
using Verse;

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

        public static void ClearMapExtensions()
        {
            if(mapExtensions is null) mapExtensions = new Dictionary<Map, MapExtension>();
            mapExtensions.Clear();
        }

        private static Dictionary<Map, MapExtension> mapExtensions = new Dictionary<Map, MapExtension>();
    }
}
