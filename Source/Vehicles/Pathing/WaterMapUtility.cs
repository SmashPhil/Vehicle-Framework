using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles.AI
{
    public static class WaterMapUtility
    {
        [Obsolete("WaterMap has been reimplemented as a MapComponent. Utility Method kept for preexisting method calls. Use map.GetComponent<WaterMap>() instead.")]
        public static WaterMap GetExtensionToMap(this Map map)
        {
            return map.GetCachedMapComponent<WaterMap>();
        }
    }
}
