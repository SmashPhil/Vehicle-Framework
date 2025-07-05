using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using SmashTools;
using SmashTools.Xml;
using Verse;

namespace Vehicles;

[StaticConstructorOnModInit]
public static class ParsingHelper
{
  /// <summary>
  /// VehicleDef, HashSet of fields
  /// </summary>
  public static readonly Dictionary<string, HashSet<FieldInfo>> lockedFields = [];

  /// <summary>
  /// VehicleDef, (fieldName, defaultValue)
  /// </summary>
  public static readonly Dictionary<string, Dictionary<string, string>> setDefaultValues = [];

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
      SmashLog.Error(
        $"{entry} is not a valid <struct>Pair<VehicleEventDef, VehicleEventDef></struct> format. Exception: {ex}");
      return new Pair<VehicleEventDef, VehicleEventDef>();
    }
  }

  private static void RegisterAttributes()
  {
    XmlParseHelper.RegisterAttribute("LockSetting", CheckFieldLocked);
    XmlParseHelper.RegisterAttribute("AssignDefaults", AssignDefaults);
    XmlParseHelper.RegisterAttribute("DisableSettings", CheckDisabledSettings);
    XmlParseHelper.RegisterAttribute("AllowTerrainWithTag", AllowTerrainCosts,
      "customTerrainCosts");
    XmlParseHelper.RegisterAttribute("DisallowTerrainWithTag", DisallowTerrainCosts,
      "customTerrainCosts");
  }

  private static void CheckFieldLocked(XmlNode node, string value, FieldInfo field)
  {
    if (value.ToUpperInvariant() == "TRUE")
    {
      string defName = BackSearchDefName(node);
      if (string.IsNullOrEmpty(defName))
      {
        SmashLog.Error(
          $"Cannot use <attribute>LockSetting</attribute> on {field.Name} since it is not nested within a Def.");
        return;
      }
      if (!field.HasAttribute<PostToSettingsAttribute>())
      {
        SmashLog.Error(
          $"Cannot use <attribute>LockSetting</attribute> on <field>{field.Name}</field> since related field does not have PostToSettings attribute in <type>{field.DeclaringType}</type>");
      }
      if (!lockedFields.ContainsKey(defName))
      {
        lockedFields.Add(defName, new HashSet<FieldInfo>());
      }
      lockedFields[defName].Add(field);
    }
  }

  private static void AssignDefaults(XmlNode node, string value, FieldInfo field)
  {
    string defName = BackSearchDefName(node);
    if (string.IsNullOrEmpty(defName))
    {
      SmashLog.Error(
        $"Cannot use <attribute>AssignAllDefault</attribute> on {field.Name}. This attribute cannot be used in abstract defs.");
      return;
    }
    if (!setDefaultValues.ContainsKey(defName))
    {
      setDefaultValues.Add(defName, new Dictionary<string, string>());
    }
    setDefaultValues[defName][node.Name] = value;
  }

  private static void CheckDisabledSettings(XmlNode node, string value, FieldInfo field)
  {
    if (value.ToUpperInvariant() == "TRUE")
    {
      XmlNode defNode = node.SelectSingleNode("defName");
      if (defNode is null)
      {
        SmashLog.Error(
          "Cannot use <attribute>DisableSetting</attribute> on non-VehicleDef XmlNodes.");
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
      SmashLog.Error($"Could not find <xml>defName</xml> node for {node.Name}.");
      return;
    }
    int pathCost = 1;
    if (node.Attributes?["PathCost"] is { } pathCostAttribute)
    {
      if (!int.TryParse(pathCostAttribute.Value, out pathCost))
      {
        Log.Warning($"Unable to parse <attribute>PathCost</attribute> attribute for {defName}");
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
      SmashLog.Error($"Could not find <xml>defName</xml> node for {node.Name}.");
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