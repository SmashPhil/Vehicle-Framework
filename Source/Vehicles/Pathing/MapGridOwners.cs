using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles;

public class MapGridOwners : GridOwnerList<MapGridOwners.PathConfig>
{
  private readonly VehiclePathingSystem mapping;

  public MapGridOwners(VehiclePathingSystem mapping)
  {
    this.mapping = mapping;
  }

  protected override bool CanTransferOwnershipTo(VehicleDef vehicleDef)
  {
    return mapping[vehicleDef].VehiclePathGrid.Enabled;
  }

  // Accessed from Init, already locked for the duration of owner generation
  protected override void GenerateConfigs()
  {
    configs = new PathConfig[DefDatabase<VehicleDef>.DefCount];
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
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
    private readonly bool defaultTerrainImpassable;

    internal PathConfig(VehicleDef vehicleDef)
    {
      this.vehicleDef = vehicleDef;

      size = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z);
      defaultTerrainImpassable = vehicleDef.properties.defaultTerrainImpassable;
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
        defaultTerrainImpassable == pathConfig.defaultTerrainImpassable &&
        impassableThingDefs.SetEquals(pathConfig.impassableThingDefs) &&
        impassableTerrain.SetEquals(pathConfig.impassableTerrain);
    }
  }
}