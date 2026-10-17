using System;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using SmashTools.Xml;
using Verse;

namespace SmashTools.Patching;

internal class Patch_XmlParsing : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Mod;

	void IPatchCategory.PatchMethods()
	{
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
			prefix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(PreProcessAttributesOnDef)),
			postfix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(ReadCustomAttributesOnDef)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(DirectXmlToObjectNew), nameof(DirectXmlToObjectNew.DefFromNodeNew)),
			prefix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(PreProcessAttributesOnDef)),
			postfix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(ReadCustomAttributesOnDef)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(XmlToObjectUtils),
				nameof(XmlToObjectUtils.DoFieldSearch)),
			prefix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(PreProcessAttributes)),
			postfix: new HarmonyMethod(typeof(Patch_XmlParsing),
				nameof(ReadCustomAttributes)));
	}

	private static bool PreProcessAttributesOnDef(out Def __result, XmlNode node, LoadableXmlAsset loadingAsset)
	{
		__result = null;
		return PreProcessXmlNode(node, loadingAsset: loadingAsset, fieldInfo: null);
	}

	private static void ReadCustomAttributesOnDef(XmlNode node, LoadableXmlAsset loadingAsset)
	{
		ProcessXmlNode(node, loadingAsset: loadingAsset, fieldInfo: null);
	}

	private static bool PreProcessAttributes(XmlNode fieldNode, out FieldInfo __result)
	{
		__result = null;
		return PreProcessXmlNode(fieldNode, loadingAsset: null, __result);
	}

	/// <summary>
	/// Parse XmlNode and handle registered custom attributes
	/// </summary>
	private static void ReadCustomAttributes(XmlNode fieldNode, FieldInfo __result)
	{
		ProcessXmlNode(fieldNode, loadingAsset: null, __result);
	}

	private static bool PreProcessXmlNode(XmlNode node, LoadableXmlAsset loadingAsset, FieldInfo fieldInfo)
	{
		if (node?.NodeType != XmlNodeType.Element)
			return true;

		XmlAttributeCollection attributes = node.Attributes;
		if (attributes is null)
			return true;

		try
		{
			foreach (XmlAttribute attribute in attributes)
			{
				if (!XmlParseHelper.RegisteredAttributes.TryGetValue(attribute.Name, out XmlParseHelper.CustomAttribute attr))
					continue;

				if (!attr.PreProcess(node, attribute, loadingAsset, fieldInfo))
					return false;
			}
		}
		catch (Exception ex)
		{
			Log.Error(
				$"Exception thrown while trying to apply registered preprocessors to Def of type {node.Name}.\n{ex}");
		}
		return true;
	}


	private static void ProcessXmlNode(XmlNode node, LoadableXmlAsset loadingAsset, FieldInfo fieldInfo)
	{
		if (node?.NodeType != XmlNodeType.Element)
			return;

		XmlAttributeCollection attributes = node.Attributes;
		if (attributes is null)
			return;

		try
		{
			foreach (XmlAttribute attribute in attributes)
			{
				if (!XmlParseHelper.RegisteredAttributes.TryGetValue(attribute.Name, out XmlParseHelper.CustomAttribute attr))
					continue;

				attr.Process(node, attribute, loadingAsset, fieldInfo);
			}
		}
		catch (Exception ex)
		{
			Log.Error(
				$"Exception thrown while trying to apply registered attributes to Def of type {node.Name}.\n{ex}");
		}
	}
}