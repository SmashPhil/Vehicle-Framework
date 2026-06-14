using System.Collections.Generic;
using Verse;

namespace Vehicles.Config;

public sealed class FeatureFlags
{
  public const string Raiders = "Raiders";
  public const string Paratroopers = "Paratroopers";
  public const string Fishing = "Fishing";
  public const string TradeableVehicles = "TradeableVehicles";
  public const string VehicleCaravanProps = "VehicleCaravanProps";
  public const string BetterAutoLoadConfig = "BetterAutoLoadConfig";
  public const string Acceleration = "Acceleration";

  public const string PathFinderV2 = "PathFinderV2";
  public const string BurstLib = "BurstLib";

  public const string BlitTexturePortraits = "BlitTexturePortraits";

  private readonly List<IFeatureFlag> features;

  internal FeatureFlags(List<IFeatureFlag> flags)
  {
    features = flags;
  }

  public static FeatureFlags Default => VehicleMod.mod.features;

  public static bool RaidersEnabled => Default.IsEnabled(Raiders) && VehicleMod.settings.debug.debugAllowRaiders;

  public static bool FishingEnabled => Default.IsEnabled(Fishing);

  internal static FeatureFlags InitDefault()
  {
    FeatureFlags flags = new([
      Feature.Create(Raiders, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(Paratroopers, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(Fishing, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(TradeableVehicles, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(Acceleration, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(BurstLib, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(PathFinderV2, Build.Configuration.Debug, Build.Configuration.Unstable),
      Feature.Create(BlitTexturePortraits, Build.Configuration.Debug, Build.Configuration.Unstable)
    ]);
    return flags;
  }

  public bool IsEnabled(string featureName)
  {
    if (features.NullOrEmpty())
      return false;

    foreach (IFeatureFlag feature in features)
    {
      if (feature.Name == featureName)
        return feature.Enabled;
    }
    return false;
  }

  public static bool IsFeatureEnabled(string featureName)
  {
    return Default.IsEnabled(featureName);
  }

  private class Feature : IFeatureFlag
  {
    private string name;

    private readonly HashSet<Build.Configuration> enabledFor = [];

    string IFeatureFlag.Name => name;

    bool IFeatureFlag.Enabled => enabledFor.Contains(Build.Config);

    public static Feature Create(string name, params Build.Configuration[] config)
    {
      Feature feature = new()
      {
        name = name
      };
      if (!config.NullOrEmpty())
      {
        feature.enabledFor.AddRange(config);
      }
      return feature;
    }
  }
}