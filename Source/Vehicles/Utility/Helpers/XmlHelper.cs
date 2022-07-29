using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public static class XmlHelper
	{
		public static void FillDefaults_Def<K, V>(string defName, string fieldName, Dictionary<K, V> dictionary) where K : Def
		{
			if (ParsingHelper.setDefaultValues.TryGetValue(defName, out Dictionary<string, string> defaultValues))
			{
				if (defaultValues.TryGetValue(fieldName, out string valueString))
				{
					V value = (V)Convert.ChangeType(valueString, typeof(V));
					foreach (K def in DefDatabase<K>.AllDefsListForReading)
					{
						if (!dictionary.ContainsKey(def))
						{
							dictionary[def] = value;
						}
					}
				}
			}
		}

		public static void FillDefaults_Enum<K, V>(string defName, string fieldName, Dictionary<K, V> dictionary) where K : Enum
		{
			if (ParsingHelper.setDefaultValues.TryGetValue(defName, out Dictionary<string, string> defaultValues))
			{
				if (defaultValues.TryGetValue(fieldName, out string valueString))
				{
					V value = (V)Convert.ChangeType(valueString, typeof(V));
					foreach (K @enum in Enum.GetValues(typeof(K)))
					{
						if (!dictionary.ContainsKey(@enum))
						{
							dictionary[@enum] = value;
						}
					}
				}
			}
		}
	}
}
