using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;

namespace Vehicles.World;

public class WorldGridOwners : GridOwnerList<WorldGridOwners.PathConfig>
{
  public WorldGridOwners(List<VehicleDef> vehicleDefs) : base(vehicleDefs)
  {
  }

  protected override bool CanTransferOwnershipTo(VehicleDef vehicleDef)
  {
    //WorldVehiclePathGrid.PathGrid pathGrid =
    //  WorldVehiclePathGrid.Instance.pathGrids[vehicleDef.DefIndex];
    //return pathGrid.Enabled;
    throw new NotSupportedException();
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

  public bool MatchingReachability(VehicleDef vehicleDef, VehicleDef otherVehicleDef)
  {
    IPathConfig config = configs[vehicleDef.DefIndex];
    IPathConfig otherConfig = configs[otherVehicleDef.DefIndex];
    return config.MatchesReachability(otherConfig);
  }

  public readonly struct PathConfig : IPathConfig
  {
    private readonly VehicleDef vehicleDef;

    private readonly DefaultImpassable defaultWorldImpassable;
    private readonly SimpleDictionary<BiomeDef, float> customBiomeCosts;
    private readonly SimpleDictionary<Hilliness, float> customHillinessCosts;
    private readonly SimpleDictionary<RiverDef, float> customRiverCosts;

    internal PathConfig(VehicleDef vehicleDef)
    {
      const DefaultImpassable BitMaskWorld =
        DefaultImpassable.Biomes | DefaultImpassable.Rivers | DefaultImpassable.Hilliness;

      this.vehicleDef = vehicleDef;

      defaultWorldImpassable = vehicleDef.properties.defaultImpassable & BitMaskWorld;
      customBiomeCosts = vehicleDef.properties.customBiomeCosts;
      customHillinessCosts = vehicleDef.properties.customHillinessCosts;
      customRiverCosts = vehicleDef.properties.customRiverCosts;
    }

    bool IPathConfig.UsesRegions =>
      !Mathf.Approximately(vehicleDef.GetStatValueAbstract(VehicleStatDefOf.MoveSpeed), 0);

    bool IPathConfig.MatchesReachability(IPathConfig other)
    {
      if (other is not PathConfig pathConfig)
        return false;

      if (defaultWorldImpassable != pathConfig.defaultWorldImpassable)
        return false;
      if (!MatchingValues(customBiomeCosts, pathConfig.customBiomeCosts))
        return false;
      if (!MatchingValues(customHillinessCosts, pathConfig.customHillinessCosts))
        return false;
      if (!MatchingValues(customRiverCosts, pathConfig.customRiverCosts))
        return false;
      return true;

      static bool MatchingValues<T>(SimpleDictionary<T, float> lhs, SimpleDictionary<T, float> rhs)
      {
        // NOTE - We must check both dictionary configurations to avoid missed cases resulting from
        // 1 dictionary containing all the keys of the other plus more.

        foreach ((T key, float cost) in lhs)
        {
          if (!rhs.TryGetValue(key, out float otherCost) ||
            Mathf.Approximately(cost, WorldVehiclePathGrid.ImpassableMovementDifficulty) ==
            Mathf.Approximately(otherCost, WorldVehiclePathGrid.ImpassableMovementDifficulty))
          {
            return false;
          }
        }

        foreach ((T key, float cost) in rhs)
        {
          if (!lhs.TryGetValue(key, out float otherCost) ||
            Mathf.Approximately(cost, WorldVehiclePathGrid.ImpassableMovementDifficulty) ==
            Mathf.Approximately(otherCost, WorldVehiclePathGrid.ImpassableMovementDifficulty))
          {
            return false;
          }
        }

        return true;
      }
    }
  }
}