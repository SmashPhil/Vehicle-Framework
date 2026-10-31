using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// Simplified parsing for Dictionary where XmlNode Name = Key and XmlNode InnerText = Value
/// </summary>
/// <remarks>Similar to <see cref="RimWorld.StatModifier"/>'s implementation of Name + Text
/// parsing but for dictionaries.</remarks>
public class SimpleDictionary<K, V> : Dictionary<K, V>
{
  public SimpleDictionary()
  {
    if (!typeof(K).IsSubclassOf(typeof(Def)) && !ParseHelper.HandlesType(typeof(K)))
    {
      Log.Error(
        $"Attempting to use SimpleDictionary with Key of type {typeof(K)} which is not assignable from Def and not " +
        $"handled by ParseHelper. This will not be parseable on startup.");
    }
  }

  [UsedWithReflection]
  public void LoadDataFromXmlCustom(XmlNode xmlRoot)
  {
    try
    {
      if (xmlRoot["li"] != null)
      {
        ParseNormalDictionary(xmlRoot);
        return;
      }

      if (GenTypes.IsDef(typeof(K)))
      {
        foreach (XmlNode childNode in xmlRoot)
        {
          if (ValidateSimpleDictNode(childNode, xmlRoot))
          {
            DirectXmlCrossRefLoader.RegisterDictionaryWantsCrossRef(this, childNode,
              xmlRoot.Name);
          }
        }
      }
      else if (ParseHelper.HandlesType(typeof(K)) && ParseHelper.HandlesType(typeof(V)))
      {
        foreach (XmlNode xmlNode in xmlRoot)
        {
          K key = ParseHelper.FromString<K>(xmlNode.Name);
          V value = ParseHelper.FromString<V>(xmlNode.InnerText);
          Add(key, value);
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error($"Malformed dictionary XML. Node: {xmlRoot.OuterXml}.\n\nException: {ex}");
    }
  }

  private static bool ValidateSimpleDictNode(XmlNode listEntryNode, XmlNode listRootNode)
  {
    switch (listEntryNode)
    {
      case XmlComment:
        return false;
      case XmlText:
        Log.Error(
          "XML format error: Raw text found inside a list element. Did you mean to surround it with <li> tags? " +
          listRootNode.OuterXml);
        return false;
      default:
        return true;
    }
  }

  private void ParseNormalDictionary(XmlNode xmlNode)
  {
    if (!GenTypes.IsDef(typeof(K)) && !GenTypes.IsDef(typeof(V)))
    {
      foreach (XmlNode childNode in xmlNode)
      {
        if (DirectXmlToObject.ValidateListNode(childNode, xmlNode, typeof(KeyValuePair<K, V>)))
        {
          K key = DirectXmlToObject.ObjectFromXml<K>(childNode["key"], true);
          V value = DirectXmlToObject.ObjectFromXml<V>(childNode["value"], true);
          Add(key, value);
        }
      }
    }

    foreach (XmlNode childNode in xmlNode)
    {
      if (DirectXmlToObject.ValidateListNode(childNode, xmlNode, typeof(KeyValuePair<K, V>)))
      {
        DirectXmlCrossRefLoader.RegisterDictionaryWantsCrossRef(this, childNode, xmlNode.Name);
      }
    }
  }
}
