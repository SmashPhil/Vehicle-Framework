using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;

namespace Vehicles
{
	public class VehicleStatModifier : ICustomSettingsDrawer
	{
		public VehicleStatDef statDef;
		public float value;

		public void LoadDataFromXmlCustom(XmlNode xmlNode)
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(statDef), xmlNode.Name);
			value = ParseHelper.FromString<float>(xmlNode.InnerText);
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
