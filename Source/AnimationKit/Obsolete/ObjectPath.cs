using System;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using SmashTools.Xml;
using Verse;

namespace SmashTools;

public class ObjectPath : IXmlExport
{
  private readonly Type type;
  private readonly string name;
  private readonly int index;

  public ObjectPath()
  {
  }

  public ObjectPath(FieldInfo fieldInfo, int index = -1)
  {
    this.index = index;
    type = fieldInfo.DeclaringType;
    name = fieldInfo.Name;
  }

  public int Index => index;

  public bool IsIndexer => index >= 0;

  public FieldInfo FieldInfo => AccessTools.Field(type, name);

  public object GetValue(object obj)
  {
    object childObj = FieldInfo.GetValue(obj);
    if (IsIndexer)
    {
      return FieldInfo.FieldType.GetMethod("get_Item", BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance).Invoke(childObj, [index]);
    }
    return childObj;
  }

  void IXmlExport.Export()
  {
    XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
    XmlExporter.WriteElement(nameof(name), name);
    XmlExporter.WriteObject(nameof(index), index);
  }

  public static implicit operator ObjectPath(FieldInfo fieldInfo)
  {
    return new ObjectPath(fieldInfo);
  }
}
