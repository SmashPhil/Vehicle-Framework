using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using JetBrains.Annotations;
using SmashTools;
using SmashTools.Xml;
using Verse;

namespace Vehicles.Config;

#pragma warning disable CS8793

[PublicAPI]
public class FeatureFlags : Mod
{
	public const string Raiders = "Raiders";
	public const string Paratroopers = "Paratroopers";
	public const string Fishing = "Fishing";
	public const string VehicleCaravanProps = "VehicleCaravanProps";

	private static ModContentPack mod;
	private static Data data;

	public FeatureFlags(ModContentPack content) : base(content)
	{
		mod = content;
		Load();
	}

	private static string FilePath => Path.Combine(mod.RootDir, "FeatureFlags.xml");

	public static bool RaidersEnabled => IsEnabled(Raiders);

	public static bool ParatroopersEnabled => IsEnabled(Paratroopers);

	public static bool FishingEnabled => IsEnabled(Fishing);

	public static bool IsEnabled(string featureName)
	{
		if (data is null || data.enabled.NullOrEmpty())
			return false;

		foreach (Feature feature in data.enabled)
		{
			if (feature.name == featureName)
				return feature.Enabled;
		}
		return true;
	}

	internal static void Save()
	{
		try
		{
			XmlExporter.StartDocument(FilePath);
			XmlExporter.WriteElement(nameof(FeatureFlags), data);
		}
		catch (IOException ex)
		{
			Log.Error($"Unable to export feature flag data.\nException = {ex}");
		}
		finally
		{
			XmlExporter.Close();
		}
	}

	private static void Load()
	{
		data = DirectXmlLoader.ItemFromXmlFile<Data>(FilePath, resolveCrossRefs: false);
	}

	private class Data : IXmlExport
	{
		#pragma warning disable CS0649

		[UsedImplicitly]
		public List<Feature> enabled;

		void IXmlExport.Export()
		{
			XmlExporter.WriteList(nameof(enabled), enabled, Feature.Write, Feature.Name);
		}
	}

	private record Feature
	{
		public string name;

		private readonly HashSet<Build.Configuration> enabledFor = [];

		public bool Enabled => enabledFor.Contains(Build.Config);

		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			name = xmlRoot.Name;
			string[] configStrs = xmlRoot.InnerText.Split('|');
			foreach (string configStr in configStrs)
			{
				if (!Enum.TryParse(configStr.Trim(), out Build.Configuration config))
				{
					Log.Error($"Unable to parse {configStr} config.");
					continue;
				}
				enabledFor.Add(config);
			}
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