using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles.AI
{
    public static class MapExtensionUtility
    {
        [Obsolete("MapExtension has been reimplemented as a MapComponent. Utility Method only preexisting method calls. Use map.GetComponent<MapExtension>() instead.")]
        public static MapExtension GetExtensionToMap(this Map map)
        {
            return map.GetComponent<MapExtension>();;
        }
    }
}
