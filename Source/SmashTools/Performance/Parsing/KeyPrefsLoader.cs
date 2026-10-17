using System;
using System.IO;
using System.Xml;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Performance;

internal static class KeyPrefsLoader
{
	private static string openParseNode;

	private delegate void Processor<T>(ref T item, string name, string value);

	public static void Init()
	{
		Load(out KeyPrefsData data);
		AccessTools.Field(typeof(KeyPrefs), "data").SetValue(null, data);
	}

	private static void Load(out KeyPrefsData data)
	{
#if DEBUG
		using ProfilerScope dps = new("KeyPrefsLoader");
#endif
		data = new KeyPrefsData();
		string filePath = GenFilePaths.KeyPrefsFilePath;
		bool newFile = false;
		if (!File.Exists(filePath))
		{
			data.ResetToDefaults();
			newFile = true;
		}
		else
		{
			XmlReaderSettings settings = new()
			{
				IgnoreWhitespace = true,
				IgnoreComments = true
			};
			using XmlReader reader = XmlReader.Create(filePath, settings);
			while (reader.Read())
			{
				// Start at top of li node
				if (reader.NodeType != XmlNodeType.Element || reader.Name != "li")
					continue;

				string openNode = reader.Name;
				KeyBindingDef keyBindingDef = null;
				KeyBindingData keyBindingData = null;
				do
				{
					if (reader.NodeType == XmlNodeType.EndElement && reader.Name == openNode)
					{
						openParseNode = null;
						return;
					}

					switch (reader.Name)
					{
						case "key":
							Parse(in reader, ref keyBindingDef, ParseDef);
						break;
						case "value":
							Parse(in reader, ref keyBindingData, ParseKeyBindingData);
						break;
					}
				} while (reader.Read());

				if (keyBindingDef != null && keyBindingData != null)
					data.keyPrefs[keyBindingDef] = keyBindingData;
			}
		}
		data.AddMissingDefaultBindings();
		data.ErrorCheck();
		if (!newFile)
		{
			KeyPrefs.Save();
		}
		return;

		static void ParseDef(ref KeyBindingDef keyBindingDef, string name, string value)
		{
			keyBindingDef = DefDatabase<KeyBindingDef>.GetNamed(value, errorOnFail: false);
		}

		static void ParseKeyBindingData(ref KeyBindingData keyBindingData, string name, string value)
		{
			keyBindingData ??= new KeyBindingData();
			KeyCode keyCode = Enum.Parse<KeyCode>(value);
			switch (name)
			{
				case nameof(KeyBindingData.keyBindingA):
					keyBindingData.keyBindingA = keyCode;
				break;
				case nameof(KeyBindingData.keyBindingB):
					keyBindingData.keyBindingB = keyCode;
				break;
			}
		}
	}

	private static void Parse<T>(ref readonly XmlReader reader, ref T obj, Processor<T> processor)
		where T : class
	{
		Assert.AreEqual(reader.NodeType, XmlNodeType.Element);
		openParseNode = reader.Name;
		do
		{
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == openParseNode)
			{
				openParseNode = null;
				return;
			}
			if (reader.NodeType == XmlNodeType.Text)
			{
				processor(ref obj, openParseNode, reader.Value);
			}
		} while (reader.Read());
	}
}