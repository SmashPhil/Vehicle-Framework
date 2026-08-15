using System.Collections.Generic;
using SmashTools;
using Verse;

namespace Vehicles;

internal class PathGridCalculator : IPathGridCalculator
{
  private const ushort ImpassableCost = VehiclePathGrid.ImpassableCost;

  ushort IPathGridCalculator.PathCostAt(Map map, IntVec3 cell, VehicleDef vehicleDef)
  {
    TerrainDef terrainDef = map.terrainGrid.TerrainAt(cell);
    ushort pathCost = TerrainCost(terrainDef, vehicleDef);
    if (pathCost >= ImpassableCost)
      return ImpassableCost;

    pathCost += ThingCostAt(map, cell, vehicleDef);
    if (pathCost >= ImpassableCost)
      return ImpassableCost;

    pathCost += WeatherCost(map.snowGrid.GetCategory(cell), vehicleDef);
    return pathCost;
  }

  private static ushort ThingCostAt(Map map, IntVec3 cell, VehicleDef vehicleDef)
  {
    ThingGrid thingGrid = map.thingGrid;
    lock (thingGrid)
    {
      List<Thing> thingList = thingGrid.ThingsListAt(cell);
      if (!thingList.NullOrEmpty())
      {
        ushort maxCost = 0;
        foreach (Thing thing in thingList)
        {
          if (thing is null || !thing.Spawned || thing.Destroyed || thing is VehiclePawn)
            continue;

          ushort thingCost = ThingCost(thing, vehicleDef);
          if (thingCost > maxCost)
          {
            maxCost = thingCost;
          }
        }
        return maxCost;
      }
    }
    return 0;
  }

  private static ushort ThingCost(Thing thing, VehicleDef vehicleDef)
  {
    ThingDef thingDef = thing.def;
    if (vehicleDef.properties.customThingCosts.TryGetValue(thingDef,
          out int thingPathCost))
    {
      if (thingPathCost >= ImpassableCost)
      {
        return ImpassableCost;
      }
    }
    else if ((vehicleDef.properties.defaultImpassable & DefaultImpassable.Things) != 0 ||
             thingDef.ImpassableForVehicles())
    {
      return ImpassableCost;
    }
    else
    {
      thingPathCost = thingDef.pathCost;
    }
    return (ushort)thingPathCost;
  }

  internal static ushort TerrainCost(TerrainDef terrainDef, VehicleDef vehicleDef)
  {
    int pathCost = terrainDef.pathCost;
    if (vehicleDef.properties.customTerrainCosts.TryGetValue(terrainDef, out int customPathCost))
    {
      pathCost = customPathCost;
    }
    else if (terrainDef.passability == Traversability.Impassable ||
        (vehicleDef.properties.defaultImpassable & DefaultImpassable.Terrain) != 0)
    {
      return ImpassableCost;
    }

    return (ushort)pathCost;
  }

  internal static ushort WeatherCost(WeatherBuildupCategory weatherCategory, VehicleDef vehicleDef)
  {
    if (!vehicleDef.properties.customWeatherCosts.TryGetValue(weatherCategory, out int weatherPathCost))
    {
      weatherPathCost = WeatherBuildupUtility.MovementTicksAddOn(weatherCategory);
    }
    return (ushort)weatherPathCost.Clamp(0, 450);
  }
}
