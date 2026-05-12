using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles;

public class MapGridOwners : GridOwnerList<MapGridOwners.PathConfig>
{
  private readonly IPathingManager pathing;

  public MapGridOwners(IPathingManager pathing, List<VehicleDef> vehicleDefs) :
    base(vehicleDefs)
  {
    this.pathing = pathing;
  }

  protected override bool CanTransferOwnershipTo(VehicleDef vehicleDef)
  {
    return pathing.GetPathGrid(vehicleDef).Enabled;
  }

  // Accessed from Init, already locked for the duration of owner generation
  protected override void GenerateConfigs()
  {
    configs = new PathConfig[vehicleDefs.Count];
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      configs[vehicleDef.DefIndex] = new PathConfig(vehicleDef);
    }
  }

  public readonly struct PathConfig : IPathConfig
  {
    private readonly VehicleDef vehicleDef;

    private readonly HashSet<ThingDef> impassableThingDefs;
    private readonly HashSet<TerrainDef> impassableTerrain;
    private readonly int size;

    private readonly DefaultImpassable defaultMapImpassable;

    internal PathConfig(VehicleDef vehicleDef)
    {
      const DefaultImpassable BitMaskMap = DefaultImpassable.Terrain | DefaultImpassable.Things;

      this.vehicleDef = vehicleDef;
      size = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z);

      defaultMapImpassable = vehicleDef.properties.defaultImpassable & BitMaskMap;
      impassableThingDefs = vehicleDef.properties.customThingCosts
       .Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key)
       .ToHashSet();
      impassableTerrain = vehicleDef.properties.customTerrainCosts
       .Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key)
       .ToHashSet();
    }

    bool IPathConfig.UsesRegions =>
      !Mathf.Approximately(vehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed), 0);

    bool IPathConfig.MatchesReachability(IPathConfig other)
    {
      if (other is not PathConfig pathConfig)
        return false;

      return size == pathConfig.size &&
        defaultMapImpassable == pathConfig.defaultMapImpassable &&
        impassableThingDefs.SetEquals(pathConfig.impassableThingDefs) &&
        impassableTerrain.SetEquals(pathConfig.impassableTerrain);
    }
  }
}