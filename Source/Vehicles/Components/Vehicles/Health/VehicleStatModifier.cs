using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;
using SmashTools.Xml;

namespace Vehicles
{
	public class VehicleStatModifier : ICustomSettingsDrawer
	{
		public VehicleStatDef statDef;
		public float value;
		public List<VehicleStatPart> parts;

		public void LoadDataFromXmlCustom(XmlNode xmlNode)
		{
			string statDefName = xmlNode.Name;
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(statDef), statDefName);
			if (xmlNode.FirstChild is XmlText)
			{
				value = ParseHelper.FromString<float>(xmlNode.InnerText);
			}
			else
			{
				ClassLoader.Distribute(xmlNode, this);
				if (!parts.NullOrEmpty())
				{
					foreach (VehicleStatPart statPart in parts)
					{
						DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(statPart, nameof(statPart.statDef), statDefName);
					}
				}
			}
		}

		public void DrawSetting(Listing_Settings lister, VehicleDef vehicleDef, FieldInfo field, string label, string tooltip, string disabledTooltip, bool locked, bool translate)
		{
			if (statDef.modSettingsInfo.IsValid)
			{
				PostToSettingsAttribute.DrawSetting(lister, vehicleDef, field, statDef.modSettingsInfo, statDef.LabelCap, statDef.description, disabledTooltip, locked, false);
			}
		}

		public override string ToString()
		{
			return $"{statDef?.defName ?? "[Null Stat]"} - {value}";
		}
	}
}
