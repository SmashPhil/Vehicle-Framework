using System;
using System.Collections.Generic;
using System.Xml;
using Verse;
using HarmonyLib;

namespace SmashTools
{
	public static class ScribeWriter
	{
		public static void WriteElementWithAttribute(this ScribeSaver saver, string value, string attributeName, string attrValue)
		{
			XmlWriter writer = (XmlWriter)AccessTools.Field(typeof(ScribeSaver), "writer").GetValue(saver);
			if (writer is null)
			{
				Log.Error("Called WriteElementWithAttribute(), but writer is null.");
				return;
			}
			try
			{
				writer.WriteAttributeString(attributeName, attrValue);
				writer.WriteString(value);
			}
			catch (Exception)
			{
				AccessTools.Field(typeof(ScribeSaver), "anyInternalException").SetValue(saver, true);
				throw;
			}
		}

		public static void WriteElementWithAttributes(this ScribeSaver saver, string value, List<Pair<string, string>> attributeParams)
		{
			XmlWriter writer = (XmlWriter)AccessTools.Field(typeof(ScribeSaver), "writer").GetValue(saver);
			if (writer is null)
			{
				Log.Error("Called WriteElementWithAttributes(), but writer is null.");
				return;
			}
			try
			{
				foreach (Pair<string, string> attributeParam in attributeParams)
				{
					writer.WriteAttributeString(attributeParam.First, attributeParam.Second);
				}
				writer.WriteString(value);
			}
			catch (Exception)
			{
				AccessTools.Field(typeof(ScribeSaver), "anyInternalException").SetValue(saver, true);
				throw;
			}
		}
	}
}
