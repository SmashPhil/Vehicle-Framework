using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public static class RoadCostHelper
{
  [Obsolete("Use GetRoadMovementMultiplier instead")]
  public static float GetRoadMovementDifficultyMultiplier(List<VehiclePawn> vehicles,
    int fromTile, int toTile, StringBuilder explanation = null)
  {
    List<SurfaceTile.RoadLink> roads = Find.WorldGrid.Surface[fromTile].Roads;
    if (roads == null)
    {
      return MaxRoadMultiplier(vehicles, VehicleOffRoadMultiplier);
    }
    if (toTile == -1)
    {
      toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
    }
    for (int i = 0; i < roads.Count; i++)
    {
      if (roads[i].neighbor == toTile)
      {
        float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicles, roads[i].road);

        if (explanation != null)
        {
          if (explanation.Length > 0)
          {
            explanation.AppendLine();
          }
          explanation.Append($"{roads[i].road.LabelCap}: {roadMultiplier.ToStringPercent()}");
        }
        return roadMultiplier;
      }
    }
    return 1f;
  }

  [Obsolete("Use GetRoadMovementMultiplier instead")]
  public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs,
    int fromTile, int toTile, StringBuilder explanation = null)
  {
    List<SurfaceTile.RoadLink> roads = Find.WorldGrid.Surface[fromTile].Roads;
    if (roads == null)
    {
      float offRoadMultiplier = MaxRoadMultiplier(vehicleDefs, VehicleDefOffRoadMultiplier);
      // road multiplier is multiplicative against the cost to move, so bonus speed % is 1 - multiplier.
      // e.g. moveCost * 0.8 = 20% bonus to speed
      explanation?.Append("VF_MultiplierFromOffRoad".Translate((1 - offRoadMultiplier).ToStringPercent()));
      return offRoadMultiplier;
    }
    if (toTile == -1)
    {
      toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
    }
    for (int i = 0; i < roads.Count; i++)
    {
      if (roads[i].neighbor == toTile)
      {
        float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicleDefs, roads[i].road);
        explanation?.AppendInNewLine("VF_MultiplierFromRoad".Translate((1 - roadMultiplier).ToStringPercent()));
        return roadMultiplier;
      }
    }
    return 1f;
  }

  [Obsolete]
  private static float MaxRoadMultiplier<T>(List<T> vehicles, Func<T, float> selector)
  {
    float maxValue = float.MinValue;
    foreach (T vehicle in vehicles)
    {
      float value = selector(vehicle);
      if (value > maxValue)
      {
        maxValue = value;
      }
    }
    return Mathf.Clamp(maxValue, 0.01f, 100);
  }

  public static RoadMultiplier GetRoadMovementMultiplier(List<VehiclePawn> vehicles, int fromTile, int toTile)
  {
    List<SurfaceTile.RoadLink> roads = Find.WorldGrid.Surface[fromTile].Roads;
    if (roads == null)
    {
      return RoadMultiplier.OffRoad(MaxOffRoadMultiplier(vehicles));
    }
    if (toTile == -1)
    {
      toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
    }
    foreach (SurfaceTile.RoadLink roadLink in roads)
    {
      if (roadLink.neighbor != toTile)
        continue;

      float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicles, roadLink.road);
      return new RoadMultiplier(roadLink.road, roadMultiplier);
    }
    return RoadMultiplier.Default;

    static float MaxOffRoadMultiplier(List<VehiclePawn> vehicles)
    {
      float maxValue = float.MinValue;
      foreach (VehiclePawn vehicle in vehicles)
      {
        float value = VehicleOffRoadMultiplier(vehicle);
        if (value > maxValue)
        {
          maxValue = value;
        }
      }
      return Mathf.Clamp(maxValue, 0.01f, 100);
    }
  }

  public static RoadMultiplier GetRoadMovementMultiplier(List<VehicleDef> vehicleDefs, int fromTile, int toTile)
  {
    List<SurfaceTile.RoadLink> roads = Find.WorldGrid.Surface[fromTile].Roads;
    if (roads == null)
    {
      return RoadMultiplier.OffRoad(MaxOffRoadMultiplier(vehicleDefs));
    }
    if (toTile == -1)
    {
      toTile = Find.WorldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
    }
    foreach (SurfaceTile.RoadLink roadLink in roads)
    {
      if (roadLink.neighbor != toTile)
        continue;

      float roadMultiplier = GetRoadMovementDifficultyMultiplier(vehicleDefs, roadLink.road);
      return new RoadMultiplier(roadLink.road, roadMultiplier);
    }
    return RoadMultiplier.Default;

    static float MaxOffRoadMultiplier(List<VehicleDef> vehicleDefs)
    {
      float maxValue = float.MinValue;
      foreach (VehicleDef vehicleDef in vehicleDefs)
      {
        float value = VehicleDefOffRoadMultiplier(vehicleDef);
        if (value > maxValue)
        {
          maxValue = value;
        }
      }
      return Mathf.Clamp(maxValue, 0.01f, 100);
    }
  }

  public static float VehicleOffRoadMultiplier(VehiclePawn vehicle)
  {
    float offRoadMultiplier = VehicleDefOffRoadMultiplier(vehicle.VehicleDef);
    offRoadMultiplier =
      vehicle.statHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.OffRoadMultiplier,
        offRoadMultiplier);
    return Mathf.Clamp(offRoadMultiplier, 0.01f, 10);
  }

  public static float VehicleDefOffRoadMultiplier(VehicleDef vehicleDef)
  {
    return SettingsCache.TryGetValue(vehicleDef, typeof(VehicleProperties),
      nameof(VehicleProperties.offRoadMultiplier), vehicleDef.properties.offRoadMultiplier);
  }

  public static float GetRoadMovementDifficultyMultiplier(List<VehiclePawn> vehicles, RoadDef roadDef)
  {
    float roadMultiplier = roadDef.movementCostMultiplier;
    bool customRoadCosts = false;
    foreach (VehiclePawn vehicle in vehicles)
    {
      if (vehicle.VehicleDef.properties.customRoadCosts.TryGetValue(roadDef,
            out float movementCostMultiplier) &&
          (!customRoadCosts || movementCostMultiplier < roadMultiplier))
      {
        customRoadCosts = true;
        roadMultiplier = movementCostMultiplier;
      }
    }
    return roadMultiplier;
  }

  public static float GetRoadMovementDifficultyMultiplier(List<VehicleDef> vehicleDefs, RoadDef roadDef)
  {
    float roadMultiplier = roadDef.movementCostMultiplier;
    bool customRoadCosts = false;
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      if (vehicleDef.properties.customRoadCosts.TryGetValue(roadDef,
          out float movementCostMultiplier) &&
        (!customRoadCosts || movementCostMultiplier < roadMultiplier))
      {
        customRoadCosts = true;
        roadMultiplier = movementCostMultiplier;
      }
    }
    return roadMultiplier;
  }

  public readonly struct RoadMultiplier
  {
    public readonly RoadDef roadDef;
    public readonly float multiplier;

    public RoadMultiplier(RoadDef roadDef, float multiplier)
    {
      this.roadDef = roadDef;
      this.multiplier = multiplier;
    }

    public static RoadMultiplier OffRoad(float multiplier)
    {
      return new RoadMultiplier(null, multiplier);
    }

    public bool HasMultiplier => !Mathf.Approximately(multiplier, 1);

    public string Explanation
    {
      get
      {
        if (!HasMultiplier)
          return null;

        return roadDef != null ?
          "VF_MultiplierFromRoad".Translate((1 - multiplier).ToStringPercent()) :
          "VF_MultiplierFromOffRoad".Translate((1 - multiplier).ToStringPercent());
      }
    }

    public static RoadMultiplier Default => new(roadDef: null, multiplier: 1);
  }
}