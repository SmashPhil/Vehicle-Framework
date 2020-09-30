using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
    /// <summary>
    /// Cache Map, World, and Game components to turn O(n) operation to O(1) if comp exists
    /// Worst Case Scenario: Comp doesn't exist or isn't yet cached and is O(n) for retrieval
    /// </summary>
    public static class ComponentCache
    {
        /// <summary>
        /// Cache Retrieval for MapComponents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="map"></param>
        /// <returns></returns>
        public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
        {
            if(cachedMapComps.TryGetValue(map, out var mapCache))
            {
                if (mapCache.TryGetValue(typeof(T), out MapComponent mapComp))
                {
                    return (T)mapComp;
                }
                T mapComp2 = map.GetComponent<T>();
                if (mapComp2 is null)
                {
                    return default;
                }
                mapCache.Add(typeof(T), mapComp2);
                return (T)mapCache[typeof(T)];
            }
            cachedMapComps.Add(map, new Dictionary<Type, MapComponent>());
            T comp = map.GetComponent<T>();
            return comp;
        }

        /// <summary>
        /// Cache Retrieval for WorldComponents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="world"></param>
        /// <returns></returns>
        public static T GetCachedWorldComponent<T>(this World world) where T : WorldComponent
        {
            if (cachedWorldComps.TryGetValue(typeof(T), out WorldComponent compMatch))
            {
                return (T)compMatch;
            }
            T comp = world.GetComponent<T>();
            if (comp is null)
            {
                return default;
            }
            cachedWorldComps.Add(typeof(T), comp);
            return (T)cachedWorldComps[typeof(T)];
        }

        /// <summary>
        /// Cache Retrieval for GameComponents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="game"></param>
        /// <returns></returns>
        public static T GetCachedGameComponent<T>(this Game game) where T : GameComponent
        {
            if (cachedGameComps.TryGetValue(typeof(T), out GameComponent compMatch))
            {
                return (T)compMatch;
            }
            T comp = game.GetComponent<T>();
            if (comp is null)
            {
                return default;
            }
            cachedGameComps.Add(typeof(T), comp);
            return (T)cachedGameComps[typeof(T)];
        }

        public static Dictionary<Map, Dictionary<Type, MapComponent>> cachedMapComps = new Dictionary<Map, Dictionary<Type, MapComponent>>();

        public static Dictionary<Type, WorldComponent> cachedWorldComps = new Dictionary<Type, WorldComponent>();

        public static Dictionary<Type, GameComponent> cachedGameComps = new Dictionary<Type, GameComponent>();
    }
}
