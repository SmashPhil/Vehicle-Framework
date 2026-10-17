using System;
using System.Xml;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	public static class ObjectValueExtractor
	{
		public static object ValueFromNode(XmlNode subNode)
		{
			if (subNode is null)
			{
				return null;
			}
			XmlAttribute xmlType = subNode.Attributes["Type"];
			XmlAttribute xmlSavedField = subNode.Attributes["SavedField"];
			if (xmlType is null)
			{
				Log.Error($"Failed to retrieve Type attribute from ObjectValue saved XmlNode. Cannot parse into game. Node: {subNode}");
				return null;
			}
			Type objectType = AccessTools.TypeByName(xmlType.Value);
			XmlAttribute xmlAttribute = subNode.Attributes["IsNull"];
			if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
			{
				return objectType.GetDefaultValue();
			}
			try
			{
				try
				{
					if (xmlSavedField != null)
					{
						return SavedField<object>.FromTypedString(subNode.InnerText, objectType);
					}
					return AccessTools.Method(typeof(ParseHelper), nameof(ParseHelper.FromString), new Type[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { subNode.InnerText, objectType });
				}
				catch (Exception ex)
				{
					Log.Error(string.Concat(new object[]
					{
						"Exception parsing node ",
						subNode.OuterXml,
						" into a ",
						objectType,
						":\n",
						ex.ToString()
					}));
				}
				return objectType.GetDefaultValue();
			}
			catch (Exception arg)
			{
				Log.Error("Exception loading XML: " + arg);
			}
			return objectType.GetDefaultValue();
		}
	}
}
