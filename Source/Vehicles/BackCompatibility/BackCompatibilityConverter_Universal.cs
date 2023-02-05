using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace Vehicles.BackCompatibility
{
	public class BackCompatibilityConverter_Universal : BackCompatibilityConverter
	{
		public override bool AppliesToVersion(int majorVer, int minorVer)
		{
			return true;
		}

		public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
		{
			if (defType == typeof(VehicleEventDef))
			{
				if (defName == "DraftOn") return "IgnitionOn";
				if (defName == "DraftOff") return "IgnitionOff";
			}
			return null;
		}

		public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
		{
			return null;
		}

		public override void PostExposeData(object obj)
		{
		}
	}
}
