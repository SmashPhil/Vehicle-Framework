using System;
using System.Reflection;
using System.Xml;
using System.Collections.Generic;
using Verse;
using SmashTools;
using SmashTools.Xml;

namespace Vehicles
{
	[LoadedEarly]
	[StaticConstructorOnModInit]
	public static class ParsingHelper
	{
		/// <summary>
		/// VehicleDef, HashSet of fields
		/// </summary>
		public static readonly Dictionary<string, HashSet<FieldInfo>> lockedFields = new Dictionary<string, HashSet<FieldInfo>>();
		/// <summary>
		/// VehicleDef, XmlNodes
		/// </summary>
		public static readonly Dictionary<string, HashSet<string>> overriddenVehicleNodes = new Dictionary<string, HashSet<string>>();
		/// <summary>
		/// VehicleDef, (fieldName, defaultValue)
		/// </summary>
		public static readonly Dictionary<string, Dictionary<string, string>> setDefaultValues = new Dictionary<string, Dictionary<string, string>>();

		static ParsingHelper()
		{
			RegisterParsers();
			RegisterAttributes();
		}

		internal static void RegisterParsers()
		{
			ParseHelper.Parsers<VehicleJobLimitations>.Register(new Func<string, VehicleJobLimitations>(VehicleJobLimitations.FromString));
			ParseHelper.Parsers<CompVehicleLauncher.DeploymentTimer>.Register(new Func<string, CompVehicleLauncher.DeploymentTimer>(CompVehicleLauncher.DeploymentTimer.FromString));
			ParseHelper.Parsers<VehicleTurretRender.RotationalOffset>.Register(new Func<string, VehicleTurretRender.RotationalOffset>(VehicleTurretRender.RotationalOffset.FromString));
			ParseHelper.Parsers<Pair<VehicleEventDef, VehicleEventDef>>.Register(new Func<string, Pair<VehicleEventDef, VehicleEventDef>>(VehicleEventDefPairFromString));
		}

		private static Pair<VehicleEventDef, VehicleEventDef> VehicleEventDefPairFromString(string entry)
		{
			entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
			string[] data = entry.Split(new char[] { ',' });

			try
			{
				VehicleEventDef eventDef1 = DefDatabase<VehicleEventDef>.GetNamed(data[0].Trim());
				VehicleEventDef eventDef2 = DefDatabase<VehicleEventDef>.GetNamed(data[1].Trim());
				return new Pair<VehicleEventDef, VehicleEventDef>(eventDef1, eventDef2);
			}
			catch (Exception ex)
			{
				SmashLog.Error($"{entry} is not a valid <struct>Pair<VehicleEventDef, VehicleEventDef></struct> format. Exception: {ex}");
				return new Pair<VehicleEventDef, VehicleEventDef>();
			}
		}

		internal static void RegisterAttributes()
		{
			XmlParseHelper.RegisterAttribute("LockSetting", CheckFieldLocked);
			XmlParseHelper.RegisterAttribute("AssignDefaults", AssignDefaults);
			XmlParseHelper.RegisterAttribute("DisableSettings", CheckDisabledSettings);
			XmlParseHelper.RegisterAttribute("TurretAllowedFor", CheckTurretStatus);
			XmlParseHelper.RegisterAttribute("AllowTerrainWithTag", AllowTerrainCosts, "customTerrainCosts");
			XmlParseHelper.RegisterAttribute("DisallowTerrainWithTag", DisallowTerrainCosts, "customTerrainCosts");
		}

