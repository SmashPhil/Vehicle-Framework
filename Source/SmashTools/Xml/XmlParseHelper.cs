using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using SmashTools.Animations;
using Verse;

namespace SmashTools.Xml;

[PublicAPI]
public static class XmlParseHelper
{
  private const string ValidAttributeRegex = "^([A-Za-z0-9]*$)";

  internal static readonly Dictionary<string, CustomAttribute> RegisteredAttributes = [];

  public delegate bool AttributePreProcessor(XmlNode node, string attrValue, FieldInfo fieldInfo = null);

  public delegate void AttributeProcessor(XmlNode node, string attrValue, FieldInfo fieldInfo = null);

  public delegate bool AttributeDefPreProcessor(XmlNode node, string attrValue, LoadableXmlAsset loadingAsset);

  public delegate void AttributeDefProcessor(XmlNode node, string attrValue, LoadableXmlAsset loadingAsset);

  internal static void RegisterParseTypes()
  {
    ParseHelper.Parsers<Rot8>.Register(Rot8.FromString);
    ParseHelper.Parsers<Quadrant>.Register(Quadrant.FromString);
    ParseHelper.Parsers<RimWorldTime>.Register(RimWorldTime.FromString);
    ParseHelper.Parsers<Guid>.Register(ParseGuid);
  }

  private static Guid ParseGuid(string entry)
  {
    return !entry.NullOrEmpty() ? Guid.Parse(entry) : Guid.Empty;
  }

  /// <summary>
  /// Wraps text into xml object so vanilla parsing can operate on it
  /// </summary>
  [Obsolete("Will be removed in 1.7")]
  public static object WrapStringAndParse(Type type, string innerText, bool doPostLoad = true)
  {
    if (ParseHelper.HandlesType(type))
    {
      return ParseHelper.FromString(innerText, type);
    }

    if (type != typeof(string))
    {
      innerText = innerText.Trim();
    }

    XmlDocument doc = new();
    doc.LoadXml($"<temp>{innerText}</temp>");
    XmlNode newNode = doc.DocumentElement;
    return DirectXmlToObject.GetObjectFromXmlMethod(type)(newNode, doPostLoad);
  }

  /// <summary>
  /// Register def preprocessor attribute to execute when loading a def from xml.
  /// </summary>
  /// <remarks>
  /// PreProcessor is called before the def has been parsed. If <paramref name="defPreProcessor"/> returns
  /// <see langword="false"/>, the def is skipped over for parsing.
  /// </remarks>
  /// <param name="attribute">XmlAttribute name</param>
  /// <param name="defPreProcessor">
  /// Function to execute on nodes with <paramref name="attribute"/>.
  /// </param>
  public static void RegisterDefPreProcessor(string attribute, AttributeDefPreProcessor defPreProcessor)
  {
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant))
    {
      Log.Error("Cannot register attribute due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }
    RegisteredAttributes.Add(attribute, new CustomAttribute(attribute)
    {
      DefPreProcessor = defPreProcessor
    });
  }

  /// <summary>
  /// Register def processor attribute to execute when loading a def from xml.
  /// </summary>
  /// <remarks>>Processor is called after the Def has been parsed, but before being added to the def database.</remarks>
  /// <param name="attribute">XmlAttribute name</param>
  /// <param name="defProcessor">
  /// Function to execute on nodes with <paramref name="attribute"/>
  /// </param>
  public static void RegisterDefProcessor(string attribute, AttributeDefProcessor defProcessor)
  {
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant))
    {
      Log.Error("Cannot register attribute due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }
    RegisteredAttributes.Add(attribute, new CustomAttribute(attribute)
    {
      DefProcessor = defProcessor
    });
  }

  /// <summary>
  /// Register custom preprocessor attribute to execute when loading fields from xml.
  /// </summary>
  /// <param name="attribute">XmlAttribute name</param>
  /// <param name="preProcessor">Action to be executed upon loading of attribute. If returns false, node will skip parsing.</param>
  /// <param name="nodeAllowed">Specify the only XmlNodes that will be able to use this XmlAttribute</param>
  public static void RegisterPreProcessor(string attribute,
    AttributePreProcessor preProcessor, params string[] nodeAllowed)
  {
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant))
    {
      Log.Error("Cannot register attribute due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }
    RegisteredAttributes.Add(attribute, new CustomAttribute(attribute, nodeAllowed)
    {
      PreProcessor = preProcessor
    });
  }

  /// <summary>
  /// Register custom attribute action to execute when loading fields from xml.
  /// </summary>
  /// <param name="attribute">XmlAttribute name</param>
  /// <param name="processor">Action to be executed upon loading of attribute</param>
  /// <param name="nodeAllowed">Specify the only XmlNodes that will be able to use this XmlAttribute</param>
  public static void RegisterAttribute(string attribute,
    AttributeProcessor processor, params string[] nodeAllowed)
  {
    if (!Regex.IsMatch(attribute, ValidAttributeRegex, RegexOptions.CultureInvariant))
    {
      Log.Error("Cannot register attribute due to invalid naming. Only alphanumeric characters may be used.");
      return;
    }
    RegisteredAttributes.Add(attribute, new CustomAttribute(attribute, nodeAllowed)
    {
      Processor = processor
    });
  }


  internal sealed class CustomAttribute
  {
    private readonly string attribute;

    private readonly HashSet<string> nodeWhiteList;

    public CustomAttribute(string attribute, params string[] nodeAllowed)
    {
      this.attribute = attribute;
      if (!nodeAllowed.NullOrEmpty())
      {
        nodeWhiteList = nodeAllowed.ToHashSet();
      }
    }

    public AttributePreProcessor PreProcessor { get; init; }

    public AttributeProcessor Processor { get; init; }

    public AttributeDefPreProcessor DefPreProcessor { get; init; }

    public AttributeDefProcessor DefProcessor { get; init; }

    public bool PreProcess(XmlNode node, XmlAttribute attr, LoadableXmlAsset loadingAsset, FieldInfo fieldInfo)
    {
      if (DefPreProcessor != null)
      {
        ValidateNode(node, attr);
        return DefPreProcessor(node, attr.Value, loadingAsset);
      }
      if (PreProcessor != null)
      {
        ValidateNode(node, attr);
        return PreProcessor(node, attr.Value, fieldInfo);
      }
      return true;
    }

    public void Process(XmlNode node, XmlAttribute attr, LoadableXmlAsset loadingAsset, FieldInfo fieldInfo)
    {
      if (Processor != null)
      {
        ValidateNode(node, attr);
        Processor(node, attr.Value, fieldInfo);
      }
      if (DefPreProcessor != null)
      {
        ValidateNode(node, attr);
        DefProcessor(node, attr.Value, loadingAsset);
      }
    }

    private bool ValidateNode(XmlNode node, XmlAttribute attr)
    {
      if (attr.Name.NullOrEmpty())
      {
        Log.Error("Malformed xml attribute, missing name.");
        return false;
      }
      if (!nodeWhiteList.NullOrEmpty() && !nodeWhiteList.Contains(node.Name))
      {
        Log.Error(
          $"Unable to execute {attribute}. It is only allowed to be used on nodes=({string.Join(",", nodeWhiteList)}) curNode={node.Name}");
        return false;
      }
      return true;
    }
  }
}