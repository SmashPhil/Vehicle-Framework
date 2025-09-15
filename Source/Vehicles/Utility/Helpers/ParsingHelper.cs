using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using SmashTools;
using SmashTools.Xml;
using Vehicles.Config;
using Verse;

namespace Vehicles;

[StaticConstructorOnModInit]
public static class ParsingHelper
{
	/// <summary>
	/// VehicleDef, HashSet of fields
	/// </summary>
	internal static readonly Dictionary<string, HashSet<FieldInfo>> LockedFields = [];

	/// <summary>
	/// VehicleDef, (fieldName, defaultValue)
	/// </summary>
	internal static readonly Dictionary<string, Dictionary<string, string>> SetDefaultValues = [];

	static ParsingHelper()
	{
		RegisterParsers();
		RegisterAttributes();
	}

	private static void RegisterParsers()
	{
		ParseHelper.Parsers<VehicleJobLimitations>.Register(VehicleJobLimitations.FromString);
		ParseHelper.Parsers<CompVehicleLauncher.DeploymentTimer>.Register(CompVehicleLauncher
		 .DeploymentTimer.FromString);
		ParseHelper.Parsers<Pair<VehicleEventDef, VehicleEventDef>>.Register(
			VehicleEventDefPairFromString);
	}

	private static Pair<VehicleEventDef, VehicleEventDef> VehicleEventDefPairFromString(
		string entry)
	{
		entry = entry.TrimStart(['(']).TrimEnd([')']);
		string[] data = entry.Split([',']);

		try
		{
			VehicleEventDef eventDef1 = DefDatabase<VehicleEventDef>.GetNamed(data[0].Trim());
			VehicleEventDef eventDef2 = DefDatabase<VehicleEventDef>.GetNamed(data[1].Trim());
			return new Pair<VehicleEventDef, VehicleEventDef>(eventDef1, eventDef2);
		}
		catch (Exception ex)
		{
			Log.Error(
				$"{entry} is not a valid Pair<VehicleEventDef, VehicleEventDef> format. Exception: {ex}");
			return new Pair<VehicleEventDef, VehicleEventDef>();
		}
	}

	private static void RegisterAttributes()
	{
		XmlParseHelper.RegisterPreProcessor("FeatureFlag", CheckFeatureFlag);

		XmlParseHelper.RegisterAttribute("LockSetting", CheckFieldLocked);
		XmlParseHelper.RegisterAttribute("AssignDefaults", AssignDefaults);
		XmlParseHelper.RegisterAttribute("DisableSettings", CheckDisabledSettings);
		XmlParseHelper.RegisterAttribute("AllowTerrainWithTag", AllowTerrainCosts,
			nameof(VehicleProperties.customTerrainCosts));
		XmlParseHelper.RegisterAttribute("DisallowTerrainWithTag", DisallowTerrainCosts,
			nameof(VehicleProperties.customTerrainCosts));
	}

	private static bool CheckFeatureFlag(XmlNode node, string value, FieldInfo field)
	{
		if (value.NullOrEmpty())
			return true;

		return FeatureFlags.IsEnabled(value);
	}

	private static void CheckFieldLocked(XmlNode node, string value, FieldInfo field)
	{
		if (value.ToUpperInvariant() == "TRUE")
		{
			string defName = BackSearchDefName(node);
			if (string.IsNullOrEmpty(defName))
			{
				Log.Error(
					$"Cannot use LockSetting on {field.Name} since it is not nested within a Def.");
				return;
			}
			if (!field.HasAttribute<PostToSettingsAttribute>())
			{
				Log.Error(
					$"Cannot use LockSetting on {field.Name} since related field does not have PostToSettings attribute in {field.DeclaringType}");
			}
			if (!LockedFields.ContainsKey(defName))
			{
				LockedFields.Add(defName, new HashSet<FieldInfo>());
			}
			LockedFields[defName].Add(field);
		}
	}

	private static void AssignDefaults(XmlNode node, string value, FieldInfo field)
	{
		string defName = BackSearchDefName(node);
		if (string.IsNullOrEmpty(defName))
		{
			Log.Error(
				$"Cannot use AssignAllDefault on {field.Name}. This attribute cannot be used in abstract defs.");
			return;
		}
		if (!SetDefaultValues.ContainsKey(defName))
		{
			SetDefaultValues.Add(defName, new Dictionary<string, string>());
		}
		SetDefaultValues[defName][node.Name] = value;
	}

	private static void CheckDisabledSettings(XmlNode node, string value, FieldInfo field)
	{
		if (value.ToUpperInvariant() == "TRUE")
		{
			XmlNode defNode = node.SelectSingleNode("defName");
			if (defNode is null)
			{
				Log.Error(
					"Cannot use DisableSetting on non-VehicleDef XmlNodes.");
				return;
			}
			string defName = defNode.InnerText;
			VehicleMod.SettingsDisabledFor.Add(defName);
		}
	}

	private static void AllowTerrainCosts(XmlNode node, string value, FieldInfo field)
	{
		string defName = BackSearchDefName(node);
		if (string.IsNullOrEmpty(defName))
		{
			Log.Error($"Could not find defName node for {node.Name}.");
			return;
		}
		int pathCost = 1;
		if (node.Attributes?["PathCost"] is { } pathCostAttribute)
		{
			if (!int.TryParse(pathCostAttribute.Value, out pathCost))
			{
				Log.Warning($"Unable to parse PathCost attribute for {defName}");
				pathCost = 1;
			}
		}
		if (!PathingHelper.allTerrainCostsByTag.TryGetValue(defName,
			out Dictionary<string, int> terrainDict))
		{
			terrainDict = new Dictionary<string, int>();
			PathingHelper.allTerrainCostsByTag[defName] = terrainDict;
		}
		terrainDict[value] = pathCost;
	}

	private static void DisallowTerrainCosts(XmlNode node, string value, FieldInfo field)
	{
		string defName = BackSearchDefName(node);
		if (string.IsNullOrEmpty(defName))
		{
			Log.Error($"Could not find defName node for {node.Name}.");
			return;
		}
		if (!PathingHelper.allTerrainCostsByTag.TryGetValue(defName,
			out Dictionary<string, int> terrainDict))
		{
			terrainDict = new Dictionary<string, int>();
			PathingHelper.allTerrainCostsByTag[defName] = terrainDict;
		}
		terrainDict[value] = VehiclePathGrid.ImpassableCost;
	}

	/// <summary>
	/// Traverse backwards from the <paramref name="curNode"/> until the defName node is found.
	/// </summary>
	/// <param name="curNode"></param>
	/// <returns>Empty string if not found and the document element is reached</returns>
	private static string BackSearchDefName(XmlNode curNode)
	{
		XmlNode defNode = curNode.SelectSingleNode("defName");
		XmlNode parentNode = curNode;
		while (defNode is null)
		{
			parentNode = parentNode.ParentNode;
			if (parentNode is null)
			{
				return string.Empty;
			}

			defNode = parentNode.SelectSingleNode("defName");
		}

		return defNode.InnerText;
	}
}