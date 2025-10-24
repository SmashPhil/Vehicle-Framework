using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools.Xml;
using Verse;

namespace Vehicles.Config;

#pragma warning disable CS8793, CS0649

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
internal class FeatureFlags
{
	public const string Raiders = "Raiders";
	public const string Paratroopers = "Paratroopers";
	public const string Fishing = "Fishing";
	public const string TradeableVehicles = "TradeableVehicles";
	public const string VehicleCaravanProps = "VehicleCaravanProps";
	public const string BetterAutoLoadConfig = "BetterAutoLoadConfig";

	public const string BurstLib = "Burst";

	[UsedImplicitly]
	public List<IFeatureFlag> features;

	public static FeatureFlags Default => VehicleMod.mod.features;

	public static bool RaidersEnabled => Default.IsEnabled(Raiders);

	public static bool ParatroopersEnabled => Default.IsEnabled(Paratroopers);

	public static bool FishingEnabled => Default.IsEnabled(Fishing);

	public static FeatureFlags InitDefault()
	{
		FeatureFlags flags = new()
		{
			features =
			[
				Feature.Create(Raiders, Build.Configuration.Debug, Build.Configuration.Unstable),
				Feature.Create(Paratroopers, Build.Configuration.Debug, Build.Configuration.Unstable),
				Feature.Create(Fishing, Build.Configuration.Debug, Build.Configuration.Unstable),
				Feature.Create(TradeableVehicles, Build.Configuration.Debug, Build.Configuration.Unstable),
			]
		};
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

	public class Feature : IFeatureFlag
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

		public static string Name(Feature feature)
		{
			return feature.name;
		}

		public static void Write(Feature feature)
		{
			XmlExporter.WriteString(string.Join('|', feature.enabledFor));
		}
	}
}