		private static void CheckFieldLocked(XmlNode node, string value, FieldInfo field)
		{
			if (value.ToUpperInvariant() == "TRUE")
			{
				string defName = XmlParseHelper.BackSearchDefName(node);
				if (string.IsNullOrEmpty(defName))
				{
					SmashLog.Error($"Cannot use <attribute>LockSetting</attribute> on {field.Name} since it is not nested within a Def.");
					return;
				}
				if (!field.HasAttribute<PostToSettingsAttribute>())
				{
					SmashLog.Error($"Cannot use <attribute>LockSetting</attribute> on <field>{field.Name}</field> since related field does not have PostToSettings attribute in <type>{field.DeclaringType}</type>");
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
			string defName = XmlParseHelper.BackSearchDefName(node);
			if (string.IsNullOrEmpty(defName))
			{
				SmashLog.Error($"Cannot use <attribute>AssignAllDefault</attribute> on {field.Name}. This attribute cannot be used in abstract defs.");
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
					SmashLog.Error($"Cannot use <attribute>DisableSetting</attribute> on non-VehicleDef XmlNodes.");
					return;
				}
				string defName = defNode.InnerText;
				VehicleMod.settingsDisabledFor.Add(defName);
			}
		}

		private static void CheckTurretStatus(XmlNode node, string value, FieldInfo field)
		{
			if (value.ToUpperInvariant().Contains("STRAFING"))
			{
				string defName = XmlParseHelper.BackSearchDefName(node);
				if (string.IsNullOrEmpty(defName))
				{
					SmashLog.Error($"Cannot use <attribute>TurretAllowedFor</attribute> on non-VehicleDef XmlNodes.");
					return;
				}
				foreach (XmlNode childNode in node.ChildNodes)
				{
					XmlNode keyNode = childNode.SelectSingleNode("key");
					if (keyNode is null)
					{
						SmashLog.Error($"Unable to locate <text>key</text> for VehicleTurret.");
					}
					if (!Enum.TryParse(keyNode.InnerText, out TurretDisableType enableType))
					{

					}
					VehicleTurret.conditionalTurrets.Add(new Pair<string, TurretDisableType>(defName, enableType));
				}
			}
		}

		private static void AllowTerrainCosts(XmlNode node, string value, FieldInfo field)
		{
			string defName = XmlParseHelper.BackSearchDefName(node);
			if (string.IsNullOrEmpty(defName))
			{
				SmashLog.Error($"Could not find <xml>defName</xml> node for {node.Name}.");
				return;
			}
			int pathCost = 1;
			if (node.Attributes["PathCost"] is XmlAttribute pathCostAttribute)
			{
				if (!int.TryParse(pathCostAttribute.Value, out pathCost))
				{
					SmashLog.Warning($"Unable to parse <attribute>PathCost</attribute> attribute for {defName}");
					pathCost = 1;
				}
			}
			if (!PathingHelper.allTerrainCostsByTag.TryGetValue(defName, out var terrainDict))
			{
				terrainDict = new Dictionary<string, int>();
				PathingHelper.allTerrainCostsByTag[defName] = terrainDict;
			}
			terrainDict[value] = pathCost;
		}

		private static void DisallowTerrainCosts(XmlNode node, string value, FieldInfo field)
		{
			string defName = XmlParseHelper.BackSearchDefName(node);
			if (string.IsNullOrEmpty(defName))
			{
				SmashLog.Error($"Could not find <xml>defName</xml> node for {node.Name}.");
				return;
			}
			if (!PathingHelper.allTerrainCostsByTag.TryGetValue(defName, out var terrainDict))
			{
				terrainDict = new Dictionary<string, int>();
				PathingHelper.allTerrainCostsByTag[defName] = terrainDict;
			}
			terrainDict[value] = VehiclePathGrid.ImpassableCost;
		}

		private static void MarkAsOverride(XmlNode node, string value, FieldInfo field)
		{
			if (value.EqualsIgnoreCase("true"))
			{
				string defName = XmlParseHelper.BackSearchDefName(node);
				if (string.IsNullOrEmpty(defName))
				{
					SmashLog.Error($"Could not find <xml>defName</xml> node for {node.Name}.");
					return;
				}
				overriddenVehicleNodes.AddOrInsert(defName, node.Name);
			}
		}
	}
}
