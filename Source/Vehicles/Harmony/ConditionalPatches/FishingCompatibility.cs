using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Compatibility;

[PublicAPI]
public static class FishingCompatibility
{
  private static readonly List<Thing> FishingResult = [];
  private static readonly Dictionary<BiomeDef, FishList> FishDefs = [];

  public static bool Active { get; private set; }

  public static bool EnabledFor(VehicleDef vehicleDef)
  {
    if (!Active) return false;

    return SettingsCache.TryGetValue(vehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.canFish),
      vehicleDef.properties.canFish);
  }

  public static bool CanFishAt(VehiclePawn vehicle, IntVec3 cell)
  {
    Assert.IsTrue(vehicle.Spawned);
    BiomeDef biomeDef = vehicle.Map.Biome;
    WaterBodyType waterBodyType = cell.GetWaterBodyType(vehicle.Map);
    return FishDefs.TryGetValue(biomeDef, out FishList fishChances) && !fishChances.IsEmpty(waterBodyType);
  }

  public static void AddFishDef(BiomeDef biomeDef, WaterBodyType waterBodyType, ThingDef thingDef, float commonality,
    float fishYield = 1)
  {
    Active = true;

    if (!FishDefs.TryGetValue(biomeDef, out FishList fishChances))
    {
      FishDefs[biomeDef] = fishChances = new FishList();
    }
    fishChances[waterBodyType, thingDef] = commonality;
    fishChances.SetYield(thingDef, fishYield);
  }

  public static List<Thing> GetCatchesFor(VehiclePawn vehicle, IntVec3 cell, BiomeDef biomeDef, out bool rare)
  {
    if (ModsConfig.OdysseyActive)
    {
      return FishingUtility.GetCatchesFor(vehicle, cell, false, out rare);
    }

    const int MaxYieldSkillLevel = 15;
    const int MaxYieldAmount = 3; // 3x stackLimit

    rare = false;
    WaterBodyType waterBodyType = cell.GetWaterBodyType(vehicle.Map);
    FishingResult.Clear();
    if (!FishDefs.TryGetValue(biomeDef, out FishList fishList) || fishList.IsEmpty(waterBodyType))
    {
      Trace.Fail($"Fishing in biome {biomeDef} which does not have any fish registered.");
      return null;
    }
    FishingProperties fishingProps = vehicle.VehicleDef.fishingProperties;
    ThingDef fishDef = fishList.GetRandomFishDef(waterBodyType);
    int fishingSkillAvg = fishingProps?.animalSkillOverride ?? vehicle.AverageSkillOfCapablePawns(SkillDefOf.Animals);
    float pctYield = Mathf.CeilToInt((float)fishingSkillAvg / MaxYieldSkillLevel);
    Thing fish = ThingMaker.MakeThing(fishDef);
    fish.stackCount = Mathf.Clamp(
      Mathf.RoundToInt(pctYield * fishList.GetYield(fishDef) * VehicleMod.settings.main.fishingMultiplier),
      1, fish.def.stackLimit * MaxYieldAmount);
    FishingResult.Add(fish);
    return FishingResult;
  }

  internal class FishList
  {
    internal const float DefaultYieldModifier = 1;

    private readonly Dictionary<ThingDef, float> yieldModifiers = [];
    private readonly Dictionary<ThingDef, float> fishDefsFreshWater = [];
    private readonly Dictionary<ThingDef, float> fishDefsSaltWater = [];
    private readonly Dictionary<ThingDef, float> fishDefsOther = [];

    public float this[WaterBodyType waterBodyType, ThingDef thingDef]
    {
      get
      {
        return waterBodyType switch
        {
          WaterBodyType.Freshwater => fishDefsFreshWater.TryGetValue(thingDef),
          WaterBodyType.Saltwater => fishDefsSaltWater.TryGetValue(thingDef),
          WaterBodyType.Other => fishDefsOther.TryGetValue(thingDef),
          _ => 0
        };
      }
      set
      {
        switch (waterBodyType)
        {
          case WaterBodyType.Freshwater:
            fishDefsFreshWater[thingDef] = value;
            break;
          case WaterBodyType.Saltwater:
            fishDefsSaltWater[thingDef] = value;
            break;
          case WaterBodyType.Other:
            fishDefsOther[thingDef] = value;
            break;
          case WaterBodyType.None:
            throw new InvalidOperationException("Trying to register fish for water body of type 'None'");
          default:
            throw new NotImplementedException(waterBodyType.ToString());
        }
      }
    }

    public float GetYield(ThingDef fishDef)
    {
      return yieldModifiers.TryGetValue(fishDef, fallback: DefaultYieldModifier);
    }

    public void SetYield(ThingDef fishDef, float modifier)
    {
      if (Mathf.Approximately(modifier, DefaultYieldModifier))
        return;

      yieldModifiers[fishDef] = modifier;
    }

    public bool IsCompletelyEmpty()
    {
      return IsEmpty(WaterBodyType.Freshwater) && IsEmpty(WaterBodyType.Saltwater);
    }

    public bool IsEmpty(WaterBodyType waterBodyType)
    {
      return waterBodyType switch
      {
        WaterBodyType.Freshwater => fishDefsFreshWater.Count == 0,
        WaterBodyType.Saltwater => fishDefsSaltWater.Count == 0,
        _ => true
      };
    }

    public ThingDef GetRandomFishDef(WaterBodyType waterBodyType)
    {
      switch (waterBodyType)
      {
        case WaterBodyType.Freshwater:
          return fishDefsFreshWater.RandomElementByWeightWithFallback(FishWeightSelector).Key;
        case WaterBodyType.Saltwater:
          return fishDefsSaltWater.RandomElementByWeightWithFallback(FishWeightSelector).Key;
        case WaterBodyType.Other:
          Log.ErrorOnce("Fishing in water body type that only exists in Odyssey, but Odyssey isn't loaded.",
            "OdysseyNotLoadedForFishing".GetHashCode());
          break;
        case WaterBodyType.None:
          break;
        default:
          throw new NotImplementedException(nameof(WaterBodyType));
      }
      return null;
    }

    private static float FishWeightSelector(KeyValuePair<ThingDef, float> kvp)
    {
      return kvp.Value;
    }
  }
}