using System.Collections.Generic;
using Verse;

namespace SmashTools;

public static class MapComponentCache<T> where T : MapComponent
{
  // When 1 map is loaded or the same map is rapidly pulling the same component, doing an id comp will
  // net ~2x faster lookups on average. The tradeoff is worth it compared to a flat dictionary lookup.
  private static T recentAccess;
  private static readonly Dictionary<int, T> MapComps = [];

  public static T GetComponent(Map map)
  {
    if (map.Disposed)
      return null;
    if (recentAccess != null && recentAccess.map.uniqueID == map.uniqueID)
      return recentAccess;

    if (!MapComps.TryGetValue(map.uniqueID, out T component))
    {
      component = map.GetComponent<T>();
      MapComps[map.uniqueID] = component;
    }
    recentAccess = component;
    return component;
  }

  public static void ClearMap(Map map)
  {
    recentAccess = null;
    MapComps.Remove(map.uniqueID);
  }

  public static void ClearAll()
  {
    recentAccess = null;
    MapComps.Clear();
  }

  public static int ClearAllDisposed()
  {
    recentAccess = null;
    List<int> entriesToRemove = [];
    foreach ((int id, T component) in MapComps)
    {
      if (component?.map is null or { Disposed: true })
        entriesToRemove.Add(id);
    }
    foreach (int id in entriesToRemove)
      MapComps.Remove(id);
    return entriesToRemove.Count;
  }

  internal static T GetComponent(int mapId)
  {
    return MapComps.TryGetValue(mapId);
  }

  internal static int Count()
  {
    return MapComps.Values.CountWhere(item => item is not null);
  }
}